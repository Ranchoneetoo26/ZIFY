using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Zyft.Application.Profiles;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

/// <summary>Regressões do QA de importação, respostas e candidaturas.</summary>
public class QaImportFixesTests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    private const string DocxType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    /// <summary>Um valor para CADA campo de PUT /profile. Campo novo sem valor aqui reprova o teste de preservação.</summary>
    private static readonly Dictionary<string, object> FullProfile = new()
    {
        ["fullName"] = "Maria Clara", ["email"] = "maria@exemplo.com", ["phone"] = "(44) 99999-1111", ["birthDate"] = "1990-05-02", ["city"] = "Maringá",
        ["state"] = "PR", ["country"] = "Brasil", ["zipCode"] = "87000-000", ["linkedinUrl"] = "https://linkedin.com/in/maria", ["githubUrl"] = "https://github.com/maria",
        ["portfolioUrl"] = "https://maria.dev", ["headline"] = "Enfermeira", ["summary"] = "Resumo", ["desiredRoles"] = "Enfermeira", ["areas"] = "Saúde", ["seniority"] = "Sênior",
        ["workModel"] = "Hybrid", ["locationPreference"] = "Maringá", ["availability"] = "Imediata", ["salaryExpectation"] = 5000.5m, ["availableToTravel"] = true,
        ["availableToRelocate"] = false, ["contractTypes"] = "CLT", ["hasDriverLicense"] = true, ["driverLicenseCategory"] = "B", ["workArea"] = "Saúde",
        ["professionalRegistration"] = "COREN-PR 123456", ["salaryType"] = "PJ",
    };

    private async Task<HttpClient> User() => factory.Authed((await TestApi.RegisterAsync(factory.CreateClient())).AccessToken);

    private static async Task<JsonElement> Import(HttpClient c)
    {
        var res = await c.PostAsync("/api/v1/resume-imports", ResumeImportTests.Upload(ResumeImportTests.Docx(), "cv.docx", DocxType));
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        return await res.JsonAsync();
    }

    private static async Task<(HttpResponseMessage Res, string Id)> Apply(HttpClient c, object body)
    {
        var id = (await Import(c)).GetProperty("id").GetString()!;
        return (await c.PostAsJsonAsync($"/api/v1/resume-imports/{id}/apply", body), id);
    }

    private static async Task<IReadOnlyList<string>> Errors(HttpResponseMessage res, string key)
    {
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        return Errors(await res.JsonAsync(), key);
    }

    private static IReadOnlyList<string> Errors(JsonElement problem, string key)
    {
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty(key, out var list).Should().BeTrue($"esperava erro em {key}: {errors}");
        return list.EnumerateArray().Select(x => x.GetString()!).ToList();
    }

    [Fact]
    public async Task Importar_preserva_todos_os_dados_basicos_que_a_proposta_nao_traz()
    {
        var names = typeof(UpdateProfileCommand).GetProperties().Where(p => p.Name != "EqualityContract").Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name));
        FullProfile.Keys.Should().BeEquivalentTo(names, "todo campo básico precisa entrar neste teste");

        var c = await User();
        (await c.PutAsJsonAsync("/api/v1/profile", FullProfile)).StatusCode.Should().Be(HttpStatusCode.OK);
        var before = await (await c.GetAsync("/api/v1/profile")).JsonAsync();

        var (res, _) = await Apply(c, new { profile = new { }, skills = new[] { new { name = "Excel" } } });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await (await c.GetAsync("/api/v1/profile")).JsonAsync();
        foreach (var key in FullProfile.Keys)
            after.GetProperty(key).GetRawText().Should().Be(before.GetProperty(key).GetRawText(), key);
    }

    [Fact]
    public async Task Basico_vazio_na_importacao_mantem_o_perfil_e_nome_vazio_e_recusado()
    {
        var c = await User();
        await c.PutAsJsonAsync("/api/v1/profile", new { fullName = "Maria", city = "Maringá" });

        (await Errors((await Apply(c, new { profile = new { fullName = "" } })).Res, "profile.fullName")).Should().Contain("Informe o nome completo.");
        (await Errors((await Apply(c, new { profile = new { fullName = "​​" } })).Res, "profile.fullName")).Should().Contain("Informe o nome completo.");
    }

    [Fact]
    public async Task Cidade_vazia_na_importacao_nao_apaga_a_cidade_do_perfil()
    {
        var c = await User();
        await c.PutAsJsonAsync("/api/v1/profile", new { fullName = "Maria", city = "Maringá" });

        var (res, _) = await Apply(c, new { profile = new { city = "  " } });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        (await res.JsonAsync()).GetProperty("profile").GetProperty("city").GetString().Should().Be("Maringá");
    }

    [Fact]
    public async Task URL_sem_esquema_recebe_https_e_outro_esquema_e_recusado()
    {
        var c = await User();
        var (ok, _) = await Apply(c, new { profile = new { linkedinUrl = "linkedin.com/in/maria" } });
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ok.JsonAsync()).GetProperty("profile").GetProperty("linkedinUrl").GetString().Should().Be("https://linkedin.com/in/maria");

        (await Errors((await Apply(c, new { profile = new { githubUrl = "ftp://x.com" } })).Res, "profile.githubUrl")).Should().Contain("Informe um endereço http(s) válido.");
    }

    [Fact]
    public async Task Apply_recusa_o_que_passa_dos_limites_sem_gravar_nada()
    {
        var c = await User();
        var longName = new string('a', 300);
        var (res, id) = await Apply(c, new
        {
            experiences = new[] { new { company = longName, role = "Cargo", startDate = "2020-01", activities = new string('b', 4001) } },
            courses = new[] { new { name = "Curso", hours = 10001 } },
            skills = new[] { new { name = new string('c', 101) } },
            profile = new { phone = "123", summary = new string('d', 4001) },
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await res.JsonAsync();
        Errors(problem, "experiences[0].company").Single().Should().Contain("Empresa").And.Contain("200");
        Errors(problem, "experiences[0].activities").Should().NotBeEmpty();
        Errors(problem, "courses[0].hours").Should().Contain("Carga horária muito alta.");
        Errors(problem, "skills[0].name").Should().NotBeEmpty();
        Errors(problem, "profile.phone").Should().NotBeEmpty();
        Errors(problem, "profile.summary").Should().NotBeEmpty();

        (await (await c.GetAsync("/api/v1/profile")).JsonAsync()).GetProperty("experiences").GetArrayLength().Should().Be(0);
        (await (await c.GetAsync($"/api/v1/resume-imports/{id}")).JsonAsync()).GetProperty("status").GetString().Should().Be("Pending");
    }

    [Fact]
    public async Task Apply_recusa_periodo_invertido_no_proprio_item()
    {
        var c = await User();
        var (res, _) = await Apply(c, new { experiences = new[] { new { company = "A", role = "B", startDate = "2022-01", endDate = "2021-01" } } });

        (await Errors(res, "experiences[0].endDate")).Should().Contain("A data de término não pode ser anterior à data de início.");
    }

    [Fact]
    public async Task Apply_informa_quantos_itens_ja_estavam_no_perfil()
    {
        var c = await User();
        await c.PostAsJsonAsync("/api/v1/profile/skills", new { name = "Excel" });

        var (res, _) = await Apply(c, new { skills = new[] { new { name = "excel" }, new { name = "Word" } } });

        var j = await res.JsonAsync();
        j.GetProperty("added").GetInt32().Should().Be(1);
        j.GetProperty("skipped").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Importacao_pendente_pode_ser_retomada_sem_gastar_cota()
    {
        var c = await User();
        (await c.GetAsync("/api/v1/resume-imports/pending")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var first = await Import(c);
        var second = await Import(c);
        async Task<int> Used() => (await (await c.GetAsync("/api/v1/billing")).JsonAsync()).GetProperty("usage").GetProperty("resumeImports").GetProperty("used").GetInt32();
        var used = await Used();

        var res = await c.GetAsync("/api/v1/resume-imports/pending");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var pending = await res.JsonAsync();
        pending.GetProperty("id").GetString().Should().Be(second.GetProperty("id").GetString());
        pending.GetProperty("proposal").GetProperty("profile").GetProperty("email").GetString().Should().Be("ana.souza@email.com");
        (await Used()).Should().Be(used);

        var other = await User();
        (await other.GetAsync("/api/v1/resume-imports/pending")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        await c.PostAsJsonAsync($"/api/v1/resume-imports/{second.GetProperty("id").GetString()}/discard", new { });
        (await (await c.GetAsync("/api/v1/resume-imports/pending")).JsonAsync()).GetProperty("id").GetString().Should().Be(first.GetProperty("id").GetString());
    }

    private async Task<(HttpClient Web, string Id)> ApplicationWithNotes(string? notes)
    {
        var web = await User();
        var created = await web.PostAsJsonAsync("/api/v1/forms/completions", new { portal = "vagas.exemplo.com", jobTitle = "Assistente", fieldsFilled = 3 });
        var id = (await created.JsonAsync()).GetProperty("id").GetString()!;
        (await web.PatchAsJsonAsync($"/api/v1/applications/{id}", new { notes })).StatusCode.Should().Be(HttpStatusCode.OK);
        return (web, id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Limpar_anotacoes_limpa_de_verdade(string? cleared)
    {
        var (web, id) = await ApplicationWithNotes("Entrevista sexta");

        (await web.PatchAsJsonAsync($"/api/v1/applications/{id}", new { status = "Interview", notes = cleared })).StatusCode.Should().Be(HttpStatusCode.OK);

        var j = await (await web.GetAsync($"/api/v1/applications/{id}")).JsonAsync();
        j.TryGetProperty("notes", out _).Should().BeFalse();
        j.GetProperty("status").GetString().Should().Be("Interview");
    }

    [Fact]
    public async Task Patch_sem_notes_nao_mexe_nas_anotacoes()
    {
        var (web, id) = await ApplicationWithNotes("Entrevista sexta");

        await web.PatchAsJsonAsync($"/api/v1/applications/{id}", new { status = "Interview" });

        (await (await web.GetAsync($"/api/v1/applications/{id}")).JsonAsync()).GetProperty("notes").GetString().Should().Be("Entrevista sexta");
        (await Errors(await web.PatchAsJsonAsync($"/api/v1/applications/{id}", new { }), "body")).Should().Contain("Informe o status ou as anotações.");
    }

    [Fact]
    public async Task Mesma_pergunta_substitui_pelo_mesmo_id()
    {
        var c = await User();
        var a = await (await c.PostAsJsonAsync("/api/v1/answers", new { question = "Por que nós?", text = "A" })).JsonAsync();
        var b = await (await c.PostAsJsonAsync("/api/v1/answers", new { question = " Por que nós? ", text = "B" })).JsonAsync();

        b.GetProperty("id").GetString().Should().Be(a.GetProperty("id").GetString());
        var list = await (await c.GetAsync("/api/v1/answers")).JsonAsync();
        list.GetArrayLength().Should().Be(1);
        list[0].GetProperty("text").GetString().Should().Be("B");
    }

    [Fact]
    public async Task Editar_para_a_pergunta_de_outra_resposta_da_erro_claro()
    {
        var c = await User();
        await c.PostAsJsonAsync("/api/v1/answers", new { question = "Pergunta A", text = "A" });
        var b = await (await c.PostAsJsonAsync("/api/v1/answers", new { question = "Pergunta B", text = "B" })).JsonAsync();
        var id = b.GetProperty("id").GetString();

        var res = await c.PutAsJsonAsync($"/api/v1/answers/{id}", new { question = "Pergunta A", text = "C" });

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var j = await res.JsonAsync();
        j.GetProperty("code").GetString().Should().Be("answer.duplicated");
        j.GetProperty("detail").GetString().Should().Contain("já tem uma resposta para esta pergunta");
        (await c.PutAsJsonAsync($"/api/v1/answers/{id}", new { question = "Pergunta B", text = "C" })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("​​​")]
    [InlineData("﻿ ‍")]
    [InlineData("ㅤ")]
    public async Task Texto_so_com_invisiveis_e_recusado(string invisible)
    {
        var c = await User();
        (await Errors(await c.PostAsJsonAsync("/api/v1/answers", new { question = invisible, text = "A" }), "question")).Should().Contain("Informe a pergunta.");
        (await Errors(await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new { name = invisible, email = "x@exemplo.com", password = "Senha123" }), "name"))
            .Should().Contain("Informe o nome.");
    }

    [Theory]
    [InlineData("a@b..com")]
    [InlineData("a@b.co.")]
    [InlineData("<b>@x.com")]
    [InlineData("a@b")]
    public async Task Email_malformado_e_recusado(string email)
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/v1/auth/register", new { name = "Pessoa", email, password = "Senha123" });
        (await Errors(res, "email")).Should().Contain("Informe um e-mail válido.");

        var c = await User();
        (await Errors(await c.PutAsJsonAsync("/api/v1/profile", new { email }), "email")).Should().Contain("Informe um e-mail válido.");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1000000000)]
    [InlineData(1234.567)]
    public async Task Pretensao_fora_da_faixa_ou_com_3_casas_e_recusada(double salary)
    {
        var c = await User();
        await Errors(await c.PutAsJsonAsync("/api/v1/profile", new { salaryExpectation = (decimal)salary }), "salaryExpectation");
    }

    [Fact]
    public async Task Limites_de_perfil_telefone_e_horas()
    {
        var c = await User();
        (await c.PutAsJsonAsync("/api/v1/profile", new { salaryExpectation = 999999999.99m })).StatusCode.Should().Be(HttpStatusCode.OK);
        await Errors(await c.PutAsJsonAsync("/api/v1/profile", new { phone = "123" }), "phone");
        await Errors(await c.PutAsJsonAsync("/api/v1/profile", new { phone = "(44) 99999-1111 ramal" }), "phone");
        (await c.PutAsJsonAsync("/api/v1/profile", new { phone = "+55 44 99999-1111" })).StatusCode.Should().Be(HttpStatusCode.OK);

        (await Errors(await c.PostAsJsonAsync("/api/v1/profile/courses", new { name = "Curso", hours = 0 }), "hours")).Should().Contain("Informe um número de horas maior que zero.");
        await Errors(await c.PostAsJsonAsync("/api/v1/profile/courses", new { name = "Curso", hours = 10001 }), "hours");
        (await Errors(await c.PostAsJsonAsync("/api/v1/profile/courses", new { name = "Curso", hours = 1.5 }), "hours")).Should().Contain("Use um número inteiro de horas, de 1 a 10000.");
    }

    [Fact]
    public async Task Mensagem_padrao_sai_em_portugues_com_o_nome_do_campo()
    {
        var c = await User();
        var origin = "https://" + new string('a', 300) + ".com";

        var message = (await Errors(await c.PostAsJsonAsync("/api/v1/authorized-sites", new { origin }), "origin")).First();

        message.Should().Contain("Endereço do site").And.Contain("300").And.NotContain("Origin").And.NotContain("characters");
    }
}
