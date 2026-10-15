using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Zyft.Application.Common.Interfaces;

namespace Zyft.AcceptanceTests.Support;

/// <summary>API real em memória (SQLite) com uma IA programável pelo cenário.</summary>
public sealed class AcceptanceFactory : WebApplicationFactory<Program>
{
    private readonly string _cs = $"Data Source=zyft-acc-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public AcceptanceFactory()
    {
        _keepAlive = new SqliteConnection(_cs);
        _keepAlive.Open();
    }

    public ProgrammableLlm Llm { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:Default", _cs);
        builder.UseSetting("Jwt:Secret", "segredo-de-aceitacao-com-mais-de-32-caracteres");
        builder.UseSetting("Encryption:Key", Convert.ToBase64String(new byte[32]));
        builder.UseSetting("Llm:Provider", "None");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
        builder.UseSetting("RateLimiting:AiPermitPerMinute", "1000");
        builder.ConfigureTestServices(s =>
        {
            s.RemoveAll<ILlmClient>();
            s.AddSingleton<ILlmClient>(Llm);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _keepAlive.Dispose();
    }
}

/// <summary>IA de mentira: desligada por padrão; o cenário programa a resposta.</summary>
public sealed class ProgrammableLlm : ILlmClient
{
    public Func<LlmJsonRequest, string>? Respond { get; set; }
    public bool IsEnabled => Respond is not null;

    public Task<JsonElement> CompleteJsonAsync(LlmJsonRequest request, CancellationToken ct = default)
    {
        if (Respond is null) throw new LlmUnavailableException("IA desligada no cenário.");
        return Task.FromResult(JsonDocument.Parse(Respond(request)).RootElement.Clone());
    }
}
