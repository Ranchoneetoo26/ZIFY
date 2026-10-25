using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zyft.Infrastructure.Persistence;

/// <summary>Usada só pelo <c>dotnet ef</c> para gerar migrations PostgreSQL (não conecta no banco).</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ZyftDbContext>
{
    public ZyftDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ZYFT_MIGRATIONS_CS") ?? "Host=localhost;Port=5432;Database=zyft;Username=zyft;Password=zyft";
        var options = new DbContextOptionsBuilder<ZyftDbContext>().UseNpgsql(cs).Options;
        return new ZyftDbContext(options);
    }
}
