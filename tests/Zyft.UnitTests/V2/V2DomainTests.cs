using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Zyft.Application.Ads;
using Zyft.Application.Billing;
using Zyft.Application.Common.Interfaces;
using Zyft.Application.Forms;
using Zyft.Application.Profiles;
using Zyft.Domain.Ads;
using Zyft.Domain.Answers;
using Zyft.Domain.Billing;
using Zyft.Domain.Common;
using Zyft.Domain.Extension;
using Zyft.Domain.Onboarding;
using Zyft.Domain.Profiles;
using Zyft.UnitTests.Support;

namespace Zyft.UnitTests.V2;

public class PlanLimitTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(UsageResource.AssistedApplications, 10)]
    [InlineData(UsageResource.AiSuggestions, 5)]
    [InlineData(UsageResource.JobAnalyses, 10)]
    [InlineData(UsageResource.ResumeImports, 2)]
    public void Limites_do_Gratuito_e_Plus_sem_limite(UsageResource r, int free)
    {
        PlanRules.MonthlyLimit(PlanKind.Free, r).Should().Be(free);
        PlanRules.MonthlyLimit(PlanKind.Plus, r).Should().BeNull();
    }

    [Fact]
    public void Respostas_e_navegadores_por_plano()
    {
        PlanRules.AnswersLimit(PlanKind.Free).Should().Be(15);
        PlanRules.AnswersLimit(PlanKind.Plus).Should().BeNull();
        PlanRules.DevicesLimit(PlanKind.Free).Should().Be(1);
        PlanRules.DevicesLimit(PlanKind.Plus).Should().Be(3);
        PlanRules.AdsEnabled(PlanKind.Plus).Should().BeFalse();
    }

    [Fact]
    public void Consumo_segue_limite_depois_bonus_depois_pacote_e_entao_402()
    {
        var u = MonthlyUsage.Start(Guid.NewGuid(), "2026-10");
        var pack = UsagePack.Purchase(u.UserId, Now);
        var packs = new[] { pack };
        const int limit = 10;

        for (var i = 0; i < limit; i++) u.Consume(UsageResource.AssistedApplications, limit, packs, Now).Should().Be(UsageSource.Plan);
        u.ClaimRewarded();
        for (var i = 0; i < 3; i++) u.Consume(UsageResource.AssistedApplications, limit, packs, Now).Should().Be(UsageSource.Bonus);
        for (var i = 0; i < 15; i++) u.Consume(UsageResource.AssistedApplications, limit, packs, Now).Should().Be(UsageSource.Pack);

        pack.AssistedRemaining.Should().Be(0);
        pack.AiRemaining.Should().Be(10, "o pacote de IA é separado");
        var act = () => u.Consume(UsageResource.AssistedApplications, limit, packs, Now);
        var ex = act.Should().Throw<PlanLimitReachedException>().Which;
        ex.Code.Should().Be("plan.limit_reached");
        ex.Resource.Should().Be("assistedApplications");
        ex.Used.Should().Be(10);
        ex.Limit.Should().Be(10);
    }

    [Fact]
    public void Bonus_recompensado_vale_so_para_candidaturas_e_pacote_vencido_nao_conta()
    {
        var u = MonthlyUsage.Start(Guid.NewGuid(), "2026-10");
        u.ClaimRewarded();
        var expired = UsagePack.Purchase(u.UserId, Now.AddDays(-61));
        for (var i = 0; i < 5; i++) u.Consume(UsageResource.AiSuggestions, 5, new[] { expired }, Now);
        var act = () => u.Consume(UsageResource.AiSuggestions, 5, new[] { expired }, Now);
        act.Should().Throw<PlanLimitReachedException>().Which.Resource.Should().Be("aiSuggestions");
        u.BonusRemaining.Should().Be(3);
    }

    [Fact]
    public void Pacote_cobre_sugestoes_de_IA_mas_nao_analise_de_vaga()
    {
        var u = MonthlyUsage.Start(Guid.NewGuid(), "2026-10");
        var pack = UsagePack.Purchase(u.UserId, Now);
        for (var i = 0; i < 5; i++) u.Consume(UsageResource.AiSuggestions, 5, new[] { pack }, Now);
        u.Consume(UsageResource.AiSuggestions, 5, new[] { pack }, Now).Should().Be(UsageSource.Pack);
        pack.AiRemaining.Should().Be(9);

        for (var i = 0; i < 10; i++) u.Consume(UsageResource.JobAnalyses, 10, new[] { pack }, Now);
        var act = () => u.Consume(UsageResource.JobAnalyses, 10, new[] { pack }, Now);
        act.Should().Throw<PlanLimitReachedException>().Which.Resource.Should().Be("jobAnalyses");
    }

    [Fact]
    public void Plus_nao_tem_limite()
    {
        var u = MonthlyUsage.Start(Guid.NewGuid(), "2026-10");
        for (var i = 0; i < 50; i++) u.Consume(UsageResource.ResumeImports, null, Array.Empty<UsagePack>(), Now).Should().Be(UsageSource.Plan);
        u.UsedOf(UsageResource.ResumeImports).Should().Be(50);
    }

    [Fact]
    public void Recompensado_no_maximo_3_vezes_por_mes()
    {
        var u = MonthlyUsage.Start(Guid.NewGuid(), "2026-10");
        for (var i = 0; i < 3; i++) u.ClaimRewarded();
        u.BonusGranted.Should().Be(9);
        var act = () => u.ClaimRewarded();
        act.Should().Throw<RewardedExhaustedException>().Which.Code.Should().Be("ads.rewarded_exhausted");
    }

    [Fact]
    public void Assinatura_tem_7_dias_gratis_so_na_primeira_vez_e_cancelar_volta_ao_gratuito()
    {
        var s = Subscription.StartFree(Guid.NewGuid());
        s.Subscribe(BillingPeriod.Monthly, Now);
        s.Plan.Should().Be(PlanKind.Plus);
        s.TrialEndsAt.Should().Be(Now.AddDays(7));
        s.Cancel(Now.AddDays(1));
        s.Plan.Should().Be(PlanKind.Free);
        s.Subscribe(BillingPeriod.Yearly, Now.AddDays(2));
        s.TrialEndsAt.Should().BeNull();
        s.RenewsAt.Should().Be(Now.AddDays(2).AddYears(1));
    }

    [Fact]
    public async Task Medidor_conta_a_mesma_pagina_uma_vez_por_mes_e_guarda_so_hash()
    {
        var uid = Guid.NewGuid();
        var marks = new InMemoryRepository<UsageMark>();
        var usages = new InMemoryRepository<MonthlyUsage>();
        var hasher = new Mock<ITokenService>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(s => "h" + s.Length);
        var meter = new UsageMeter(new InMemoryRepository<Subscription>(), usages, new InMemoryRepository<UsagePack>(), marks,
            new InMemoryRepository<ApprovedAnswer>(), new InMemoryRepository<ExtensionDevice>(), hasher.Object, new FixedClock(Now));

        (await meter.ConsumeAsync(uid, UsageResource.AssistedApplications, "https://vagas.exemplo.com/form")).Should().Be(UsageSource.Plan);
        (await meter.ConsumeAsync(uid, UsageResource.AssistedApplications, "https://vagas.exemplo.com/form")).Should().BeNull();
        usages.Items.Single().AssistedUsed.Should().Be(1);
        marks.Items.Single().KeyHash.Should().NotContain("vagas");
    }

    [Fact]
    public async Task Banco_de_respostas_cheio_no_Gratuito_da_402()
    {
        var uid = Guid.NewGuid();
        var answers = new InMemoryRepository<ApprovedAnswer>();
        for (var i = 0; i < 15; i++) answers.Add(ApprovedAnswer.Approve(uid, $"P{i}", "R", Now));
        var meter = new UsageMeter(new InMemoryRepository<Subscription>(), new InMemoryRepository<MonthlyUsage>(), new InMemoryRepository<UsagePack>(),
            new InMemoryRepository<UsageMark>(), answers, new InMemoryRepository<ExtensionDevice>(), Mock.Of<ITokenService>(), new FixedClock(Now));
        var act = () => meter.EnsureAnswerCapacityAsync(uid);
        var ex = (await act.Should().ThrowAsync<PlanLimitReachedException>()).Which;
        ex.Resource.Should().Be("answers");
        ex.Limit.Should().Be(15);
    }
}

