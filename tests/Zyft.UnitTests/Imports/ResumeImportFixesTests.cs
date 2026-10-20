using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging.Abstractions;
using Zyft.Application.Common.Text;
using Zyft.Application.Imports;
using Zyft.Infrastructure.Files;
using Zyft.UnitTests.Support;

namespace Zyft.UnitTests.Imports;

/// <summary>Importação de currículo: texto normalizado, certificações sem tradução e itens incompletos marcados.</summary>
public class ResumeImportFixesTests
{
    [Theory]
    [InlineData("Certiﬁcação em Excel", "Certificação em Excel")]          // ligadura ﬁ (U+FB01)
    [InlineData("Proﬁssional de ﬂuxo", "Profissional de fluxo")]         // ﬁ e ﬂ (U+FB02)
    [InlineData("Ofﬁce", "Office")]                                       // ligadura no meio da palavra
    [InlineData("AWS Certified", "AWS Certified")]                  // espaço não separável
    [InlineData("tra­balho​", "trabalho")]                     // hífen suave e largura zero
    public void Texto_extraido_e_normalizado_em_NFKC(string raw, string expected) =>
        TextTools.NormalizeExtracted(raw).Should().Be(expected);

    [Fact]
    public async Task Extrator_de_DOCX_devolve_texto_sem_ligaduras()
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body(new Paragraph(new Run(new Text("Certiﬁcações proﬁssionais")))));
        }

        var text = await new ResumeTextExtractor().ExtractAsync(ms.ToArray(), "cv.docx");
        text.Should().Be("Certificações profissionais");
    }

    [Theory]
    [InlineData("AWS Certified Cloud Practitioner", "Certification")]
    [InlineData("Microsoft Certified: Azure Fundamentals", "Certification")]
    [InlineData("Scrum Foundation Professional Certification", "Certification")]
    [InlineData("Certificação ITIL 4 Foundation", "Certification")]
    [InlineData("Certiﬁcação PMP", "Certification")]
    [InlineData("Excel Intermediário", "Course")]
    [InlineData("Certificado de conclusão: Atendimento ao cliente", "Course")]
    [InlineData(null, "Course")]
    public void Heuristica_classifica_certificacao_pelo_nome(string? name, string expected) =>
        ResumeParser.CourseKind(name).Should().Be(expected);

    [Fact]
    public async Task IA_mantem_o_nome_da_certificacao_e_o_curso_certificado_vira_Certificacao()
    {
        var llm = new ScriptedLlm("""
            {"personal":{"fullName":"Ana","email":"","phone":"","city":"","state":""},"experiences":[],
             "education":[{"institution":"UEM","course":"Administração","degree":"Bacharelado","start":"2022","end":"","status":""}],
             "courses":[{"name":"AWS Certified Cloud Practitioner","institution":"AWS","year":"2024","hours":""},{"name":"Excel Intermediário","institution":"SENAC","year":"2023","hours":"40h"}],
             "certifications":[{"name":"AWS Certified Cloud Practitioner","issuer":"Amazon","year":"2024"}],
             "skills":[],"languages":[{"language":"Inglês","level":""}]}
            """);
        var (p, ai) = await new ResumeParser(llm, NullLogger<ResumeParser>.Instance).ParseAsync("Ana\nAWS Certiﬁed Cloud Practitioner\nExcel Intermediário");

        ai.Should().BeTrue();
        llm.Requests.Single().System.Should().Contain("AWS Certified Cloud Practitioner").And.Contain("idioma original");
        llm.Requests.Single().Prompt.Should().Contain("AWS Certified Cloud Practitioner"); // ligadura desfeita antes da IA
        p.Courses.Should().HaveCount(2); // a certificação repetida em courses e certifications aparece uma vez
        p.Courses.Should().ContainSingle(c => c.Name == "AWS Certified Cloud Practitioner").Which.Type.Should().Be("Certification");
        p.Courses.Should().ContainSingle(c => c.Name == "Excel Intermediário").Which.Type.Should().Be("Course");

        // Formação sem situação: fica nula e marcada; nada é inventado.
        var edu = p.Educations.Single();
        edu.Level.Should().Be("Undergraduate");
        edu.Status.Should().BeNull();
        edu.MissingFields.Should().Equal("status");
        p.Languages.Single().MissingFields.Should().Equal("proficiency");
    }

    [Fact]
    public void Rotulo_nao_reconhecido_nao_vira_palpite()
    {
        var p = ProposalCompleteness.Mark(new ResumeProposal
        {
            Educations = { new ProposalEducation("UEM", "ADM", "Undergraduate", "Formado-ish", "2020-01", null) },
            Experiences = { new ProposalExperience("Empresa Y", "Assistente", null, null, true, null, null, null) },
        });
        p.Educations[0].Status.Should().BeNull();
        p.Educations[0].MissingFields.Should().Equal("status");
        p.Experiences[0].MissingFields.Should().Equal("startDate");

        var complete = ProposalCompleteness.Mark(new ResumeProposal { Educations = { new ProposalEducation("UEM", "ADM", "Graduação", "Cursando", "2020-01", null) } });
        complete.Educations[0].MissingFields.Should().BeNull();
        complete.Educations[0].Status.Should().Be("InProgress");
    }
}
