using FluentValidation;
using MediatR;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Profiles;

namespace Zyft.Application.Profiles;

internal static class ProfileLoader
{
    public static async Task<Profile> LoadAsync(this IProfileRepository repo, ICurrentUser current, CancellationToken ct) =>
        await repo.GetByUserIdAsync(current.UserId, ct) ?? throw new NotFoundException("Perfil não encontrado.");
}

// ------------------------------------------------------------------ GET /profile

public sealed record GetProfileQuery : IRequest<ProfileDto>;

public sealed class GetProfileHandler(IProfileRepository profiles, ICurrentUser current) : IRequestHandler<GetProfileQuery, ProfileDto>
{
    public async Task<ProfileDto> Handle(GetProfileQuery q, CancellationToken ct) => ProfileDto.From(await profiles.LoadAsync(current, ct));
}

// ------------------------------------------------------------------ PUT /profile

/// <summary>Atualiza os dados básicos (substitui todos os campos básicos; listas não mudam).</summary>
public sealed record UpdateProfileCommand : IRequest<ProfileDto>
{
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public DateOnly? BirthDate { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? ZipCode { get; init; }
    public string? LinkedinUrl { get; init; }
    public string? GithubUrl { get; init; }
    public string? PortfolioUrl { get; init; }
    public string? Headline { get; init; }
    public string? Summary { get; init; }
    public string? DesiredRoles { get; init; }
    public string? Areas { get; init; }
    public string? Seniority { get; init; }
    public WorkModel? WorkModel { get; init; }
    public string? LocationPreference { get; init; }
    public string? Availability { get; init; }
    public decimal? SalaryExpectation { get; init; }
    public bool? AvailableToTravel { get; init; }
    public bool? AvailableToRelocate { get; init; }
    public string? ContractTypes { get; init; }
    public bool? HasDriverLicense { get; init; }
    public string? DriverLicenseCategory { get; init; }

    public ProfileBasicData ToData() => new()
    {
        FullName = FullName, Email = Email, Phone = Phone, BirthDate = BirthDate, City = City, State = State, Country = Country, ZipCode = ZipCode,
        LinkedinUrl = LinkedinUrl, GithubUrl = GithubUrl, PortfolioUrl = PortfolioUrl, Headline = Headline, Summary = Summary,
        DesiredRoles = DesiredRoles, Areas = Areas, Seniority = Seniority, WorkModel = WorkModel, LocationPreference = LocationPreference,
        Availability = Availability, SalaryExpectation = SalaryExpectation, AvailableToTravel = AvailableToTravel, AvailableToRelocate = AvailableToRelocate,
        ContractTypes = ContractTypes, HasDriverLicense = HasDriverLicense, DriverLicenseCategory = DriverLicenseCategory,
    };
}

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(160).WithMessage("O nome deve ter no máximo 160 caracteres.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).WithMessage("Informe um e-mail válido.");
        RuleFor(x => x.Phone).MaximumLength(40).Matches(@"^[0-9()+\-\s.]*$").WithMessage("Telefone só pode ter números, espaços, parênteses, + e -.");
        RuleFor(x => x.State).MaximumLength(60);
        RuleFor(x => x.City).MaximumLength(120);
        RuleFor(x => x.Country).MaximumLength(80);
        RuleFor(x => x.ZipCode).MaximumLength(20);
        RuleFor(x => x.LinkedinUrl).OptionalUrl();
        RuleFor(x => x.GithubUrl).OptionalUrl();
        RuleFor(x => x.PortfolioUrl).OptionalUrl();
        RuleFor(x => x.Headline).MaximumLength(200);
        RuleFor(x => x.Summary).MaximumLength(4000).WithMessage("O resumo deve ter no máximo 4000 caracteres.");
        RuleFor(x => x.DesiredRoles).MaximumLength(500);
        RuleFor(x => x.Areas).MaximumLength(500);
        RuleFor(x => x.Seniority).MaximumLength(60);
        RuleFor(x => x.WorkModel).IsInEnum().WithMessage("Modelo de trabalho inválido.");
        RuleFor(x => x.Availability).MaximumLength(200);
        RuleFor(x => x.ContractTypes).MaximumLength(200);
        RuleFor(x => x.DriverLicenseCategory).MaximumLength(5);
        RuleFor(x => x.BirthDate).Must(d => d is null || (d.Value.Year >= 1900 && d.Value < DateOnly.FromDateTime(DateTime.UtcNow)))
            .WithMessage("Data de nascimento inválida.");
    }
}

public sealed class UpdateProfileHandler(IProfileRepository profiles, IRepository<Domain.Audit.AuditEvent> audit, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<UpdateProfileCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpdateProfileCommand cmd, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        p.UpdateBasics(cmd.ToData());
        audit.Add(AuditEvent.Record(current.UserId, AuditActions.ProfileUpdated, clock.UtcNow));
        await uow.SaveChangesAsync(ct);
        return ProfileDto.From(p);
    }
}

// ------------------------------------------------------------------ GET /profile/missing (RF-05)

public sealed record GetMissingQuery : IRequest<IReadOnlyList<MissingItem>>;

public sealed class GetMissingHandler(IProfileRepository profiles, ICurrentUser current) : IRequestHandler<GetMissingQuery, IReadOnlyList<MissingItem>>
{
    /// <summary>Perguntas curtas apenas sobre o que falta (porte de <c>missingQuestions</c>).</summary>
    public static readonly (string Key, string Question)[] Important =
    {
        ("personal.fullName", "Qual é o seu nome completo?"),
        ("personal.email", "Qual e-mail os recrutadores devem usar?"),
        ("personal.phone", "Qual é o seu telefone com DDD?"),
        ("personal.city", "Em qual cidade você mora?"),
        ("objective.desiredRoles", "Quais cargos você deseja? (separe por vírgula)"),
        ("objective.workModel", "Qual modelo de trabalho prefere? (presencial, híbrido, remoto)"),
        ("preferences.availability", "Quando pode começar? (ex.: imediata, 30 dias)"),
        ("preferences.salaryExpectation", "Qual a sua pretensão salarial?"),
        ("preferences.travel", "Tem disponibilidade para viagens?"),
        ("preferences.relocation", "Tem disponibilidade para mudança de cidade?"),
        ("preferences.driverLicense", "Possui CNH? Qual categoria? (ou \"Não possuo\")"),
        ("experiences", "Tem alguma experiência profissional para incluir? (pode ser informal ou estágio)"),
        ("education", "Qual é a sua formação mais recente?"),
    };

    public async Task<IReadOnlyList<MissingItem>> Handle(GetMissingQuery q, CancellationToken ct)
    {
        var p = await profiles.LoadAsync(current, ct);
        return Compute(p);
    }

    public static IReadOnlyList<MissingItem> Compute(Profile p)
    {
        var facts = ProfileFacts.Build(p);
        return Important
            .Where(i => i.Key switch
            {
                "experiences" => p.Experiences.Count == 0,
                "education" => p.Educations.Count == 0,
                _ => !facts.Contains(i.Key),
            })
            .Select(i => new MissingItem(i.Key, i.Question))
            .ToList();
    }
}
