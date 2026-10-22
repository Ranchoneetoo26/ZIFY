using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Zyft.Api.Controllers;
using Zyft.Api.Infrastructure;
using Zyft.Application;
using Zyft.Application.Common.Interfaces;
using Zyft.Infrastructure;
using Zyft.Infrastructure.Persistence;
using Zyft.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------------------------------------------------------------- Logs (Serilog, sem dados pessoais — RNF-09)
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"),
    preserveStaticLogger: true); // logger por host (não usa Log.Logger estático: hosts paralelos nos testes se misturavam)

// ---------------------------------------------------------------- Camadas
builder.Services.AddApplication();
builder.Services.AddInfrastructure(config);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // enums como nome ("AiText", "Ready")
        o.JsonSerializerOptions.Converters.Add(new LenientDateOnlyConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ctx =>
    {
        // Erros de desserialização/model binding → 400 pt-BR (sem expor mensagens internas do .NET).
        // A chave do parâmetro da action (ex.: "cmd") não é um campo: só aparece quando nada mais explica o erro.
        var paramNames = ctx.ActionDescriptor.Parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var invalid = ctx.ModelState.Where(kv => kv.Value?.Errors.Count > 0).Select(kv => kv.Key.TrimStart('$', '.')).ToList();
        var fields = invalid.Where(k => k.Length > 0 && !paramNames.Contains(k)).ToList();
        var errors = (fields.Count > 0 ? fields : new List<string> { "body" })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                k => Problems.CamelPath(k),
                k => new[] { Problems.BindingMessage(k) });
        var pd = Problems.Create(ctx.HttpContext, 400, "Um ou mais campos são inválidos.", "validation_failed", errors);
        return new BadRequestObjectResult(pd) { ContentTypes = { "application/problem+json" } };
    });
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 12 * 1024 * 1024);

// ---------------------------------------------------------------- Autenticação JWT + policies
var jwt = config.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
    throw new InvalidOperationException("Configure Jwt:Secret (variável Jwt__Secret) com pelo menos 32 caracteres.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "sub",
    };
    o.Events = new JwtBearerEvents
    {
        // Token de extensão revogada deixa de valer na hora, mesmo antes de expirar.
        OnTokenValidated = async ctx =>
        {
            if (ctx.Principal?.FindFirst("scope")?.Value != TokenScopes.Extension) return;
            var deviceId = Guid.TryParse(ctx.Principal.FindFirst("device_id")?.Value, out var d) ? d : Guid.Empty;
            var db = ctx.HttpContext.RequestServices.GetRequiredService<ZyftDbContext>();
            if (!await db.ExtensionDevices.AnyAsync(x => x.Id == deviceId && x.RevokedAt == null))
                ctx.Fail("Extensão revogada.");
        },
        OnChallenge = async ctx =>
        {
            ctx.HandleResponse();
            await Problems.WriteAsync(ctx.HttpContext, 401, "Faça login para continuar (token ausente, inválido ou expirado).", "unauthorized");
        },
        OnForbidden = ctx => Problems.WriteAsync(ctx.HttpContext, 403, "Esta ação não é permitida para este tipo de acesso (ex.: token da extensão).", "forbidden"),
    };
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.WebUser, p => p.RequireAuthenticatedUser().RequireClaim("scope", TokenScopes.Web))
    .AddPolicy(Policies.ExtensionOrWeb, p => p.RequireAuthenticatedUser().RequireClaim("scope", TokenScopes.Web, TokenScopes.Extension));

