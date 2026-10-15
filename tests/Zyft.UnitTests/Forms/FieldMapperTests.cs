using Microsoft.Extensions.Logging.Abstractions;
using Zyft.Application.Forms;
using Zyft.Application.Profiles;
using Zyft.Domain.Profiles;
using Zyft.Domain.Sensitive;
using Zyft.UnitTests.Support;

namespace Zyft.UnitTests.Forms;

public class FieldMapperTests
{
    private static FormField F(string id, string label, string type = "text", string tag = "input", string? section = null, string? name = null,
        string? current = null, params (string Value, string Text)[] options) => new()
    {
        Id = id, Label = label, Type = type, Tag = tag, Section = section, Name = name, CurrentValue = current,
        Options = options.Length == 0 ? null : options.Select(o => new FieldOption(o.Value, o.Text)).ToList(),
    };

    private static async Task<MappingResult> Map(IEnumerable<FormField> fields, Profile? profile = null, ScriptedLlm? llm = null, bool sensitiveConsent = false,
        Dictionary<SensitiveDataKind, string>? sensitive = null, IReadOnlyList<ApprovedAnswerRef>? answers = null)
    {
        var mapper = new FieldMapper(llm ?? new ScriptedLlm { IsEnabled = false }, NullLogger<FieldMapper>.Instance);
        return await mapper.MapAsync(new MappingInput
        {
            Fields = fields.ToList(),
            Profile = ProfileFacts.Build(profile ?? Profiles.Ana()),
            SensitiveConsent = sensitiveConsent,
            SensitiveValues = sensitive ?? new(),
            ApprovedAnswers = answers ?? Array.Empty<ApprovedAnswerRef>(),
        });
    }

    private static FieldSuggestion One(MappingResult r, string id) => r.Mappings.Single(m => m.FieldId == id);

    [Theory]
    [InlineData("Nome completo", "personal.fullName", "Ana Paula Souza")]
    [InlineData("Full name", "personal.fullName", "Ana Paula Souza")]
    [InlineData("E-mail", "personal.email", "ana.souza@email.com")]
    [InlineData("Email address", "personal.email", "ana.souza@email.com")]
    [InlineData("Telefone celular", "personal.phone", "(44) 99999-2222")]
    [InlineData("Phone", "personal.phone", "(44) 99999-2222")]
    [InlineData("CEP", "personal.zipCode", "87000-000")]
    [InlineData("Cidade", "personal.city", "Maringá")]
    [InlineData("Disponibilidade para viagens", "preferences.travel", "Sim")]
    public async Task Regras_PT_EN_mapeiam_campos_objetivos(string label, string key, string value)
    {
        var r = await Map(new[] { F("f", label) });
        var s = One(r, "f");
        s.ProfileKey.Should().Be(key);
        s.Value.Should().Be(value);
        s.Source.Should().Be(SuggestionSource.Rule);
        s.State.Should().Be(SuggestionState.Ready);
        s.Selected.Should().BeTrue();
    }

    [Fact]
    public async Task Primeiro_nome_e_sobrenome_separados_quando_a_pagina_pede()
    {
        var r = await Map(new[] { F("a", "Nome"), F("b", "Sobrenome") });
        One(r, "a").Value.Should().Be("Ana");
        One(r, "b").Value.Should().Be("Paula Souza");
    }

    [Theory]
    [InlineData("password", "Senha")]
    [InlineData("text", "Confirme sua senha")]
    [InlineData("hidden", "token")]
    [InlineData("file", "Anexe seu currículo")]
    [InlineData("text", "Digite o código de verificação (captcha)")]
    public async Task Senha_captcha_e_arquivo_nunca_recebem_valor(string type, string label)
    {
        var r = await Map(new[] { F("x", label, type) });
        var s = One(r, "x");
        s.State.Should().Be(SuggestionState.Blocked);
        s.Value.Should().BeNull();
        s.Selected.Should().BeFalse();
    }

