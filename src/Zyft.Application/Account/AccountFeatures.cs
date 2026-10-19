using MediatR;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Answers;
using Zyft.Domain.Applications;
using Zyft.Domain.Audit;
using Zyft.Domain.Consents;
using Zyft.Domain.Extension;
using Zyft.Domain.Imports;
using Zyft.Domain.Sensitive;
using Zyft.Domain.Users;
using Zyft.Application.Profiles;

namespace Zyft.Application.Account;

public sealed record MeDto(Guid Id, string Email, string Name, DateTime CreatedAt, string? Scope);

public sealed record GetMeQuery : IRequest<MeDto>;

public sealed class GetMeHandler(IRepository<User> users, ICurrentUser current) : IRequestHandler<GetMeQuery, MeDto>
{
    public async Task<MeDto> Handle(GetMeQuery q, CancellationToken ct)
    {
        var id = current.UserId;
        var u = await users.FindAsync(x => x.Id == id, ct) ?? throw new UnauthorizedException("Conta não encontrada.");
        return new MeDto(u.Id, u.Email, u.Name, u.CreatedAt, current.Scope);
    }
}

/// <summary>Exclui a conta e TUDO o que depende dela (cascade), encerrando o pareamento das extensões (RF-08).</summary>
public sealed record DeleteMeCommand : IRequest<Unit>;

public sealed class DeleteMeHandler(IRepository<User> users, ICurrentUser current, IUnitOfWork uow) : IRequestHandler<DeleteMeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteMeCommand cmd, CancellationToken ct)
    {
        var id = current.UserId;
        var u = await users.FindAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Conta não encontrada.");
        users.Remove(u);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>Exportação legível (RF-08): perfil, respostas aprovadas, consentimentos, registro de ações e demais dados da conta.</summary>
public sealed record ExportMeQuery : IRequest<AccountExport>;

public sealed record AccountExport(
    DateTime ExportedAt,
    MeDto Account,
    ProfileDto? Profile,
    IReadOnlyList<object> Consents,
    IReadOnlyList<object> ApprovedAnswers,
    IReadOnlyList<object> SensitiveData,
    IReadOnlyList<object> Applications,
    IReadOnlyList<object> AuditEvents,
    IReadOnlyList<object> ExtensionDevices,
    IReadOnlyList<object> AuthorizedSites,
    IReadOnlyList<object> ResumeImports,
    object? Billing,
    object? Onboarding,
    IReadOnlyList<object> HiddenAds);

public sealed class ExportMeHandler(
    IRepository<User> users,
    IProfileRepository profiles,
    IRepository<Consent> consents,
    IRepository<ApprovedAnswer> answers,
    IRepository<SensitiveData> sensitive,
    IRepository<JobApplication> applications,
    IRepository<AuditEvent> audit,
    IRepository<ExtensionDevice> devices,
    IRepository<AuthorizedSite> sites,
    IRepository<ResumeImport> imports,
    IRepository<Domain.Billing.Subscription> subscriptions,
    IRepository<Domain.Billing.UsagePack> packs,
    IRepository<Domain.Billing.MonthlyUsage> usages,
    IRepository<Domain.Onboarding.OnboardingProgress> onboarding,
    IRepository<Domain.Ads.AdHide> adHides,
    ISensitiveDataProtector protector,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<ExportMeQuery, AccountExport>
{
    public async Task<AccountExport> Handle(ExportMeQuery q, CancellationToken ct)
    {
        var id = current.UserId;
        var u = await users.FindAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Conta não encontrada.");
        audit.Add(AuditEvent.Record(id, AuditActions.DataExported, clock.UtcNow));
        await uow.SaveChangesAsync(ct);

        var profile = await profiles.GetByUserIdAsync(id, ct);
        return new AccountExport(
            clock.UtcNow,
            new MeDto(u.Id, u.Email, u.Name, u.CreatedAt, null),
            profile is null ? null : ProfileDto.From(profile),
            (await consents.ListAsync(x => x.UserId == id, ct)).Select(c => (object)new { c.Purpose, c.Granted, c.UpdatedAt }).ToList(),
            (await answers.ListAsync(x => x.UserId == id, ct)).Select(a => (object)new { a.Id, a.Question, a.Text, a.ApprovedAt, a.LastUsedAt }).ToList(),
            (await sensitive.ListAsync(x => x.UserId == id, ct)).Select(s => (object)new { s.Id, s.Kind, Value = protector.Unprotect(s.EncryptedValue), s.UpdatedAt }).ToList(),
            (await applications.ListAsync(x => x.UserId == id, ct)).Select(a => (object)new { a.Id, a.Portal, a.JobTitle, a.Company, a.JobUrl, a.Status, a.FieldsFilled, a.ConfirmedAt, a.Notes }).ToList(),
            (await audit.ListAsync(x => x.UserId == id, ct)).OrderBy(e => e.OccurredAt).Select(e => (object)new { e.Action, e.Portal, e.FieldCount, e.OccurredAt }).ToList(),
            (await devices.ListAsync(x => x.UserId == id, ct)).Select(d => (object)new { d.Id, d.Name, d.PairedAt, d.LastSeenAt, d.RevokedAt }).ToList(),
            (await sites.ListAsync(x => x.UserId == id, ct)).Select(s => (object)new { s.Id, s.Origin, s.CreatedAt }).ToList(),
            (await imports.ListAsync(x => x.UserId == id, ct)).Select(i => (object)new { i.Id, i.FileName, i.Status, i.CreatedAt }).ToList(),
            new
            {
                Subscription = (await subscriptions.FindAsync(x => x.UserId == id, ct)) is { } sub
                    ? new { sub.Plan, sub.Period, sub.StartedAt, sub.TrialEndsAt, sub.RenewsAt, sub.CanceledAt } : null,
                Packs = (await packs.ListAsync(x => x.UserId == id, ct)).Select(p => new { p.AssistedRemaining, p.AiRemaining, p.PurchasedAt, p.ExpiresAt }).ToList(),
                Usage = (await usages.ListAsync(x => x.UserId == id, ct)).Select(u => new { u.Month, u.AssistedUsed, u.AiUsed, u.JobAnalysesUsed, u.ResumeImportsUsed, u.BonusGranted, u.BonusUsed, u.RewardedClaims }).ToList(),
            },
            (await onboarding.FindAsync(x => x.UserId == id, ct)) is { } ob ? new { Answered = ob.AnsweredSteps, Skipped = ob.SkippedSteps, ob.LastStep, ob.CompletedAt } : null,
            (await adHides.ListAsync(x => x.UserId == id, ct)).Select(h => (object)new { h.AdId, h.Topic, h.Scope, h.HiddenAt, h.ExpiresAt }).ToList());
    }
}