public class CustomFieldTests
{
    [Theory]
    [InlineData("Raça/cor")]
    [InlineData("Religião")]
    [InlineData("Condição de saúde")]
    [InlineData("Possui deficiência?")]
    [InlineData("Gênero")]
    [InlineData("Orientação sexual")]
    [InlineData("CPF")]
    [InlineData("Número do RG")]
    [InlineData("Senha do portal")]
    [InlineData("PcD")]
    public void Rotulo_sensivel_e_recusado(string label)
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        var act = () => p.AddCustomField(label, "valor");
        act.Should().Throw<DomainException>().Which.Code.Should().Be("profile.custom_field_sensitive");
        p.CustomFields.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Disponível para plantão noturno")]
    [InlineData("Registro no conselho")]
    [InlineData("Orientação a objetos")]
    [InlineData("Transporte próprio")]
    [InlineData("Cidade onde estudou")]
    public void Rotulo_comum_e_aceito(string label)
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        p.AddCustomField(label, "Sim").Label.Should().Be(label);
    }

    [Fact]
    public void Rotulo_duplicado_e_limites_de_tamanho()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        p.AddCustomField("Plantão", "Sim");
        ((Action)(() => p.AddCustomField("plantão", "Não"))).Should().Throw<DomainException>().Which.Code.Should().Be("profile.custom_field_duplicated");
        ((Action)(() => p.AddCustomField(new string('a', 81), "x"))).Should().Throw<DomainException>().Which.Code.Should().Be("profile.custom_field_label_too_long");
        ((Action)(() => p.AddCustomField("Outro", new string('a', 501)))).Should().Throw<DomainException>().Which.Code.Should().Be("profile.custom_field_value_too_long");
    }

    [Fact]
    public void Campo_personalizado_entra_nos_fatos_com_chave_custom()
    {
        var p = Profiles.Ana();
        var f = p.AddCustomField("Disponível para plantão noturno", "Sim, escala 12x36");
        var facts = ProfileFacts.Build(p);
        facts.Get($"custom:{f.Id}")!.Label.Should().Be("Disponível para plantão noturno");
        ProfileFacts.Lines(p, new DateOnly(2026, 10, 21)).Should().Contain("Disponível para plantão noturno: Sim, escala 12x36");
    }

    [Fact]
    public async Task Mapeador_por_regras_usa_campo_personalizado_pelo_rotulo()
    {
        var p = Profiles.Ana();
        var f = p.AddCustomField("Disponível para plantão noturno", "Sim, escala 12x36");
        var mapper = new FieldMapper(new ScriptedLlm { IsEnabled = false }, NullLogger<FieldMapper>.Instance);
        var r = await mapper.MapAsync(new MappingInput
        {
            Fields = new[]
            {
                new FormField { Id = "a", Label = "Disponível para plantão noturno", Type = "text", Tag = "input" },
                new FormField { Id = "b", Label = "Você tem disponibilidade para plantão noturno?", Type = "text", Tag = "input" },
            },
            Profile = ProfileFacts.Build(p),
        });
        foreach (var m in r.Mappings)
        {
            m.ProfileKey.Should().Be($"custom:{f.Id}");
            m.Value.Should().Be("Sim, escala 12x36");
            m.ProfileLabel.Should().Be("Disponível para plantão noturno");
        }
    }

    [Fact]
    public async Task IA_pode_apontar_custom_id_e_o_valor_vem_do_perfil()
    {
        var p = Profiles.Ana();
        var f = p.AddCustomField("Turno preferido", "Noite");
        var llm = new ScriptedLlm($$"""{"mappings":[{"fieldId":"x","action":"fill","profileKey":"custom:{{f.Id}}","value":"INVENTADO"}]}""");
        var mapper = new FieldMapper(llm, NullLogger<FieldMapper>.Instance);
        var r = await mapper.MapAsync(new MappingInput
        {
            Fields = new[] { new FormField { Id = "x", Label = "Qual período você prefere trabalhar", Type = "text", Tag = "input" } },
            Profile = ProfileFacts.Build(p),
        });
        r.Mappings.Single().Value.Should().Be("Noite");
        r.Mappings.Single().ProfileKey.Should().Be($"custom:{f.Id}");
    }

    [Fact]
    public async Task Registro_profissional_e_mapeado_por_regra()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        p.UpdateBasics(new ProfileBasicData { FullName = "Beatriz Lima", ProfessionalRegistration = "COREN-MG 123456", WorkArea = "Saúde", SalaryType = "Por plantão" });
        var mapper = new FieldMapper(new ScriptedLlm { IsEnabled = false }, NullLogger<FieldMapper>.Instance);
        var r = await mapper.MapAsync(new MappingInput
        {
            Fields = new[] { new FormField { Id = "r", Label = "Número do COREN", Type = "text", Tag = "input" } },
            Profile = ProfileFacts.Build(p),
        });
        r.Mappings.Single().Value.Should().Be("COREN-MG 123456");
        p.SalaryType.Should().Be("Por plantão");
    }

    [Fact]
    public void Tipo_de_pretensao_invalido_viola_regra()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        var act = () => p.UpdateBasics(new ProfileBasicData { SalaryType = "Mesada" });
        act.Should().Throw<DomainException>().Which.Code.Should().Be("profile.invalid_salary_type");
    }
}

