using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zyft.Domain.Ads;
using Zyft.Domain.Answers;
using Zyft.Domain.Applications;
using Zyft.Domain.Audit;
using Zyft.Domain.Billing;
using Zyft.Domain.Consents;
using Zyft.Domain.Extension;
using Zyft.Domain.Imports;
using Zyft.Domain.Onboarding;
using Zyft.Domain.Profiles;
using Zyft.Domain.Sensitive;
using Zyft.Domain.Users;

namespace Zyft.Infrastructure.Persistence.Configurations;

// Fluent API: tabelas relacionais, FKs com cascade a partir de User (RF-08), índices únicos e tamanhos.
// Enums gravados como texto (legível em consultas e estável se a ordem do enum mudar).

internal sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
    }
}

internal sealed class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.Property(x => x.ReplacedByHash).HasMaxLength(64);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        // Sem cascade a partir do dispositivo: a exclusão de usuário já apaga os dois (evita múltiplos caminhos de cascade).
        b.HasOne<ExtensionDevice>().WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ConsentConfig : IEntityTypeConfiguration<Consent>
{
    public void Configure(EntityTypeBuilder<Consent> b)
    {
        b.ToTable("Consents");
        b.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(x => new { x.UserId, x.Purpose }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ProfileConfig : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> b)
    {
        b.ToTable("Profiles");
        b.HasIndex(x => x.UserId).IsUnique(); // 1:1 com User
        b.HasOne<User>().WithOne().HasForeignKey<Profile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Property(x => x.FullName).HasMaxLength(160);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.City).HasMaxLength(120);
        b.Property(x => x.State).HasMaxLength(60);
        b.Property(x => x.Country).HasMaxLength(80);
        b.Property(x => x.ZipCode).HasMaxLength(20);
        b.Property(x => x.LinkedinUrl).HasMaxLength(300);
        b.Property(x => x.GithubUrl).HasMaxLength(300);
        b.Property(x => x.PortfolioUrl).HasMaxLength(300);
        b.Property(x => x.Headline).HasMaxLength(200);
        b.Property(x => x.Summary).HasMaxLength(4000);
        b.Property(x => x.DesiredRoles).HasMaxLength(500);
        b.Property(x => x.Areas).HasMaxLength(500);
        b.Property(x => x.Seniority).HasMaxLength(60);
        b.Property(x => x.WorkModel).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.LocationPreference).HasMaxLength(200);
        b.Property(x => x.Availability).HasMaxLength(200);
        b.Property(x => x.SalaryExpectation).HasPrecision(12, 2);
        b.Property(x => x.ContractTypes).HasMaxLength(200);
        b.Property(x => x.DriverLicenseCategory).HasMaxLength(5);
        b.Property(x => x.WorkArea).HasMaxLength(80);
        b.Property(x => x.ProfessionalRegistration).HasMaxLength(120);
        b.Property(x => x.SalaryType).HasMaxLength(30);

        b.HasMany(x => x.Experiences).WithOne().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Educations).WithOne().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Courses).WithOne().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Skills).WithOne().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Languages).WithOne().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.CustomFields).WithOne().HasForeignKey(x => x.ProfileId).OnDelete(DeleteBehavior.Cascade);
        foreach (var nav in new[] { nameof(Profile.Experiences), nameof(Profile.Educations), nameof(Profile.Courses), nameof(Profile.Skills), nameof(Profile.Languages), nameof(Profile.CustomFields) })
            b.Navigation(nav).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ExperienceConfig : IEntityTypeConfiguration<Experience>
{
    public void Configure(EntityTypeBuilder<Experience> b)
    {
        b.ToTable("Experiences", t => t.HasCheckConstraint("CK_Experiences_Period", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));
        b.Property(x => x.Company).HasMaxLength(200).IsRequired();
        b.Property(x => x.Role).HasMaxLength(200).IsRequired();
        b.Property(x => x.Activities).HasMaxLength(4000);
        b.Property(x => x.Tools).HasMaxLength(1000);
        b.Property(x => x.Results).HasMaxLength(4000);
    }
}

internal sealed class EducationConfig : IEntityTypeConfiguration<Education>
{
    public void Configure(EntityTypeBuilder<Education> b)
    {
        b.ToTable("Educations", t => t.HasCheckConstraint("CK_Educations_Period", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\""));
        b.Property(x => x.Institution).HasMaxLength(200).IsRequired();
        b.Property(x => x.Course).HasMaxLength(200).IsRequired();
        b.Property(x => x.Level).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
    }
}

internal sealed class CourseConfig : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.ToTable("Courses");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Institution).HasMaxLength(200);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
    }
}

internal sealed class SkillConfig : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> b)
    {
        b.ToTable("Skills");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Level).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => new { x.ProfileId, x.Name }).IsUnique();
    }
}

internal sealed class LanguageConfig : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> b)
    {
        b.ToTable("Languages");
        b.Property(x => x.Name).HasMaxLength(60).IsRequired();
        b.Property(x => x.Proficiency).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => new { x.ProfileId, x.Name }).IsUnique(); // D-06
    }
}

