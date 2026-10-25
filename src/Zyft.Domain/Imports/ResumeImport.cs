using Zyft.Domain.Common;

namespace Zyft.Domain.Imports;

public enum ResumeImportStatus { Pending = 1, Applied = 2, Discarded = 3 }

/// <summary>Importação de currículo (RF-03/04). O arquivo não é guardado; só a proposta, até ser aplicada ou descartada.</summary>
public sealed class ResumeImport : Entity
{
    private ResumeImport() { }

    public Guid UserId { get; private set; }
    public string FileName { get; private set; } = default!;
    public ResumeImportStatus Status { get; private set; }
    public string ProposalJson { get; private set; } = default!;

    public static ResumeImport Create(Guid userId, string fileName, string proposalJson) => new()
    {
        UserId = userId,
        FileName = Guard.Required(fileName, "import.file_required", "Arquivo sem nome."),
        ProposalJson = proposalJson,
        Status = ResumeImportStatus.Pending,
    };

    public void MarkApplied()
    {
        EnsurePending();
        Status = ResumeImportStatus.Applied;
        ProposalJson = "{}"; // a proposta não é mais necessária depois de aplicada
    }

    public void Discard()
    {
        EnsurePending();
        Status = ResumeImportStatus.Discarded;
        ProposalJson = "{}";
    }

    private void EnsurePending()
    {
        if (Status != ResumeImportStatus.Pending)
            throw new DomainException("import.not_pending", "Esta importação já foi aplicada ou descartada.");
    }
}
