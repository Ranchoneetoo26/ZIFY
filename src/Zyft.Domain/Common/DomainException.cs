namespace Zyft.Domain.Common;

/// <summary>Violação de regra de negócio (vira HTTP 422 na API).</summary>
public class DomainException : Exception
{
    public DomainException(string code, string message) : base(message) => Code = code;

    /// <summary>Código estável, legível por máquina (ex.: <c>experience.invalid_period</c>).</summary>
    public string Code { get; }
}