    [Fact]
    public async Task Campos_bloqueados_sao_classificados_antes_da_IA_e_nem_chegam_ao_prompt()
    {
        var llm = new ScriptedLlm("""{"mappings":[]}""");
        await Map(new[] { F("p", "Senha", "password"), F("c", "captcha"), F("g", "Gênero", "select", "select", options: ("f", "Feminino")) }, llm: llm);
        llm.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Sensivel_sem_consentimento_fica_bloqueado_sem_valor()
    {
        var r = await Map(new[] { F("g", "Gênero", "select", "select", options: ("f", "Feminino")) },
            sensitiveConsent: false, sensitive: new() { [SensitiveDataKind.Gender] = "Feminino" });
        var s = One(r, "g");
        s.Kind.Should().Be(SuggestionKinds.Sensitive);
        s.State.Should().Be(SuggestionState.Blocked);
        s.Value.Should().BeNull();
    }

    [Fact]
    public async Task Sensivel_com_consentimento_traz_valor_mas_nunca_marcado()
    {
        var r = await Map(new[] { F("g", "Gênero", "select", "select", options: new[] { ("", "Selecione"), ("f", "Feminino"), ("m", "Masculino") }) },
            sensitiveConsent: true, sensitive: new() { [SensitiveDataKind.Gender] = "Feminino" });
        var s = One(r, "g");
        s.Value.Should().Be("f");
        s.Selected.Should().BeFalse();
        s.State.Should().Be(SuggestionState.Blocked);
    }

    [Fact]
    public async Task Declaracao_e_termos_nunca_marcados()
    {
        var r = await Map(new[] { F("d", "Declaro que as informações são verdadeiras", "checkbox"), F("t", "I agree to the terms", "checkbox") });
        r.Mappings.Should().OnlyContain(m => m.Kind == SuggestionKinds.Declaration && !m.Selected && m.Value == null);
    }

    [Fact]
    public async Task Select_escolhe_opcao_existente_na_pagina()
    {
        var r = await Map(new[] { F("m", "Modalidade de trabalho", "select", "select", options: new[] { ("", "Selecione"), ("1", "Remoto"), ("2", "Presencial") }) });
        var s = One(r, "m");
        s.Value.Should().Be("2");
        s.OptionText.Should().Be("Presencial");
    }

    [Fact]
    public async Task Select_sem_opcao_correspondente_fica_ambiguo_e_desmarcado()
    {
        var r = await Map(new[] { F("m", "Modalidade", "select", "select", options: new[] { ("1", "Remoto"), ("2", "Integral") }) });
        var s = One(r, "m");
        s.State.Should().Be(SuggestionState.Ambiguous);
        s.Selected.Should().BeFalse();
        s.Value.Should().BeNull();
        s.ProfileValue.Should().Be("Presencial");
    }

    [Fact]
    public async Task Valor_diferente_ja_no_portal_fica_divergente_e_mantem_o_do_portal()
    {
        var r = await Map(new[] { F("t", "Telefone", current: "(11) 3333-4444") });
        var s = One(r, "t");
        s.State.Should().Be(SuggestionState.Divergent);
        s.Selected.Should().BeFalse();
        s.ProfileValue.Should().Be("(44) 99999-2222");
    }

    [Fact]
    public async Task Blocos_repetidos_sao_alinhados_pela_organizacao()
    {
        // O portal já tem "Empresa X Comércio" no bloco 1; no perfil ela é o item 2 (ordem: atual primeiro).
        var fields = new[]
        {
            F("c0", "Empresa", section: "Experiência 1", current: "Empresa X Comércio"),
            F("r0", "Cargo", section: "Experiência 1"),
            F("c1", "Empresa", section: "Experiência 2"),
            F("r1", "Cargo", section: "Experiência 2"),
        };
        var r = await Map(fields);
        One(r, "r0").Value.Should().Be("Auxiliar Administrativo");
        One(r, "c1").Value.Should().Be("Empresa Y Logística");
        One(r, "r1").Value.Should().Be("Assistente Administrativo");
    }

    [Fact]
    public async Task Indices_explicitos_no_name_base_1_sao_normalizados()
    {
        var fields = new[] { F("a", "Empresa", name: "exp[1][empresa]"), F("b", "Empresa", name: "exp[2][empresa]") };
        var r = await Map(fields);
        One(r, "a").Value.Should().Be("Empresa Y Logística");
        One(r, "b").Value.Should().Be("Empresa X Comércio");
    }

    [Theory]
    [InlineData("date", "Data de nascimento", "1998-03-14")]
    [InlineData("month", "Data de início", "2023-07")]
    public async Task Datas_sao_adaptadas_ao_tipo_do_input(string type, string label, string expected)
    {
        var section = label.Contains("início") ? "Experiência profissional" : null;
        var r = await Map(new[] { F("d", label, type, section: section) });
        One(r, "d").Value.Should().Be(expected);
    }

    [Fact]
    public async Task Pretensao_salarial_em_texto_vem_formatada_em_reais()
    {
        var r = await Map(new[] { F("s", "Pretensão salarial") });
        One(r, "s").Value.Should().MatchRegex(@"^R\$\s2\.500,00$");
    }

    [Fact]
    public async Task Campo_numerico_recebe_so_numero()
    {
        var r = await Map(new[] { F("s", "Pretensão salarial", "number") });
        One(r, "s").Value.Should().Be("2500");
    }

    [Fact]
    public async Task Respeita_maxLength_do_campo()
    {
        var f = F("n", "Nome completo") with { MaxLength = 5 };
        var r = await Map(new[] { f });
        One(r, "n").Value.Should().Be("Ana P");
    }

    [Fact]
    public async Task IA_so_aponta_chave_e_o_valor_vem_do_perfil()
    {
        // A IA "tenta" escrever um valor — deve ser ignorado.
        var llm = new ScriptedLlm("""{"mappings":[{"fieldId":"x","action":"fill","profileKey":"personal.email","value":"hacker@mal.com"}]}""");
        var r = await Map(new[] { F("x", "Como podemos te contatar por escrito") }, llm: llm);
        var s = One(r, "x");
        s.Source.Should().Be(SuggestionSource.Ai);
        s.Value.Should().Be("ana.souza@email.com");
        s.Confidence.Should().Be("medium");
    }

    [Fact]
    public async Task Chave_inventada_pela_IA_e_descartada()
    {
        var llm = new ScriptedLlm("""{"mappings":[{"fieldId":"x","action":"fill","profileKey":"personal.cpf"},{"fieldId":"y","action":"fill","profileKey":"experiences[9].company"}]}""");
        var r = await Map(new[] { F("x", "Documento"), F("y", "Onde trabalhou antes") }, llm: llm);
        r.Mappings.Should().OnlyContain(m => m.Value == null && m.State == SuggestionState.Missing);
    }

    [Fact]
    public async Task IA_fora_do_ar_nao_derruba_o_mapeamento()
    {
        var r = await Map(new[] { F("a", "Nome completo"), F("b", "Campo estranho") }, llm: new ScriptedLlm { Fail = true });
        One(r, "a").Value.Should().Be("Ana Paula Souza");
        One(r, "b").State.Should().Be(SuggestionState.Missing);
        r.AiUsed.Should().BeFalse();
    }

    [Fact]
    public async Task Prompt_da_IA_nao_contem_dados_sensiveis()
    {
        var llm = new ScriptedLlm("""{"mappings":[]}""");
        await Map(new[] { F("x", "Campo qualquer") }, llm: llm, sensitiveConsent: true, sensitive: new() { [SensitiveDataKind.Ethnicity] = "ValorSensivelSecreto" });
        llm.Requests.Should().ContainSingle().Which.Prompt.Should().NotContain("ValorSensivelSecreto");
    }

    [Fact]
    public async Task Pergunta_aberta_reaproveita_resposta_aprovada_sem_marcar()
    {
        var answers = new[] { new ApprovedAnswerRef(Guid.NewGuid(), "Por que você quer trabalhar conosco?", "Porque admiro a empresa.") };
        var r = await Map(new[] { F("q", "Por que você quer trabalhar conosco?", "textarea", "textarea") }, answers: answers);
        var s = One(r, "q");
        s.Source.Should().Be(SuggestionSource.Answer);
        s.Value.Should().Be("Porque admiro a empresa.");
        s.Selected.Should().BeFalse();
    }

    [Fact]
    public async Task Devolve_exatamente_um_item_por_campo_na_ordem_da_pagina()
    {
        var fields = new[] { F("3", "E-mail"), F("1", "Senha", "password"), F("2", "Qualquer coisa") };
        var r = await Map(fields);
        r.Mappings.Select(m => m.FieldId).Should().Equal("3", "1", "2");
        r.Stats.Fields.Should().Be(3);
    }

    [Theory]
    [InlineData("Superior completo", "Graduação", false)]
    [InlineData("Sim", "Sim", true)]
    [InlineData("Não possuo", "Não", true)]
    public void PickOption_casa_por_texto_ou_sim_nao(string option, string value, bool shouldMatch)
    {
        var opts = new[] { new FieldOption("v", option), new FieldOption("x", "Outro") };
        (FieldMapper.PickOption(opts, value) is not null).Should().Be(shouldMatch);
    }
}
