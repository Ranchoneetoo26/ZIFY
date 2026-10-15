using Zyft.Domain.Common;

namespace Zyft.Domain.Applications;

/// <summary>Status herdados do protótipo. O MVP 1 cria tudo como <see cref="Confirmed"/> (ADR-0012).</summary>
public enum ApplicationStatus
{
    Confirmed = 1,
    InReview = 2,
    Interview = 3,
    Test = 4,
    Offer = 5,
    Hired = 6,
    Rejected = 7,
    Withdrawn = 8,
    NoResponse = 9,
    Archived = 10,
}

/// <summary>
/// Registro mínimo de conclusão (RF-19, ADR-0012): a pessoa enviou a candidatura no portal, por conta própria,
/// e confirmou na extensão. Chamado de <c>JobApplication</c> para não colidir com o namespace <c>Zyft.Application</c>.
/// </summary>
public sealed class JobApplication : Entity
{
    private JobApplication() { }

    public Guid UserId { get; private set; }
    public string Portal { get; private set; } = default!;
    public string JobTitle { get; private set; } = default!;
    public string? Company { get; private set; }
    public string? JobUrl { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public int FieldsFilled { get; private set; }
    public DateTime ConfirmedAt { get; private set; }
    public string? Notes { get; private set; }

    public static JobApplication Confirm(Guid userId, string portal, string jobTitle, string? company, string? jobUrl, int fieldsFilled, DateTime now)
    {
        if (fieldsFilled < 0)
            throw new DomainException("application.invalid_fields", "A quantidade de campos preenchidos não pode ser negativa.");
        return new JobApplication
        {
            UserId = userId,
            Portal = Guard.Required(portal, "application.portal_required", "Informe o portal."),
            JobTitle = Guard.Required(jobTitle, "application.title_required", "Informe o cargo."),
            Company = Guard.Optional(company),
            JobUrl = Guard.Optional(jobUrl),
            FieldsFilled = fieldsFilled,
            Status = ApplicationStatus.Confirmed,
            ConfirmedAt = now,
        };
    }

    public void Update(ApplicationStatus? status, string? notes)
    {
        if (status is { } s) Status = s;
        if (notes is not null) Notes = Guard.Optional(notes);
    }
}
