using Zyft.Domain.Common;

namespace Zyft.Domain.Answers;

/// <summary>Resposta aprovada para pergunta recorrente (RF-06). Sempre mostrada para revisão antes do reuso.</summary>
public sealed class ApprovedAnswer : Entity
{
    private ApprovedAnswer() { }

    public Guid UserId { get; private set; }
    public string Question { get; private set; } = default!;
    public string Text { get; private set; } = default!;
    public DateTime ApprovedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    public static ApprovedAnswer Approve(Guid userId, string question, string text, DateTime now)
    {
        var a = new ApprovedAnswer { UserId = userId };
        a.Update(question, text, now);
        return a;
    }

    public void Update(string question, string text, DateTime now)
    {
        Question = Guard.Required(question, "answer.question_required", "Informe a pergunta.");
        Text = Guard.Required(text, "answer.text_required", "Informe a resposta.");
        ApprovedAt = now;
    }

    public void MarkUsed(DateTime now) => LastUsedAt = now;
}
