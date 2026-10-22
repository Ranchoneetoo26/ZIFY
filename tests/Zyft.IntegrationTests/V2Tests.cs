using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Zyft.IntegrationTests.Support;

namespace Zyft.IntegrationTests;

/// <summary>Contrato v2: campos de perfil novos, campos personalizados, catálogo, onboarding, planos/limites e anúncios.</summary>
public class V2Tests(ZyftApiFactory factory) : IClassFixture<ZyftApiFactory>
{
    private async Task<(HttpClient Web, TestApi.Session S)> NewUser()
    {
        var s = await TestApi.RegisterAsync(factory.CreateClient());
        return (factory.Authed(s.AccessToken), s);
    }

    private static object Map(string page, string label = "Nome") => new { pageUrl = page, fields = new[] { new { id = "a", label } } };

    // ------------------------------------------------------------ perfil

    [Fact]
    public async Task Perfil_tem_area_registro_tipo_de_salario_e_campos_personalizados()
    {
        var (c, _) = await NewUser();
        var put = await c.PutAsJsonAsync("/api/v1/profile", new { fullName = "Beatriz Lima", workArea = "Saúde", professionalRegistration = "COREN-MG 123456", salaryType = "Por plantão" });
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        (await c.PutAsJsonAsync("/api/v1/profile", new { salaryType = "Mesada" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var created = await c.PostAsJsonAsync("/api/v1/profile/custom-fields", new { label = "Disponível para plantão noturno", value = "Sim, 12x36" });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await created.JsonAsync()).GetProperty("id").GetGuid();

        var p = await (await c.GetAsync("/api/v1/profile")).JsonAsync();
        p.GetProperty("workArea").GetString().Should().Be("Saúde");
        p.GetProperty("professionalRegistration").GetString().Should().Be("COREN-MG 123456");
        p.GetProperty("salaryType").GetString().Should().Be("Por plantão");
        var cf = p.GetProperty("customFields").EnumerateArray().Single();
        cf.GetProperty("label").GetString().Should().Be("Disponível para plantão noturno");
        cf.TryGetProperty("updatedAt", out _).Should().BeTrue();

        (await (await c.PutAsJsonAsync($"/api/v1/profile/custom-fields/{id}", new { label = "Plantão noturno", value = "Não" })).JsonAsync())
            .GetProperty("value").GetString().Should().Be("Não");
        (await (await c.GetAsync("/api/v1/profile/custom-fields")).JsonAsync()).GetArrayLength().Should().Be(1);
        (await c.DeleteAsync($"/api/v1/profile/custom-fields/{id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await c.DeleteAsync($"/api/v1/profile/custom-fields/{id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var audit = await (await c.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync();
        audit.Should().Contain("custom_field.changed").And.NotContain("plantão", "auditoria nunca guarda rótulo nem valor");
    }

    [Theory]
    [InlineData("Religião")]
    [InlineData("CPF")]
    [InlineData("Tem alguma deficiência")]
    public async Task Campo_personalizado_sensivel_da_422(string label)
    {
        var (c, _) = await NewUser();
        var res = await c.PostAsJsonAsync("/api/v1/profile/custom-fields", new { label, value = "x" });
        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var j = await res.JsonAsync();
        j.GetProperty("code").GetString().Should().Be("profile.custom_field_sensitive");
        j.GetProperty("detail").GetString().Should().Contain("Dados sensíveis");
    }

    [Fact]
    public async Task Campo_personalizado_e_validado_e_entra_no_mapeamento_com_chave_custom()
    {
        var (c, _) = await NewUser();
        (await c.PostAsJsonAsync("/api/v1/profile/custom-fields", new { label = new string('a', 81), value = "x" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var id = (await (await c.PostAsJsonAsync("/api/v1/profile/custom-fields", new { label = "Disponível para plantão noturno", value = "Sim" })).JsonAsync()).GetProperty("id").GetGuid();
        var m = (await (await c.PostAsJsonAsync("/api/v1/forms/mappings", Map("https://vagas.exemplo.com/f", "Disponível para plantão noturno"))).JsonAsync())
            .GetProperty("mappings")[0];
        m.GetProperty("profileKey").GetString().Should().Be($"custom:{id}");
        m.GetProperty("value").GetString().Should().Be("Sim");
    }

    [Fact]
    public async Task Catalogo_de_areas_igual_ao_mockup()
    {
        var (c, _) = await NewUser();
        var areas = (await (await c.GetAsync("/api/v1/catalog/areas")).JsonAsync()).EnumerateArray().ToList();
        areas.Select(a => a.GetProperty("name").GetString()).Should().Equal("Tecnologia", "Saúde", "Vendas e comercial", "Administrativo e financeiro",
            "Educação", "Logística e operações", "Jurídico", "Outra área");
        var saude = areas.Single(a => a.GetProperty("id").GetString() == "saude");
        saude.GetProperty("registration").GetProperty("label").GetString().Should().Be("Registro no conselho");
        saude.GetProperty("linkLabel").GetString().Should().Be("Currículo Lattes");
        areas.Single(a => a.GetProperty("id").GetString() == "vendas-e-comercial").GetProperty("registration").ValueKind.Should().Be(JsonValueKind.Null);
        (await factory.CreateClient().GetAsync("/api/v1/catalog/areas")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ------------------------------------------------------------ onboarding

    [Fact]
    public async Task Onboarding_guarda_progresso_e_conclusao_gera_evento()
    {
        var (c, _) = await NewUser();
        var empty = await (await c.GetAsync("/api/v1/onboarding")).JsonAsync();
        empty.GetProperty("answered").GetArrayLength().Should().Be(0);
        empty.GetProperty("lastStep").ValueKind.Should().Be(JsonValueKind.Null);
        empty.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.Null);

        var put = await (await c.PutAsJsonAsync("/api/v1/onboarding", new { answered = new[] { "nome", "area" }, skipped = new[] { "links" }, lastStep = "links", completed = false })).JsonAsync();
        put.GetProperty("skipped").EnumerateArray().Select(x => x.GetString()).Should().Equal("links");
        (await c.PutAsJsonAsync("/api/v1/onboarding", new { answered = new[] { "nao-existe" } })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var done = await (await c.PutAsJsonAsync("/api/v1/onboarding", new { answered = new[] { "nome", "area", "links" }, skipped = Array.Empty<string>(), lastStep = "combinados", completed = true })).JsonAsync();
        done.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.String);
        (await (await c.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync()).Should().Contain("onboarding.completed");
    }

    // ------------------------------------------------------------ planos e limites

    [Fact]
    public async Task Billing_padrao_e_gratuito_com_precos_e_limites()
    {
        var (c, _) = await NewUser();
        var b = await (await c.GetAsync("/api/v1/billing")).JsonAsync();
        b.GetProperty("plan").GetString().Should().Be("free");
        b.GetProperty("period").ValueKind.Should().Be(JsonValueKind.Null);
        b.GetProperty("demo").GetBoolean().Should().BeTrue();
        b.GetProperty("prices").GetProperty("plusMonthly").GetDecimal().Should().Be(9.90m);
        b.GetProperty("prices").GetProperty("plusYearly").GetDecimal().Should().Be(99m);
        b.GetProperty("prices").GetProperty("pack").GetDecimal().Should().Be(4.90m);
        var u = b.GetProperty("usage");
        u.GetProperty("month").GetString().Should().MatchRegex(@"^\d{4}-\d{2}$");
        u.GetProperty("assistedApplications").GetProperty("limit").GetInt32().Should().Be(10);
        u.GetProperty("aiSuggestions").GetProperty("limit").GetInt32().Should().Be(5);
        u.GetProperty("jobAnalyses").GetProperty("limit").GetInt32().Should().Be(10);
        u.GetProperty("resumeImports").GetProperty("limit").GetInt32().Should().Be(2);
        b.GetProperty("answersLimit").GetInt32().Should().Be(15);
        b.GetProperty("devicesLimit").GetInt32().Should().Be(1);
        b.GetProperty("rewarded").GetProperty("maxPerMonth").GetInt32().Should().Be(3);
        b.GetProperty("rewarded").GetProperty("creditsPerClaim").GetInt32().Should().Be(3);
        b.GetProperty("adsEnabled").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Candidatura_assistida_conta_uma_vez_por_pagina_e_402_depois_do_limite_e_bonus()
    {
        var (c, _) = await NewUser();
        for (var i = 0; i < 10; i++)
            (await c.PostAsJsonAsync("/api/v1/forms/mappings", Map($"https://vagas.exemplo.com/vaga/{i}?utm=x"))).StatusCode.Should().Be(HttpStatusCode.OK);
        // Mesma página (query diferente) não conta de novo.
        (await c.PostAsJsonAsync("/api/v1/forms/mappings", Map("https://vagas.exemplo.com/vaga/3?outra=1"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var over = await c.PostAsJsonAsync("/api/v1/forms/mappings", Map("https://vagas.exemplo.com/vaga/nova"));
        over.StatusCode.Should().Be((HttpStatusCode)402);
        var j = await over.JsonAsync();
        j.GetProperty("code").GetString().Should().Be("plan.limit_reached");
        j.GetProperty("resource").GetString().Should().Be("assistedApplications");
        j.GetProperty("used").GetInt32().Should().Be(10);
        j.GetProperty("limit").GetInt32().Should().Be(10);
        j.GetProperty("extensions").GetProperty("resource").GetString().Should().Be("assistedApplications");

        // Recompensado: +3; depois de 3 resgates, 409.
        var claimed = await (await c.PostAsync("/api/v1/ads/rewarded/claim", null)).JsonAsync();
        claimed.GetProperty("usage").GetProperty("assistedApplications").GetProperty("bonus").GetInt32().Should().Be(3);
        (await c.PostAsJsonAsync("/api/v1/forms/mappings", Map("https://vagas.exemplo.com/vaga/nova"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await c.PostAsync("/api/v1/ads/rewarded/claim", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await c.PostAsync("/api/v1/ads/rewarded/claim", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var exhausted = await c.PostAsync("/api/v1/ads/rewarded/claim", null);
        exhausted.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await exhausted.JsonAsync()).GetProperty("code").GetString().Should().Be("ads.rewarded_exhausted");

        var b = await (await c.GetAsync("/api/v1/billing")).JsonAsync();
        b.GetProperty("rewarded").GetProperty("claimedThisMonth").GetInt32().Should().Be(3);
        b.GetProperty("usage").GetProperty("assistedApplications").GetProperty("bonus").GetInt32().Should().Be(8);
        (await (await c.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync()).Should().Contain("rewarded.claimed");
    }

    [Fact]
    public async Task Pacote_avulso_libera_sugestoes_de_IA_depois_do_limite()
    {
        var (c, _) = await NewUser();
        await c.PutAsJsonAsync("/api/v1/consents/AiText", new { granted = true });
        for (var i = 0; i < 5; i++)
            (await c.PostAsJsonAsync("/api/v1/forms/open-answers", new { question = $"Pergunta {i}?" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var over = await c.PostAsJsonAsync("/api/v1/forms/open-answers", new { question = "Mais uma?" });
        over.StatusCode.Should().Be((HttpStatusCode)402);
        (await over.JsonAsync()).GetProperty("resource").GetString().Should().Be("aiSuggestions");

        var pack = await c.PostAsync("/api/v1/billing/packs", null);
        pack.StatusCode.Should().Be(HttpStatusCode.Created);
        (await pack.JsonAsync()).GetProperty("packs")[0].GetProperty("aiRemaining").GetInt32().Should().Be(10);
        (await c.PostAsJsonAsync("/api/v1/forms/open-answers", new { question = "Mais uma?" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await (await c.GetAsync("/api/v1/billing")).JsonAsync()).GetProperty("packs")[0].GetProperty("aiRemaining").GetInt32().Should().Be(9);
        (await (await c.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync()).Should().Contain("pack.purchased");
    }

    [Fact]
    public async Task Plus_remove_limites_e_anuncios_e_cancelar_volta_ao_gratuito()
    {
        var (c, _) = await NewUser();
        (await c.PostAsJsonAsync("/api/v1/billing/subscribe", new { period = "semanal" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var sub = await (await c.PostAsJsonAsync("/api/v1/billing/subscribe", new { period = "yearly" })).JsonAsync();
        sub.GetProperty("plan").GetString().Should().Be("plus");
        sub.GetProperty("period").GetString().Should().Be("yearly");
        sub.GetProperty("trialEndsAt").ValueKind.Should().Be(JsonValueKind.String);
        sub.GetProperty("usage").GetProperty("assistedApplications").GetProperty("limit").ValueKind.Should().Be(JsonValueKind.Null);
        sub.GetProperty("answersLimit").ValueKind.Should().Be(JsonValueKind.Null);
        sub.GetProperty("devicesLimit").GetInt32().Should().Be(3);
        sub.GetProperty("adsEnabled").GetBoolean().Should().BeFalse();

        for (var i = 0; i < 12; i++)
            (await c.PostAsJsonAsync("/api/v1/forms/mappings", Map($"https://vagas.exemplo.com/p/{i}"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await c.GetAsync("/api/v1/ads?placement=web.profile.rail")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await c.PostAsync("/api/v1/ads/rewarded/claim", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        var cancel = await (await c.PostAsync("/api/v1/billing/cancel", null)).JsonAsync();
        cancel.GetProperty("plan").GetString().Should().Be("free");
        (await c.PostAsJsonAsync("/api/v1/forms/mappings", Map("https://vagas.exemplo.com/p/99"))).StatusCode.Should().Be((HttpStatusCode)402);
        (await (await c.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync()).Should().Contain("plan.changed");
    }

    [Fact]
    public async Task Banco_de_respostas_e_navegadores_respeitam_o_plano()
    {
        var (c, _) = await NewUser();
        for (var i = 0; i < 15; i++)
            (await c.PostAsJsonAsync("/api/v1/answers", new { question = $"Pergunta {i}?", text = "Resposta." })).StatusCode.Should().Be(HttpStatusCode.Created);
        var over = await c.PostAsJsonAsync("/api/v1/answers", new { question = "Pergunta 16?", text = "Resposta." });
        over.StatusCode.Should().Be((HttpStatusCode)402);
        (await over.JsonAsync()).GetProperty("resource").GetString().Should().Be("answers");
        // Reaprovar uma pergunta existente substitui o texto e não conta.
        (await c.PostAsJsonAsync("/api/v1/answers", new { question = "Pergunta 1?", text = "Nova." })).StatusCode.Should().Be(HttpStatusCode.Created);

        async Task<HttpResponseMessage> Pair(string name)
        {
            var code = (await (await c.PostAsync("/api/v1/extension/pairing-codes", null)).JsonAsync()).GetProperty("code").GetString();
            return await factory.CreateClient().PostAsJsonAsync("/api/v1/extension/pair", new { code, deviceName = name });
        }

        (await Pair("Chrome")).StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await Pair("Edge");
        second.StatusCode.Should().Be((HttpStatusCode)402);
        (await second.JsonAsync()).GetProperty("resource").GetString().Should().Be("devices");
    }

    // ------------------------------------------------------------ anúncios

    [Fact]
    public async Task Anuncio_por_area_sem_dados_pessoais_e_ocultar_e_clique()
    {
        var (c, s) = await NewUser();
        await c.PutAsJsonAsync("/api/v1/profile", new { fullName = "Beatriz Privada Lima", phone = "(31) 98877-1020", email = "beatriz.privada@exemplo.com", workArea = "Saúde", desiredRoles = "Técnico(a) de enfermagem" });

        var res = await c.GetAsync("/api/v1/ads?placement=web.profile.rail");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadAsStringAsync();
        body.Should().NotContain("Beatriz").And.NotContain("98877").And.NotContain("beatriz.privada").And.NotContain(s.Email);
        var ad = JsonDocument.Parse(body).RootElement;
        ad.GetProperty("placement").GetString().Should().Be("web.profile.rail");
        ad.GetProperty("format").GetString().Should().Be("native");
        ad.GetProperty("advertiser").GetString().Should().Be("Anunciante de demonstração");
        ad.GetProperty("reason").GetString().Should().NotBeNullOrWhiteSpace();
        ad.GetProperty("imageUrl").ValueKind.Should().Be(JsonValueKind.Null);
        ad.GetProperty("id").GetString().Should().StartWith("demo-sau-");
        var id = ad.GetProperty("id").GetString();

        (await c.PostAsync($"/api/v1/ads/{id}/click", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await c.PostAsJsonAsync($"/api/v1/ads/{id}/hide", new { scope = "ad" })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var next = await c.GetAsync("/api/v1/ads?placement=web.profile.rail");
        if (next.StatusCode == HttpStatusCode.OK) (await next.JsonAsync()).GetProperty("id").GetString().Should().NotBe(id);
        (await c.PostAsJsonAsync("/api/v1/ads/nao-existe/hide", new { scope = "ad" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await c.PostAsJsonAsync($"/api/v1/ads/{id}/hide", new { scope = "tudo" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await (await c.GetAsync("/api/v1/audit-events")).Content.ReadAsStringAsync()).Should().Contain("ad.hidden");
    }

    [Theory]
    [InlineData("web.onboarding")]
    [InlineData("web.sensitive-data")]
    [InlineData("ext.form.review")]
    [InlineData("web.account")]
    [InlineData("web.resume-import")]
    public async Task Espacos_proibidos_respondem_204(string placement)
    {
        var (c, _) = await NewUser();
        (await c.GetAsync($"/api/v1/ads?placement={placement}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Contextual_liga_ao_requisito_e_ocultar_assunto_vale_sempre()
    {
        var (c, _) = await NewUser();
        await c.PutAsJsonAsync("/api/v1/profile", new { workArea = "Tecnologia" });
        (await c.GetAsync("/api/v1/ads")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await c.GetAsync("/api/v1/ads?placement=ext.job.contextual")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var ad = await (await c.GetAsync("/api/v1/ads?placement=ext.job.contextual&topic=" + Uri.EscapeDataString("Experiência com SQL e bancos relacionais"))).JsonAsync();
        ad.GetProperty("format").GetString().Should().Be("contextual");
        ad.GetProperty("topic").GetString().Should().Contain("SQL");
        var id = ad.GetProperty("id").GetString();

        (await c.PostAsJsonAsync($"/api/v1/ads/{id}/hide", new { scope = "topic" })).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var after = await c.GetAsync("/api/v1/ads?placement=ext.job.contextual&topic=" + Uri.EscapeDataString("Experiência com SQL e bancos relacionais"));
        after.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Extensao_pode_ler_billing_e_anuncios_mas_nao_assinar()
    {
        var (web, _) = await NewUser();
        var code = (await (await web.PostAsync("/api/v1/extension/pairing-codes", null)).JsonAsync()).GetProperty("code").GetString();
        var pair = await (await factory.CreateClient().PostAsJsonAsync("/api/v1/extension/pair", new { code, deviceName = "Chrome" })).JsonAsync();
        var ext = factory.Authed(pair.GetProperty("tokens").GetProperty("accessToken").GetString()!);
        (await ext.GetAsync("/api/v1/billing")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ext.GetAsync("/api/v1/ads?placement=ext.panel.banner")).StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        (await ext.PostAsJsonAsync("/api/v1/billing/subscribe", new { period = "monthly" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ext.GetAsync("/api/v1/onboarding")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------ correções pós-integração

    [Fact]
    public async Task Limite_atingido_nunca_bloqueia_exportar_nem_excluir_os_dados()
    {
        // TN-14 / RN-12: privacidade não é recurso pago.
        var (c, _) = await NewUser();
        for (var i = 0; i < 10; i++)
            await c.PostAsJsonAsync("/api/v1/forms/mappings", Map($"https://vagas.exemplo.com/limite/{i}"));
        (await c.PostAsJsonAsync("/api/v1/forms/mappings", Map("https://vagas.exemplo.com/limite/x"))).StatusCode.Should().Be((HttpStatusCode)402);

        (await c.GetAsync("/api/v1/me/export")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await c.GetAsync("/api/v1/consents")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await c.DeleteAsync("/api/v1/me")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Extensao_que_se_desconecta_libera_a_vaga_de_navegador_e_nao_compra_pacote()
    {
        var (web, _) = await NewUser();
        async Task<HttpResponseMessage> Pair(string name)
        {
            var code = (await (await web.PostAsync("/api/v1/extension/pairing-codes", null)).JsonAsync()).GetProperty("code").GetString();
            return await factory.CreateClient().PostAsJsonAsync("/api/v1/extension/pair", new { code, deviceName = name });
        }

        var first = await (await Pair("Chrome")).JsonAsync();
        var ext = factory.Authed(first.GetProperty("tokens").GetProperty("accessToken").GetString()!);
        (await ext.PostAsync("/api/v1/billing/packs", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await ext.DeleteAsync("/api/v1/extension/devices/current")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ext.GetAsync("/api/v1/billing")).StatusCode.Should().Be(HttpStatusCode.Unauthorized); // dispositivo revogado
        (await Pair("Edge")).StatusCode.Should().Be(HttpStatusCode.OK); // a vaga do plano gratuito foi liberada

        // Pela plataforma (token sem dispositivo) a rota não se aplica.
        (await web.DeleteAsync("/api/v1/extension/devices/current")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Logs_das_rotas_v2_nao_contem_dados_pessoais()
    {
        var (c, s) = await NewUser();
        var segredo = $"Valor{Guid.NewGuid():N}";
        await c.PutAsJsonAsync("/api/v1/profile", new { fullName = "Pessoa Muito Privada", workArea = "Saúde", professionalRegistration = "COREN-XX 987654" });
        await c.PostAsJsonAsync("/api/v1/profile/custom-fields", new { label = "Rotulo " + segredo, value = segredo });
        await c.GetAsync("/api/v1/ads?placement=web.banner");
        await c.GetAsync("/api/v1/billing");
        await c.PutAsJsonAsync("/api/v1/onboarding", new { answered = new[] { "nome" }, skipped = Array.Empty<string>(), lastStep = "nome", completed = false });

        var all = string.Join('\n', factory.Logs.Lines);
        all.Should().Contain("/api/v1/profile/custom-fields");
        all.Should().NotContain(segredo).And.NotContain("Pessoa Muito Privada").And.NotContain("987654").And.NotContain(s.AccessToken);
    }
}
