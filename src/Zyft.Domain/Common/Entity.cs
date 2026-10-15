namespace Zyft.Domain.Common;

/// <summary>Base de todas as entidades: chave Guid gerada no cliente e carimbos UTC.</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>Preenchido pela infraestrutura ao inserir (UTC).</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Preenchido pela infraestrutura ao inserir/alterar (UTC).</summary>
    public DateTime UpdatedAt { get; private set; }
}
