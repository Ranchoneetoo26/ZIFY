using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

/// <summary>Correções encontradas na gravação do vídeo de tour: erros legíveis, importação incompleta, teste grátis e rascunho vazio.</summary>
public class TourFixesTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    private async Task<HttpClient> NewUser() => factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);

    [Fact]
    public async Task Curso_sem_tipo_vira_Curso_e_erro_de_formato_aponta_o_campo_e_nao_o_parametro()
    {
        var c = await NewUser();
        var created = await c.PostAsJsonAsync("/api/v1/profile/courses", new { name = "Excel básico", type = (string?)null });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        (await created.JsonAsync()).GetProperty("type").GetString().Should().Be("Course");

        // Enum inválido: o 400 diz qual campo ("level"), sem a chave técnica do parâmetro da action ("cmd").
        var bad = await c.PostAsJsonAsync("/api/v1/profile/educations", new { institution = "UEM", course = "ADM", level = "Qualquer", status = "InProgress", startDate = "2022-02" });
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errors = (await bad.JsonAsync()).GetProperty("errors");
        errors.TryGetProperty("level", out _).Should().BeTrue();
        errors.TryGetProperty("cmd", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Aplicar_formacao_sem_situacao_e_recusado_com_mensagem_que_nomeia_o_item_e_nada_e_inventado()
    {
        var llm = new FakeLlm(_ => """
            {"personal":{"fullName":"Ana Souza","email":"","phone":"","city":"","state":""},"experiences":[],
             "education":[{"institution":"UEM","course":"Administração","degree":"Graduação","start":"02/2022","end":"","status":""}],
             "courses":[{"name":"AWS Certified Cloud Practitioner","institution":"AWS","year":"2024","hours":""}],
             "certifications":[],"skills":[],"languages":[]}
            """);
        using var f = new ZyftApiFactory { Llm = llm };
        var c = f.Authed((await TestApi.RegisterAsync(f.CreateClient())).AccessToken);
        var res = await c.PostAsync("/api/v1/resume-imports", ResumeImportTests.Upload(ResumeImportTests.Docx(), "cv.docx", "application/octet-stream"));
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var j = await res.JsonAsync();
        var edu = j.GetProperty("proposal").GetProperty("educations")[0];
        edu.TryGetProperty("status", out var st).Should().BeFalse(st.ToString()); // ausente: não inventa
        edu.GetProperty("missingFields").EnumerateArray().Select(x => x.GetString()).Should().Equal("status");
        var course = j.GetProperty("proposal").GetProperty("courses")[0];
        course.GetProperty("name").GetString().Should().Be("AWS Certified Cloud Practitioner");
        course.GetProperty("type").GetString().Should().Be("Certification");

        var id = j.GetProperty("id").GetGuid();
        var apply = await c.PostAsJsonAsync($"/api/v1/resume-imports/{id}/apply", new
        {
            educations = new[] { new { institution = "UEM", course = "Administração", level = "Undergraduate", status = (string?)null, startDate = "2022-02" } },
        });
        apply.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var msg = (await apply.JsonAsync()).GetProperty("errors").GetProperty("educations[0].status")[0].GetString();
        msg.Should().Contain("Administração").And.Contain("situação");

        // A pessoa escolhe a situação e a mesma importação (ainda pendente) é aplicada.
        var ok = await c.PostAsJsonAsync($"/api/v1/resume-imports/{id}/apply", new
        {
            educations = new[] { new { institution = "UEM", course = "Administração", level = "Undergraduate", status = "InProgress", startDate = "2022-02" } },
        });
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Teste_gratis_fica_indisponivel_depois_de_usado_mesmo_cancelando()
    {
        var c = await NewUser();
        var b = await (await c.GetAsync("/api/v1/billing")).JsonAsync();
        b.GetProperty("trialAvailable").GetBoolean().Should().BeTrue();
        b.GetProperty("trialDays").GetInt32().Should().Be(7);

        var sub = await (await c.PostAsJsonAsync("/api/v1/billing/subscribe", new { period = "monthly" })).JsonAsync();
        sub.GetProperty("trialEndsAt").ValueKind.Should().Be(JsonValueKind.String);
        sub.GetProperty("trialAvailable").GetBoolean().Should().BeFalse();

        (await (await c.PostAsync("/api/v1/billing/cancel", null)).JsonAsync()).GetProperty("trialAvailable").GetBoolean().Should().BeFalse();
        (await (await c.GetAsync("/api/v1/billing")).JsonAsync()).GetProperty("trialAvailable").GetBoolean().Should().BeFalse();

        var again = await (await c.PostAsJsonAsync("/api/v1/billing/subscribe", new { period = "monthly" })).JsonAsync();
        again.GetProperty("plan").GetString().Should().Be("plus");
        again.GetProperty("trialEndsAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Theory]
    [InlineData("""{"answer":"","usedFacts":[],"missingInfo":""}""")]
    [InlineData("""{"answer":"   ","usedFacts":[],"missingInfo":""}""")]
    [InlineData("""{"resposta":"texto em campo com outro nome","usedFacts":[]}""")]
    public async Task Rascunho_de_IA_vazio_volta_200_com_aviso_e_sem_texto_em_branco(string aiJson)
    {
        using var f = new ZyftApiFactory { Llm = new FakeLlm(_ => aiJson) };
        var c = f.Authed((await TestApi.RegisterAsync(f.CreateClient())).AccessToken);
        (await c.PutAsJsonAsync("/api/v1/consents/AiText", new { granted = true })).EnsureSuccessStatusCode();

        var res = await c.PostAsJsonAsync("/api/v1/forms/open-answers", new { question = "Por que você quer trabalhar conosco?", maxLength = 500 });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var j = await res.JsonAsync();
        j.GetProperty("kind").GetString().Should().Be("missing");
        (j.TryGetProperty("value", out var v) ? v.GetString() : null).Should().BeNull();
        j.GetProperty("note").GetString().Should().StartWith("A IA não conseguiu escrever um rascunho com os fatos do seu perfil; escreva do seu jeito.");
    }
}
