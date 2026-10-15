using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Zyft.Application.Common.Interfaces;
using Zyft.Infrastructure.Ai;
using Zyft.Infrastructure.Files;
using Zyft.Infrastructure.Persistence;
using Zyft.Infrastructure.Security;

namespace Zyft.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra persistência, segurança e IA. <c>Database:Provider</c> = <c>Postgres</c> (padrão, com migrations)
    /// ou <c>Sqlite</c> (desenvolvimento sem Docker e testes, com EnsureCreated).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Database:Provider"] ?? "Postgres";
        var cs = config.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
        services.AddDbContext<ZyftDbContext>(o =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)) o.UseSqlite(cs);
            else o.UseNpgsql(cs, npg => npg.MigrationsAssembly(typeof(ZyftDbContext).Assembly.FullName));
        });
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ZyftDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IProfileRepository, ProfileRepository>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.Configure<EncryptionOptions>(config.GetSection(EncryptionOptions.Section));
        services.Configure<LlmOptions>(config.GetSection(LlmOptions.Section));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ISensitiveDataProtector, AesGcmSensitiveDataProtector>();
        services.AddSingleton<IResumeTextExtractor, ResumeTextExtractor>();

        var llm = config.GetSection(LlmOptions.Section).Get<LlmOptions>() ?? new LlmOptions();
        if (llm.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<ILlmClient, OllamaLlmClient>((sp, http) =>
            {
                var o = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
                http.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
                http.Timeout = Timeout.InfiniteTimeSpan; // o timeout é por chamada (CancellationToken)
            });
        }
        else
        {
            services.AddSingleton<ILlmClient, NullLlmClient>();
        }

        return services;
    }
}
