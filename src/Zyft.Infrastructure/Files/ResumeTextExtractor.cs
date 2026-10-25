using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Zyft.Application.Common.Interfaces;
using Zyft.Application.Common.Text;
using Zyft.Domain.Common;

namespace Zyft.Infrastructure.Files;

/// <summary>
/// Extrai texto em memória (o arquivo nunca é gravado em disco): PDF com PdfPig, DOCX com OpenXml. O texto sai
/// normalizado em NFKC (<see cref="TextTools.NormalizeExtracted"/>): ligaduras como "ﬁ" viram "fi".
/// </summary>
public sealed class ResumeTextExtractor : IResumeTextExtractor
{
    public Task<string> ExtractAsync(byte[] content, string fileName, CancellationToken ct = default)
    {
        try
        {
            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return Task.FromResult(TextTools.NormalizeExtracted(FromPdf(content)));
            if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) return Task.FromResult(TextTools.NormalizeExtracted(FromDocx(content)));
        }
        catch (Exception ex) when (ex is not DomainException and not OperationCanceledException)
        {
            throw new DomainException("import.unreadable", "Não foi possível ler o arquivo. Verifique se ele não está corrompido ou protegido por senha.");
        }

        throw new DomainException("import.unsupported", "Formato não suportado. Envie PDF ou DOCX.");
    }

    private static string FromPdf(byte[] content)
    {
        using var doc = PdfDocument.Open(content);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages()) sb.AppendLine(ContentOrderTextExtractor.GetText(page));
        return sb.ToString();
    }

    private static string FromDocx(byte[] content)
    {
        using var ms = new MemoryStream(content);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body is null) return string.Empty;
        return string.Join('\n', body.Descendants<Paragraph>().Select(p => p.InnerText));
    }
}