internal sealed class ApprovedAnswerConfig : IEntityTypeConfiguration<ApprovedAnswer>
{
    public void Configure(EntityTypeBuilder<ApprovedAnswer> b)
    {
        b.ToTable("ApprovedAnswers");
        b.Property(x => x.Question).HasMaxLength(500).IsRequired();
        b.Property(x => x.Text).HasMaxLength(5000).IsRequired();
        b.HasIndex(x => x.UserId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SensitiveDataConfig : IEntityTypeConfiguration<SensitiveData>
{
    public void Configure(EntityTypeBuilder<SensitiveData> b)
    {
        b.ToTable("SensitiveData");
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.EncryptedValue).HasMaxLength(1024).IsRequired();
        b.HasIndex(x => new { x.UserId, x.Kind }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ResumeImportConfig : IEntityTypeConfiguration<ResumeImport>
{
    public void Configure(EntityTypeBuilder<ResumeImport> b)
    {
        b.ToTable("ResumeImports");
        b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ProposalJson).IsRequired();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ExtensionDeviceConfig : IEntityTypeConfiguration<ExtensionDevice>
{
    public void Configure(EntityTypeBuilder<ExtensionDevice> b)
    {
        b.ToTable("ExtensionDevices");
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Ignore(x => x.IsActive);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PairingCodeConfig : IEntityTypeConfiguration<PairingCode>
{
    public void Configure(EntityTypeBuilder<PairingCode> b)
    {
        b.ToTable("PairingCodes");
        b.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.CodeHash);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AuthorizedSiteConfig : IEntityTypeConfiguration<AuthorizedSite>
{
    public void Configure(EntityTypeBuilder<AuthorizedSite> b)
    {
        b.ToTable("AuthorizedSites");
        b.Property(x => x.Origin).HasMaxLength(300).IsRequired();
        b.HasIndex(x => new { x.UserId, x.Origin }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class JobApplicationConfig : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> b)
    {
        b.ToTable("Applications");
        b.Property(x => x.Portal).HasMaxLength(200).IsRequired();
        b.Property(x => x.JobTitle).HasMaxLength(300).IsRequired();
        b.Property(x => x.Company).HasMaxLength(200);
        b.Property(x => x.JobUrl).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.HasIndex(x => new { x.UserId, x.ConfirmedAt });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AuditEventConfig : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.ToTable("AuditEvents");
        b.Property(x => x.Action).HasMaxLength(60).IsRequired();
        b.Property(x => x.Portal).HasMaxLength(200);
        b.HasIndex(x => new { x.UserId, x.OccurredAt });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CustomFieldConfig : IEntityTypeConfiguration<CustomField>
{
    public void Configure(EntityTypeBuilder<CustomField> b)
    {
        b.ToTable("CustomFields");
        b.Property(x => x.Label).HasMaxLength(CustomField.MaxLabel).IsRequired();
        b.Property(x => x.Value).HasMaxLength(CustomField.MaxValue).IsRequired();
        b.Ignore(x => x.Key);
        b.HasIndex(x => new { x.ProfileId, x.Label }).IsUnique();
    }
}

internal sealed class OnboardingProgressConfig : IEntityTypeConfiguration<OnboardingProgress>
{
    public void Configure(EntityTypeBuilder<OnboardingProgress> b)
    {
        b.ToTable("OnboardingProgress");
        b.Property(x => x.Answered).HasMaxLength(600).IsRequired();
        b.Property(x => x.Skipped).HasMaxLength(600).IsRequired();
        b.Property(x => x.LastStep).HasMaxLength(30);
        b.Ignore(x => x.AnsweredSteps);
        b.Ignore(x => x.SkippedSteps);
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne<User>().WithOne().HasForeignKey<OnboardingProgress>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SubscriptionConfig : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("Subscriptions");
        b.Property(x => x.Plan).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Period).HasConversion<string>().HasMaxLength(10);
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne<User>().WithOne().HasForeignKey<Subscription>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class MonthlyUsageConfig : IEntityTypeConfiguration<MonthlyUsage>
{
    public void Configure(EntityTypeBuilder<MonthlyUsage> b)
    {
        b.ToTable("MonthlyUsages");
        b.Property(x => x.Month).HasMaxLength(7).IsRequired();
        b.Ignore(x => x.BonusRemaining);
        b.HasIndex(x => new { x.UserId, x.Month }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UsagePackConfig : IEntityTypeConfiguration<UsagePack>
{
    public void Configure(EntityTypeBuilder<UsagePack> b)
    {
        b.ToTable("UsagePacks");
        b.HasIndex(x => new { x.UserId, x.ExpiresAt });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UsageMarkConfig : IEntityTypeConfiguration<UsageMark>
{
    public void Configure(EntityTypeBuilder<UsageMark> b)
    {
        b.ToTable("UsageMarks");
        b.Property(x => x.Month).HasMaxLength(7).IsRequired();
        b.Property(x => x.Resource).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.UserId, x.Month, x.Resource, x.KeyHash }).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AdHideConfig : IEntityTypeConfiguration<AdHide>
{
    public void Configure(EntityTypeBuilder<AdHide> b)
    {
        b.ToTable("AdHides");
        b.Property(x => x.Scope).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.AdId).HasMaxLength(60).IsRequired();
        b.Property(x => x.Topic).HasMaxLength(60);
        b.HasIndex(x => x.UserId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Contadores agregados por anúncio: sem FK para usuário (não há dado pessoal).</summary>
internal sealed class AdStatConfig : IEntityTypeConfiguration<AdStat>
{
    public void Configure(EntityTypeBuilder<AdStat> b)
    {
        b.ToTable("AdStats");
        b.Property(x => x.AdId).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.AdId).IsUnique();
    }
}