public class OnboardingTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Respondido_sai_dos_pulados_e_conclusao_conta_uma_vez()
    {
        var o = OnboardingProgress.Start(Guid.NewGuid());
        o.Update(new[] { "nome", "area" }, new[] { "links", "area" }, "links", false, Now).Should().BeFalse();
        o.AnsweredSteps.Should().Equal("nome", "area");
        o.SkippedSteps.Should().Equal("links");
        o.Update(new[] { "nome" }, Array.Empty<string>(), "combinados", true, Now).Should().BeTrue();
        o.Update(new[] { "nome" }, Array.Empty<string>(), "combinados", true, Now.AddDays(1)).Should().BeFalse();
        o.CompletedAt.Should().Be(Now);
    }

    [Fact]
    public void Passo_desconhecido_viola_regra()
    {
        var act = () => OnboardingProgress.Start(Guid.NewGuid()).Update(new[] { "senha" }, null, null, false, Now);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("onboarding.invalid_step");
    }
}

public class AdSelectionTests
{
    private static readonly HashSet<string> None = new();

    private static AdChoice? Pick(string placement, AdTargeting t, ISet<string>? hiddenAds = null, ISet<string>? hiddenTopics = null) =>
        AdSelector.Select(placement, t, hiddenAds ?? None, hiddenTopics ?? None, _ => 0);

