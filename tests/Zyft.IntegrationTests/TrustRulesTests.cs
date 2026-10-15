using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

/// <summary>Regras inegociáveis (CLAUDE.md) verificadas pela API real.</summary>
public class TrustRulesTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    private async Task<HttpClient> UserWithProfile()
    {
        var c = factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);
        await c.PutAsJsonAsync("/api/v1/profile", new { fullName = "Ana Paula Souza", email = "ana@exemplo.com", phone = "(44) 99999-2222", workModel = "OnSite" });
        await c.PostAsJsonAsync("/api/v1/profile/experiences", new { company = "Empresa Y", role = "Assistente Administrativo", startDate = "2023-07", isCurrent = true, activities = "Controle de contas a pagar no ERP Totvs" });
        await c.PostAsJsonAsync("/api/v1/profile/skills", new { name = "Excel" });
        return c;
    }

    private static object[] Fields => new object[]
    {
        new { id = "nome", tag = "input", type = "text", label = "Nome completo" },
        new { id = "senha", tag = "input", type = "password", label = "Senha" },
        new { id = "senha2", tag = "input", type = "text", label = "Confirme a senha" },
        new { id = "cap", tag = "input", type = "text", label = "Digite o captcha" },
        new { id = "cv", tag = "input", type = "file", label = "Currículo (PDF)" },
        new { id = "termo", tag = "input", type = "checkbox", label = "Li e aceito os termos" },
        new { id = "raca", tag = "select", type = "select", label = "Raça/cor", options = new[] { new { value = "1", text = "Parda" } } },
    };

    [Fact]
    public async Task Mappings_nunca_devolvem_valor_para_senha_captcha_arquivo_ou_declaracao()
    {
        var c = await UserWithProfile();
        var res = await c.PostAsJsonAsync("/api/v1/forms/mappings", new { pageUrl = "https://vagas.exemplo.com/form", fields = Fields });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var maps = (await res.JsonAsync()).GetProperty("mappings").EnumerateArray().ToDictionary(m => m.GetProperty("fieldId").GetString()!);
        maps.Should().HaveCount(Fields.Length);
        maps["nome"].GetProperty("value").GetString().Should().Be("Ana Paula Souza");
        foreach (var id in new[] { "senha", "senha2", "cap", "cv", "termo", "raca" })
        {
            maps[id].TryGetProperty("value", out _).Should().BeFalse($"campo {id} não pode ter valor");
            maps[id].GetProperty("state").GetString().Should().Be("Blocked");
            maps[id].GetProperty("selected").GetBoolean().Should().BeFalse();
        }
    }

    [Fact]
    public async Task Mappings_devolvem_so_valores_dos_campos_pedidos()
    {
        var c = await UserWithProfile();
        var body = await (await c.PostAsJsonAsync("/api/v1/forms/mappings", new { fields = new[] { new { id = "e", label = "E-mail", type = "email" } } })).Content.ReadAsStringAsync();
        body.Should().Contain("ana@exemplo.com").And.NotContain("99999-2222").And.NotContain("Empresa Y").And.NotContain("Excel");
    }

    [Fact]
    public async Task Sensivel_bloqueado_sem_consentimento_e_desmarcado_com_consentimento()
    {
        var c = await UserWithProfile();
        (await c.GetAsync("/api/v1/sensitive-data")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await c.PostAsJsonAsync("/api/v1/sensitive-data", new { kind = "Ethnicity", value = "Parda" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await c.PutAsJsonAsync("/api/v1/consents/SensitiveData", new { granted = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await c.PostAsJsonAsync("/api/v1/sensitive-data", new { kind = "Ethnicity", value = "Parda" })).StatusCode.Should().Be(HttpStatusCode.Created);

        var maps = (await (await c.PostAsJsonAsync("/api/v1/forms/mappings", new { fields = Fields })).JsonAsync()).GetProperty("mappings");
        var raca = maps.EnumerateArray().Single(m => m.GetProperty("fieldId").GetString() == "raca");
        raca.GetProperty("value").GetString().Should().Be("1");
        raca.GetProperty("selected").GetBoolean().Should().BeFalse();

        // Desligar o consentimento apaga os dados sensíveis.
        await c.PutAsJsonAsync("/api/v1/consents/sensitive-data", new { granted = false });
        await c.PutAsJsonAsync("/api/v1/consents/SensitiveData", new { granted = true });
        (await (await c.GetAsync("/api/v1/sensitive-data")).JsonAsync()).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Analise_de_vaga_sem_consentimento_e_403()
    {
        var c = await UserWithProfile();
        (await c.PutAsJsonAsync("/api/v1/consents/JobAnalysis", new { granted = false })).EnsureSuccessStatusCode();
        var res = await c.PostAsJsonAsync("/api/v1/jobs/analyze", new { text = "Requisitos:\n- Excel" });
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await res.JsonAsync()).GetProperty("code").GetString().Should().Be("consent_required");
    }

    [Fact]
    public async Task Analise_de_vaga_com_consentimento_tem_evidencia_real_e_nenhuma_porcentagem()
    {
        var c = await UserWithProfile();
        (await c.PutAsJsonAsync("/api/v1/consents/JobAnalysis", new { granted = true })).EnsureSuccessStatusCode();
        var res = await c.PostAsJsonAsync("/api/v1/jobs/analyze", new { url = "https://vagas.exemplo.com/1", text = "Requisitos:\n- Experiência com contas a pagar\n- Conhecimento em SAP\n- CNH B" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await res.Content.ReadAsStringAsync();
        raw.Should().NotContain("%");
        var j = JsonDocument.Parse(raw).RootElement;
        var reqs = j.GetProperty("requirements").EnumerateArray().ToList();
        reqs.Should().HaveCount(3);
        reqs.Single(r => r.GetProperty("text").GetString()!.Contains("SAP")).GetProperty("status").GetString().Should().Be("nao_encontrado");
        reqs.Single(r => r.GetProperty("text").GetString()!.Contains("contas")).GetProperty("evidence").GetString().Should().Contain("contas a pagar");
        j.GetProperty("toConfirm").EnumerateArray().Should().Contain(x => x.GetProperty("label").GetString() == "CNH" && !x.GetProperty("hasValue").GetBoolean());
    }

    [Fact]
    public async Task Texto_livre_sem_consentimento_e_403()
    {
        var c = await UserWithProfile();
        await c.PutAsJsonAsync("/api/v1/consents/AiText", new { granted = false });
        (await c.PostAsJsonAsync("/api/v1/forms/open-answers", new { question = "Por que você?" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Texto_livre_com_IA_fora_devolve_unknown_sem_valor()
    {
        var c = await UserWithProfile();
        await c.PutAsJsonAsync("/api/v1/consents/AiText", new { granted = true });
        // maxLength 0 = sem limite (D-03): não pode dar 400.
        var j = await (await c.PostAsJsonAsync("/api/v1/forms/open-answers", new { question = "Por que você?", maxLength = 0 })).JsonAsync();
        j.GetProperty("kind").GetString().Should().Be("unknown");
        j.TryGetProperty("value", out _).Should().BeFalse();
        j.GetProperty("selected").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Conta_nova_nasce_com_todos_os_consentimentos_desligados()
    {
        var c = await UserWithProfile();
        var list = await (await c.GetAsync("/api/v1/consents")).JsonAsync();
        list.GetArrayLength().Should().Be(3);
        list.EnumerateArray().Should().OnlyContain(x => !x.GetProperty("granted").GetBoolean());
        (await c.PostAsJsonAsync("/api/v1/forms/open-answers", new { question = "Por que você?" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MaxLength_negativo_ou_fora_da_faixa_e_400()
    {
        var c = await UserWithProfile();
        await c.PutAsJsonAsync("/api/v1/consents/AiText", new { granted = true });
        (await c.PostAsJsonAsync("/api/v1/forms/open-answers", new { question = "Q?", maxLength = 5 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await c.PostAsJsonAsync("/api/v1/forms/open-answers", new { question = "Q?", maxLength = -1 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Consentimento_desconhecido_e_404()
    {
        var c = await UserWithProfile();
        (await c.PutAsJsonAsync("/api/v1/consents/Marketing", new { granted = true })).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

/// <summary>Com IA "maliciosa": tenta escrever valores e inventar evidência.</summary>
public class AiGuardTests
{
    [Fact]
    public async Task IA_que_tenta_escrever_valor_ou_apontar_senha_nao_consegue()
    {
        var llm = new FakeLlm(_ => """{"mappings":[{"fieldId":"x","action":"fill","profileKey":"personal.email","value":"hacker@mal.com"}]}""");
        using var f = new ZyftApiFactory { Llm = llm };
        var c = f.Authed((await TestApi.RegisterAsync(f.CreateClient())).AccessToken);
        await c.PutAsJsonAsync("/api/v1/profile", new { email = "ana@exemplo.com" });
        var j = await (await c.PostAsJsonAsync("/api/v1/forms/mappings", new
        {
            fields = new object[]
            {
                new { id = "x", label = "Como falamos com você por escrito" },
                new { id = "pw", type = "password", label = "Senha" },
            },
        })).JsonAsync();
        var maps = j.GetProperty("mappings").EnumerateArray().ToDictionary(m => m.GetProperty("fieldId").GetString()!);
        maps["x"].GetProperty("value").GetString().Should().Be("ana@exemplo.com");
        maps["x"].GetProperty("source").GetString().Should().Be("Ai");
        maps["pw"].TryGetProperty("value", out _).Should().BeFalse();
        llm.Prompts.Should().OnlyContain(p => !p.Contains("\"pw\""));
    }

    [Fact]
    public async Task IA_que_inventa_linha_de_evidencia_tem_o_atende_rebaixado()
    {
        var llm = new FakeLlm(r => r.Prompt.StartsWith("Extraia")
            ? """{"title":"X","company":"Y","location":"","contractType":"","schedule":"","salary":"","workModel":"","summary":"","requirements":[{"text":"Doutorado em física","kind":"obrigatório"}]}"""
            : """{"analysis":"inventado","status":"atende","line":4242}""");
        using var f = new ZyftApiFactory { Llm = llm };
        var c = f.Authed((await TestApi.RegisterAsync(f.CreateClient())).AccessToken);
        await c.PutAsJsonAsync("/api/v1/consents/JobAnalysis", new { granted = true });
        var j = await (await c.PostAsJsonAsync("/api/v1/jobs/analyze", new { text = "Vaga X" })).JsonAsync();
        var req = j.GetProperty("requirements")[0];
        req.GetProperty("status").GetString().Should().Be("nao_encontrado");
        req.GetProperty("evidence").GetString().Should().BeEmpty();
        j.GetProperty("aiUsed").GetBoolean().Should().BeTrue();
    }
}
