using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Answers;
using Zyft.Domain.Applications;
using Zyft.Domain.Audit;
using Zyft.Domain.Common;
using Zyft.Domain.Consents;
using Zyft.Domain.Extension;
using Zyft.Domain.Imports;
using Zyft.Domain.Profiles;
using Zyft.Domain.Sensitive;
using Zyft.Domain.Users;

namespace Zyft.Infrastructure.Persistence;

public sealed class ZyftDbContext(DbContextOptions<ZyftDbContext> options, IClock? clock = null) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Consent> Consents => Set<Consent>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<ApprovedAnswer> ApprovedAnswers => Set<ApprovedAnswer>();
    public DbSet<SensitiveData> SensitiveData => Set<SensitiveData>();
    public DbSet<ResumeImport> ResumeImports => Set<ResumeImport>();
    public DbSet<ExtensionDevice> ExtensionDevices => Set<ExtensionDevice>();
    public DbSet<PairingCode> PairingCodes => Set<PairingCode>();
    public DbSet<AuthorizedSite> AuthorizedSites => Set<AuthorizedSite>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ZyftDbContext).Assembly);

        // Convenções comuns: Guid gerado no cliente (ValueGeneratedNever) e DateTime sempre UTC.
        var utc = new ValueConverter<DateTime, DateTime>(v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcNullable = new ValueConverter<DateTime?, DateTime?>(v => v == null ? v : v.Value.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc), v => v == null ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entity.ClrType))
                entity.FindProperty(nameof(Entity.Id))!.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            foreach (var p in entity.GetProperties())
            {
                if (p.ClrType == typeof(DateTime)) p.SetValueConverter(utc);
                else if (p.ClrType == typeof(DateTime?)) p.SetValueConverter(utcNullable);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock?.UtcNow ?? DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries<Entity>())
        {
            if (e.State == EntityState.Added)
            {
                e.Property(nameof(Entity.CreatedAt)).CurrentValue = now;
                e.Property(nameof(Entity.UpdatedAt)).CurrentValue = now;
            }
            else if (e.State == EntityState.Modified)
            {
                e.Property(nameof(Entity.UpdatedAt)).CurrentValue = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