    [Fact]
    public void Selecao_so_recebe_area_cargos_e_topico_nunca_dados_pessoais()
    {
        // Garantia por construção: o único insumo de segmentação tem exatamente estes três campos.
        typeof(AdTargeting).GetProperties().Select(p => p.Name).Should().BeEquivalentTo("WorkArea", "DesiredRoles", "Topic");
        typeof(AdSelector).GetMethod(nameof(AdSelector.Select))!.GetParameters().Select(p => p.ParameterType)
            .Should().NotContain(new[] { typeof(Profile), typeof(ProfileFactSet) });
    }

    [Fact]
    public void Escolhe_pela_area_e_explica_sem_dados_pessoais()
    {
        var c = Pick(AdPlacements.ProfileRail, new AdTargeting("Saúde", "Técnico(a) de enfermagem", null))!;
        c.Ad.AreaId.Should().Be("saude");
        c.Reason.Should().NotBeNullOrWhiteSpace().And.NotContain("Beatriz");
    }

    [Fact]
    public void Contextual_exige_relacao_com_o_requisito()
    {
        Pick(AdPlacements.JobContextual, new AdTargeting("Tecnologia", null, null)).Should().BeNull();
        var c = Pick(AdPlacements.JobContextual, new AdTargeting("Tecnologia", null, "Conhecimento em Docker e Kubernetes"))!;
        c.Ad.Id.Should().Be("demo-tec-cloud");
        c.Topic.Should().Be("Conhecimento em Docker e Kubernetes");
    }

    [Theory]
    [InlineData("web.onboarding")]
    [InlineData("web.sensitive")]
    [InlineData("ext.form.review")]
    [InlineData("web.account")]
    public void Espaco_proibido_nao_tem_anuncio(string placement) =>
        Pick(placement, new AdTargeting("Tecnologia", null, null)).Should().BeNull();

    [Fact]
    public void Ocultos_por_anuncio_e_por_assunto_sao_excluidos()
    {
        var t = new AdTargeting("Saúde", null, null);
        var first = Pick(AdPlacements.ProfileRail, t)!.Ad;
        Pick(AdPlacements.ProfileRail, t, hiddenAds: new HashSet<string> { first.Id })!.Ad.Id.Should().NotBe(first.Id);
        var allSaude = AdCatalog.All.Where(a => a.AreaId == "saude").Select(a => a.Topic).ToHashSet();
        var general = AdCatalog.All.Where(a => a.AreaId is null).Select(a => a.Topic);
        Pick(AdPlacements.ProfileRail, t, hiddenTopics: allSaude.Concat(general).ToHashSet()).Should().BeNull();
    }

    [Fact]
    public void Catalogo_e_de_anunciante_de_demonstracao_com_ids_unicos()
    {
        AdCatalog.All.Should().HaveCountGreaterOrEqualTo(15);
        AdCatalog.All.Select(a => a.Id).Should().OnlyHaveUniqueItems();
        AdCatalog.Advertiser.Should().Be("Anunciante de demonstração");
    }