// ---------------------------------------------------------------- Rate limiting (login, cadastro, pareamento)
var authPermit = config.GetValue("RateLimiting:AuthPermitLimit", 10);
var aiPermit = config.GetValue("RateLimiting:AiPermitPerMinute", 60);
builder.Services.AddRateLimiter(o =>
{
    // Rotas que acionam IA/processamento pesado: limite POR PESSOA (claim sub), não por IP (D-04).
    o.AddPolicy(Policies.AiRateLimit, http => RateLimitPartition.GetFixedWindowLimiter(
        http.User.FindFirst("sub")?.Value ?? "anon:" + (http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = aiPermit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.AddPolicy(Policies.AuthRateLimit, http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = authPermit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await Problems.WriteAsync(ctx.HttpContext, 429, "Muitas tentativas. Aguarde um minuto e tente de novo.", "rate_limited");
    };
});

// ---------------------------------------------------------------- CORS (plataforma + extensão)
var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var extensionIds = config.GetSection("Cors:ExtensionIds").Get<string[]>() ?? Array.Empty<string>();
if (builder.Environment.IsProduction() && extensionIds.Length == 0)
    throw new InvalidOperationException("Em produção, configure Cors:ExtensionIds (IDs publicados da extensão); sem a lista, qualquer extensão seria aceita (D-07).");
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .SetIsOriginAllowed(origin =>
    {
        if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
        const string ext = "chrome-extension://";
        if (!origin.StartsWith(ext, StringComparison.OrdinalIgnoreCase)) return false;
        // Sem lista configurada (desenvolvimento) aceita qualquer extensão; em produção, só os IDs publicados.
        return extensionIds.Length == 0 || extensionIds.Contains(origin[ext.Length..].TrimEnd('/'), StringComparer.OrdinalIgnoreCase);
    })
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Content-Disposition", "Retry-After")));

// ---------------------------------------------------------------- Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Zyft API — Candidatura Assistida (MVP 1)",
        Version = "v1",
        Description = "API do Zyft. Erros em ProblemDetails (pt-BR). Autentique em POST /api/v1/auth/login e use o botão Authorize com o accessToken.",
    });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole só o accessToken (sem a palavra Bearer).",
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>(),
    });
    foreach (var xml in new[] { "Zyft.Api.xml", "Zyft.Application.xml" })
    {
        var path = Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(path)) o.IncludeXmlComments(path);
    }

    o.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date", Example = new Microsoft.OpenApi.Any.OpenApiString("2024-03-01") });
    o.CustomSchemaIds(t => t.FullName!.Replace("Zyft.Application.", string.Empty).Replace("Zyft.Api.", string.Empty).Replace('+', '.'));
});

// ---------------------------------------------------------------- Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ZyftDbContext>("database")
    .AddCheck<LlmHealthCheck>("llm", HealthStatus.Degraded);

var app = builder.Build();

// ---------------------------------------------------------------- Pipeline
// O log de requisição fica POR FORA do tratamento de exceções: assim registra o status real (401, 422…) e não um 500
// com a exceção de negócio anexada (bug encontrado no teste de logs).
app.UseSerilogRequestLogging(o =>
{
    // Só método, caminho (sem query string) e status — nada de corpo, e-mail ou token.
    o.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0} ms";
    o.Logger = app.Services.GetService<Serilog.ILogger>();
});
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages(async ctx =>
{
    var http = ctx.HttpContext;
    if (http.Request.Path.StartsWithSegments("/api") && http.Response.ContentLength is null && string.IsNullOrEmpty(http.Response.ContentType))
    {
        var msg = http.Response.StatusCode switch
        {
            404 => "Rota não encontrada.",
            405 => "Método não permitido nesta rota.",
            415 => "Tipo de conteúdo não suportado. Envie application/json (ou multipart/form-data no upload).",
            _ => "Erro na requisição.",
        };
        await Problems.WriteAsync(http, http.Response.StatusCode, msg);
    }
});

if (config.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Zyft API v1");
        o.DocumentTitle = "Zyft API";
    });
}

// Frontend web (HTML/CSS/JS sem build): ZIFY/web em dev; wwwroot no container.
var webRoot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, config["Web:RootPath"] ?? "../../web"));
IFileProvider? webFiles = Directory.Exists(webRoot) ? new PhysicalFileProvider(webRoot) : app.Environment.WebRootFileProvider;
if (webFiles is not null)
{
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = webFiles });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = webFiles });
}

app.UseRouting();
app.UseCors();
app.UseAuthentication(); // antes do rate limiter: a política "ai" particiona pelo usuário autenticado
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description }),
        }, new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    },
});

if (config.GetValue("Database:InitializeOnStartup", true))
{
    var seed = app.Environment.IsDevelopment() && config.GetValue("Seed:DemoUser", true);
    await DatabaseInitializer.InitializeAsync(app.Services, config, seed);
}

app.Run();

/// <summary>Ponto de entrada (exposto para os testes de integração com WebApplicationFactory).</summary>
public partial class Program;
