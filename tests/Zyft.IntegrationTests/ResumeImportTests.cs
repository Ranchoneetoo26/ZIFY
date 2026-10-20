using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

public class ResumeImportTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    private static readonly string[] CvLines =
    {
        "Ana Paula Souza", "ana.souza@email.com | (44) 99999-2222", "Competências", "Excel, Contas a pagar, Atendimento ao cliente", "Idiomas", "Inglês intermediário",
    };

    internal static byte[] Docx()
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body(CvLines.Select(l => new Paragraph(new Run(new Text(l))))));
        }

        return ms.ToArray();
    }

    private static byte[] Pdf()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(UglyToad.PdfPig.Content.PageSize.A4);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var y = 780;
        // Helvetica padrão (WinAnsi) — sem acentos para evitar glifos ausentes.
        foreach (var line in new[] { "Ana Paula Souza", "ana.souza@email.com", "Competencias", "Excel, Contas a pagar" })
        {
            page.AddText(line, 12, new UglyToad.PdfPig.Core.PdfPoint(50, y), font);
            y -= 20;
        }

        return builder.Build();
    }

    internal static MultipartFormDataContent Upload(byte[] bytes, string name, string type)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(type);
        content.Add(file, "file", name);
        return content;
    }

    [Fact]
    public async Task DOCX_vira_proposta_e_apply_aplica_so_o_aceito()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var res = await c.PostAsync("/api/v1/resume-imports", Upload(Docx(), "cv.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var j = await res.JsonAsync();
        j.GetProperty("status").GetString().Should().Be("Pending");
        var proposal = j.GetProperty("proposal");
        proposal.GetProperty("profile").GetProperty("email").GetString().Should().Be("ana.souza@email.com");
        proposal.GetProperty("skills").EnumerateArray().Select(s => s.GetProperty("name").GetString()).Should().Contain("Excel");
        var id = j.GetProperty("id").GetGuid();

        // A pessoa aceita só o telefone e a competência Excel (editada).
        var apply = await c.PostAsJsonAsync($"/api/v1/resume-imports/{id}/apply", new
        {
            profile = new { phone = "(44) 99999-2222" },
            skills = new[] { new { name = "Excel avançado" } },
            experiences = new[] { new { company = "Empresa Y", role = "Assistente", startDate = "2023-07-01", isCurrent = true } },
            educations = new[] { new { institution = "UEM", course = "ADM", level = "Undergraduate", status = "InProgress", startDate = "2022-02" } },
            languages = new[] { new { name = "Inglês", proficiency = "Intermediate" } },
        });
        apply.StatusCode.Should().Be(HttpStatusCode.OK);
        var p = (await apply.JsonAsync()).GetProperty("profile");
        p.GetProperty("phone").GetString().Should().Be("(44) 99999-2222");
        p.GetProperty("email").GetString().Should().NotBe("ana.souza@email.com"); // não foi aceito
        p.GetProperty("skills").EnumerateArray().Select(s => s.GetProperty("name").GetString()).Should().Equal("Excel avançado");
        p.GetProperty("educations")[0].GetProperty("level").GetString().Should().Be("Undergraduate");

        (await c.PostAsJsonAsync($"/api/v1/resume-imports/{id}/apply", new { })).StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PDF_com_texto_vira_proposta()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var res = await c.PostAsync("/api/v1/resume-imports", Upload(Pdf(), "cv.pdf", "application/pdf"));
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        (await res.JsonAsync()).GetProperty("proposal").GetProperty("profile").GetProperty("email").GetString().Should().Be("ana.souza@email.com");
    }

    [Fact]
    public async Task Arquivo_com_extensao_falsa_ou_formato_errado_e_400()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        (await c.PostAsync("/api/v1/resume-imports", Upload("isto não é pdf"u8.ToArray(), "cv.pdf", "application/pdf"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await c.PostAsync("/api/v1/resume-imports", Upload("texto"u8.ToArray(), "cv.txt", "text/plain"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await c.PostAsync("/api/v1/resume-imports", new MultipartFormDataContent())).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Arquivo_acima_de_10MB_e_recusado()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var big = new byte[10 * 1024 * 1024 + 10];
        "%PDF"u8.ToArray().CopyTo(big, 0);
        var res = await c.PostAsync("/api/v1/resume-imports", Upload(big, "cv.pdf", "application/pdf"));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await res.JsonAsync()).GetProperty("errors").GetProperty("content")[0].GetString().Should().Contain("10 MB");
    }

    [Fact]
    public async Task Descartar_nao_muda_o_perfil()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        var id = (await (await c.PostAsync("/api/v1/resume-imports", Upload(Docx(), "cv.docx", "application/octet-stream"))).JsonAsync()).GetProperty("id").GetGuid();
        var d = await c.PostAsync($"/api/v1/resume-imports/{id}/discard", null);
        (await d.JsonAsync()).GetProperty("status").GetString().Should().Be("Discarded");
        (await (await c.GetAsync("/api/v1/profile")).JsonAsync()).GetProperty("skills").GetArrayLength().Should().Be(0);
    }
}
