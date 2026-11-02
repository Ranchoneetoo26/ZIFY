using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Ads;
using Zyft.Domain.Answers;
using Zyft.Domain.Applications;
using Zyft.Domain.Audit;
using Zyft.Domain.Billing;
using Zyft.Domain.Common;
using Zyft.Domain.Consents;
using Zyft.Domain.Extension;
using Zyft.Domain.Imports;
using Zyft.Domain.Onboarding;
using Zyft.Domain.Profiles;
using Zyft.Domain.Sensitive;
using Zyft.Domain.Users;

namespace Zyft.Infrastructure.Persistence;

public sealed class ZyftDbContext(DbContextOptions<ZyftDbContext> options, IClock? clock = null, IProfileConcurrency? profileConcurrency = null)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
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
    public DbSet<CustomField> CustomFields => Set<CustomField>();
    public DbSet<OnboardingProgress> OnboardingProgress => Set<OnboardingProgress>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<MonthlyUsage> MonthlyUsages => Set<MonthlyUsage>();
    public DbSet<UsagePack> UsagePacks => Set<UsagePack>();
    public DbSet<UsageMark> UsageMarks => Set<UsageMark>();
    public DbSet<AdHide> AdHides => Set<AdHide>();
    public DbSet<AdStat> AdStats => Set<AdStat>();

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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = ChangeTracker.Entries<Profile>().Where(e => e.State == EntityState.Modified).ToList();
        if (profileConcurrency?.ExpectedVersion is { } expected && profiles.Count > 0)
        {
            // If-Match: a verificação vai no próprio UPDATE (WHERE Version = esperada), sem janela entre ler e gravar.
            foreach (var p in profiles) p.Property(x => x.Version).OriginalValue = expected;
            profileConcurrency.ExpectedVersion = null; // vale para a primeira gravação; as seguintes partem da nova versão
        }

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

        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            if (profileConcurrency is not null && profiles.Count > 0) profileConcurrency.SavedVersion = profiles[^1].Entity.Version;
            return result;
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.FirstOrDefault(e => e.Entity is Profile) is { } entry)
        {
            var current = await entry.GetDatabaseValuesAsync(cancellationToken);
            throw new ProfileVersionConflictException(current?.GetValue<int>(nameof(Profile.Version)) ?? 0);
        }
    }
}