    [Fact]
    public async Task Plus_recebe_204_sem_consultar_perfil()
    {
        var uid = Guid.NewGuid();
        var meter = new Mock<IUsageMeter>();
        meter.Setup(m => m.GetPlanAsync(uid, It.IsAny<CancellationToken>())).ReturnsAsync(PlanKind.Plus);
        var profiles = new Mock<IProfileRepository>(MockBehavior.Strict);
        var handler = new GetAdHandler(profiles.Object, new InMemoryRepository<AdHide>(), new InMemoryRepository<AdStat>(), meter.Object,
            Network(prefers: true), new FakeCurrentUser(uid), new FakeUnitOfWork(), new FixedClock(DateTime.UtcNow));
        (await handler.Handle(new GetAdQuery(AdPlacements.ProfileRail, null), default)).Should().BeNull(); // Plus também não vê a rede
        profiles.VerifyNoOtherCalls();
    }

    // ------------------------------------------------------------ rede de anúncios (AdSense simulado, ADR-0021)

    private static IAdNetwork Network(bool prefers, bool enabled = true)
    {
        var n = new Mock<IAdNetwork>();
        n.Setup(x => x.SlotFor(It.IsAny<string>())).Returns<string>(p => enabled && AdPlacements.IsAllowed(p) ? new AdNetworkSlot("Google AdSense", "ca-pub-0000000000000000", "1000000001", "auto", true) : null);
        n.Setup(x => x.Prefers(It.IsAny<string>())).Returns(prefers && enabled);
        return n.Object;
    }

    private static GetAdHandler AdHandler(Guid uid, IAdNetwork network, InMemoryRepository<AdHide>? hides = null)
    {
        var meter = new Mock<IUsageMeter>();
        meter.Setup(m => m.GetPlanAsync(uid, It.IsAny<CancellationToken>())).ReturnsAsync(PlanKind.Free);
        var profiles = new Mock<IProfileRepository>();
        return new GetAdHandler(profiles.Object, hides ?? new InMemoryRepository<AdHide>(), new InMemoryRepository<AdStat>(), meter.Object,
            network, new FakeCurrentUser(uid), new FakeUnitOfWork(), new FixedClock(DateTime.UtcNow));
    }

    [Fact]
    public async Task Rede_vem_primeiro_nos_espacos_preferidos_e_nao_leva_dados_do_perfil()
    {
        var ad = await AdHandler(Guid.NewGuid(), Network(prefers: true)).Handle(new GetAdQuery(AdPlacements.WebBanner, "Experiência com SQL"), default);
        ad!.Format.Should().Be("network");
        ad.Network!.Client.Should().StartWith("ca-pub-");
        ad.Network.Mock.Should().BeTrue();
        ad.Topic.Should().BeNull(); // o texto da vaga não vai para a rede
        ad.Reason.Should().Contain("simulação");
    }

    [Fact]
    public async Task Sem_anuncio_direto_a_rede_preenche_o_espaco()
    {
        // Contextual sem assunto não tem anúncio direto; com a rede desligada → null; ligada mas sem bloco para o espaço → null.
        (await AdHandler(Guid.NewGuid(), Network(prefers: false, enabled: false)).Handle(new GetAdQuery(AdPlacements.JobContextual, null), default)).Should().BeNull();
        var ad = await AdHandler(Guid.NewGuid(), Network(prefers: false)).Handle(new GetAdQuery(AdPlacements.JobContextual, null), default);
        ad!.Format.Should().Be("network");
    }

    [Fact]
    public async Task Bloco_da_rede_ocultado_nao_volta()
    {
        var uid = Guid.NewGuid();
        var hides = new InMemoryRepository<AdHide>();
        hides.Add(AdHide.Create(uid, "adsense-1000000001", "rede", AdHideScope.Ad, DateTime.UtcNow));
        var ad = await AdHandler(uid, Network(prefers: true), hides).Handle(new GetAdQuery(AdPlacements.WebBanner, null), default);
        (ad is null || ad.Format != "network").Should().BeTrue();
    }

    [Fact]
    public void AdSense_simulado_por_padrao_atende_os_banners_e_nunca_espacos_proibidos()
    {
        var net = new Zyft.Infrastructure.Ads.AdSenseNetwork(Microsoft.Extensions.Options.Options.Create(new Zyft.Infrastructure.Ads.AdNetworkOptions()));
        net.SlotFor(AdPlacements.WebBanner)!.Mock.Should().BeTrue();
        net.Prefers(AdPlacements.WebBanner).Should().BeTrue();
        net.Prefers(AdPlacements.ProfileRail).Should().BeFalse();
        net.SlotFor("web.onboarding").Should().BeNull();
        net.SlotFor(AdPlacements.JobContextual).Should().BeNull(); // contextual é só anúncio direto ligado ao requisito
    }
}
