using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Core;
using Serilog.Events;
using Zyft.Application.Common.Interfaces;

namespace Zyft.IntegrationTests.Support;

/// <summary>Sobe a API real em memória com SQLite in-memory (uma base por fábrica) e IA desligada ou de mentira.</summary>
public class ZyftApiFactory : WebApplicationFactory<Program>
{
    private readonly string _cs = $"Data Source=zyft-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public ZyftApiFactory()
    {
        _keepAlive = new SqliteConnection(_cs);
        _keepAlive.Open(); // mantém a base em memória viva enquanto a fábrica existir
    }

    public int AuthPermitLimit { get; init; } = 1000;

    public int AiPermitPerMinute { get; init; } = 1000;

    /// <summary>IA substituta (null = IA desligada).</summary>
    public ILlmClient? Llm { get; init; }

    public CapturingSink Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:Default", _cs);
        builder.UseSetting("Jwt:Secret", "segredo-de-teste-com-mais-de-32-caracteres!!");
        builder.UseSetting("Encryption:Key", Convert.ToBase64String(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray()));
        builder.UseSetting("Llm:Provider", "None");
        builder.UseSetting("RateLimiting:AuthPermitLimit", AuthPermitLimit.ToString());
        builder.UseSetting("RateLimiting:AiPermitPerMinute", AiPermitPerMinute.ToString());
        builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5080");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILogEventSink>(Logs);
            if (Llm is not null)
            {
                services.RemoveAll<ILlmClient>();
                services.AddSingleton(Llm);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keepAlive.Dispose();
    }
}

public sealed class CapturingSink : ILogEventSink
{
    private static readonly Serilog.Formatting.Display.MessageTemplateTextFormatter Formatter = new("{Level:u3} {Message:lj} {Properties:j} {Exception}");

    public ConcurrentQueue<string> Lines { get; } = new();

    public void Emit(LogEvent logEvent)
    {
        using var sw = new StringWriter();
        Formatter.Format(logEvent, sw);
        Lines.Enqueue(sw.ToString());
    }
}

public static class TestApi
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public sealed record Session(string AccessToken, string RefreshToken, Guid UserId, string Email);

    public static async Task<Session> RegisterAsync(HttpClient c, string? email = null, string password = "Senha123")
    {
        email ??= $"pessoa{Guid.NewGuid():N}@exemplo.com";
        var res = await c.PostAsJsonAsync("/api/v1/auth/register", new { name = "Pessoa Teste", email, password });
        res.EnsureSuccessStatusCode();
        var j = await res.Content.ReadFromJsonAsync<JsonElement>();
        return new Session(j.GetProperty("accessToken").GetString()!, j.GetProperty("refreshToken").GetString()!, j.GetProperty("user").GetProperty("id").GetGuid(), email);
    }

    public static HttpClient Authed(this WebApplicationFactory<Program> f, string token)
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage res) => await res.Content.ReadFromJsonAsync<JsonElement>(Json);
}
