using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zyft.Infrastructure.Persistence;

namespace Zyft.IntegrationTests;

/// <summary>Banco SQLite de desenvolvimento criado por uma versão anterior ganha tabelas e colunas novas na subida.</summary>
public class SqliteUpgradeTests
{
    [Fact]
    public async Task Banco_antigo_ganha_tabelas_e_colunas_que_faltam_sem_perder_dados()
    {
        var cs = $"Data Source=zyft-upgrade-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var keepAlive = new SqliteConnection(cs);
        await keepAlive.OpenAsync();
        var services = new ServiceCollection().AddLogging().AddDbContext<ZyftDbContext>(o => o.UseSqlite(cs)).BuildServiceProvider();

        // Simula o esquema antigo: sem a tabela de tokens e sem a coluna nova em Users, com uma conta existente.
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZyftDbContext>();
            await db.Database.EnsureCreatedAsync();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"UserTokens\"");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" DROP COLUMN \"EmailVerifiedAt\"");
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO \"Users\" (\"Id\", \"Email\", \"PasswordHash\", \"Name\", \"CreatedAt\", \"UpdatedAt\") VALUES ('11111111-1111-1111-1111-111111111111', 'velho@exemplo.com', 'h', 'Velho', '2026-10-01 00:00:00', '2026-10-01 00:00:00')");
        }

        await DatabaseInitializer.InitializeAsync(services, new ConfigurationBuilder().Build(), seedDemo: false);

        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ZyftDbContext>();
            var user = await db.Users.SingleAsync();
            user.Email.Should().Be("velho@exemplo.com");
            user.EmailVerifiedAt.Should().BeNull();
            (await db.UserTokens.CountAsync()).Should().Be(0);
            user.MarkEmailVerified(DateTime.UtcNow);
            await db.SaveChangesAsync();
        }

        // Rodar de novo não muda nada (idempotente).
        await DatabaseInitializer.InitializeAsync(services, new ConfigurationBuilder().Build(), seedDemo: false);
    }
}
