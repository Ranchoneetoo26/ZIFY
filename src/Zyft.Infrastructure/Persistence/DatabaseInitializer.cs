using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Answers;
using Zyft.Domain.Audit;
using Zyft.Domain.Consents;
using Zyft.Domain.Profiles;
using Zyft.Domain.Users;

namespace Zyft.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// PostgreSQL: aplica as migrations (<c>Database.Migrate</c>). SQLite: <c>EnsureCreated</c> — as migrations são
    /// específicas do provedor (tipos, índices) e o SQLite serve só para desenvolvimento rápido e testes.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, bool seedDemo, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZyftDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Zyft.Database");

        if (db.Database.IsSqlite()) await db.Database.EnsureCreatedAsync(ct);
        else await db.Database.MigrateAsync(ct);
        logger.LogInformation("Banco pronto ({Provider})", db.Database.ProviderName);

        if (seedDemo) await SeedDemoAsync(scope.ServiceProvider, db, logger, ct);
    }

    /// <summary>Conta demo principal (Rafael Moreira, Tecnologia).</summary>
    public const string DemoEmail = "demo@zyft.local";

    /// <summary>Segunda conta demo (Beatriz Lima, Saúde), mesma senha.</summary>
    public const string DemoHealthEmail = "beatriz@demo.zyft";

    public const string DemoPassword = "Zyft@2026";

    /// <summary>Usuários de demonstração (só em Development). Dados fictícios — ver <see cref="DemoSeed"/>.</summary>
    private static Task SeedDemoAsync(IServiceProvider sp, ZyftDbContext db, ILogger logger, CancellationToken ct) =>
        DemoSeed.SeedAsync(db, sp.GetRequiredService<IPasswordHasher>(), logger, DemoPassword, ct);
}
