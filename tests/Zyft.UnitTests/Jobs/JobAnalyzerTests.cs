using Microsoft.Extensions.Logging.Abstractions;
using Zyft.Application.Jobs;
using Zyft.Application.Profiles;
using Zyft.Application.Writing;
using Zyft.UnitTests.Support;

namespace Zyft.UnitTests.Jobs;

public class JobAnalyzerTests
{
    private static readonly IReadOnlyList<string> Lines = ProfileFacts.Lines(Profiles.Ana(), new DateOnly(2026, 10, 15));

    private const string JobText = """
        Assistente Administrativo — Empresa Z
        Requisitos:
        - Experiência com contas a pagar
        - Conhecimento em SAP
        - CNH categoria B
        Diferenciais:
        - Inglês avançado
        """;

    [Fact]
    public async Task Evidencia_e_a_linha_real_do_perfil_apontada_pela_IA()
    {
        var idx = Lines.ToList().FindIndex(l => l.Contains("contas a pagar", StringComparison.OrdinalIgnoreCase)) + 1;
        var llm = new ScriptedLlm(
            """{"title":"Assistente","company":"Z","location":"","contractType":"","schedule":"","salary":"","workModel":"","summary":"","requirements":[{"text":"Experiência com contas a pagar","kind":"obrigatório"}]}""",
            $$"""{"analysis":"ok","status":"atende","line":{{idx}}}""");
        var r = await new JobAnalyzer(llm, NullLogger<JobAnalyzer>.Instance).AnalyzeAsync(null, null, JobText, Lines, _ => true);
        var req = r.Requirements.Single();
        req.Status.Should().Be("atende");
        req.Evidence.Should().Be(Lines[idx - 1]);
        req.EvidenceLine.Should().Be(idx);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Atende_sem_linha_real_vira_nao_encontrado(int line)
    {
        var llm = new ScriptedLlm(
            """{"title":"","company":"","location":"","contractType":"","schedule":"","salary":"","workModel":"","summary":"","requirements":[{"text":"Conhecimento em SAP","kind":"obrigatório"}]}""",
            $$"""{"analysis":"inventado","status":"atende","line":{{line}}}""");
        var r = await new JobAnalyzer(llm, NullLogger<JobAnalyzer>.Instance).AnalyzeAsync(null, null, JobText, Lines, _ => true);
        r.Requirements.Single().Status.Should().Be("nao_encontrado");
        r.Requirements.Single().Evidence.Should().BeEmpty();
    }

    [Fact]
    public async Task Sem_IA_usa_heuristica_e_nunca_inventa_evidencia()
    {
        var r = await new JobAnalyzer(new ScriptedLlm { IsEnabled = false }, NullLogger<JobAnalyzer>.Instance).AnalyzeAsync(null, "Assistente", JobText, Lines, k => k == "preferences.driverLicense");
        r.AiUsed.Should().BeFalse();
        r.Requirements.Should().HaveCount(4);
        r.Requirements.Single(x => x.Text.Contains("Inglês")).Kind.Should().Be("desejável");
        r.Requirements.Where(x => x.Status is "atende" or "parcial").Should().OnlyContain(x => Lines.Contains(x.Evidence));
        r.Requirements.Single(x => x.Text.Contains("contas a pagar")).Status.Should().Be("atende");
        r.ToConfirm.Should().ContainSingle(c => c.Label == "CNH" && c.HasValue);
    }

    [Fact]
    public async Task Resultado_nao_tem_nota_nem_porcentagem()
    {
        var r = await new JobAnalyzer(new ScriptedLlm { IsEnabled = false }, NullLogger<JobAnalyzer>.Instance).AnalyzeAsync(null, null, JobText, Lines, _ => false);
        var json = System.Text.Json.JsonSerializer.Serialize(r);
        json.Should().NotContain("%").And.NotContainAny("score", "percent", "nota");
        r.Disclaimer.Should().Contain("Não é garantia");
    }

    [Fact]
    public void Linhas_do_perfil_incluem_tempo_total_calculado()
    {
        Lines.Should().Contain(l => l.StartsWith("Tempo total de experiência profissional (calculado):"));
        Lines.Should().Contain(l => l.StartsWith("Experiência 1: Assistente Administrativo na Empresa Y Logística (07/2023 a atual"));
    }
}

public class AnswerWriterTests
{
    private static readonly IReadOnlyList<string> Lines = ProfileFacts.Lines(Profiles.Ana(), new DateOnly(2026, 10, 15));

    [Fact]
    public async Task Fatos_que_nao_existem_no_perfil_sao_descartados_e_texto_respeita_limite()
    {
        var llm = new ScriptedLlm("""{"answer":"Tenho experiência em contas a pagar e controle financeiro em várias empresas grandes do Brasil.","usedFacts":["Atividades (exp. 1): Lançamento de notas fiscais no ERP Totvs Protheus; controle de contas a pagar","Trabalhei 10 anos na NASA"],"missingInfo":""}""");
        var g = await new AnswerWriter(llm).GenerateAsync("Fale sobre você", Lines, null, 40);
        g.Answer.Length.Should().BeLessThanOrEqualTo(40);
        g.UsedFacts.Should().ContainSingle().Which.Should().Contain("contas a pagar");
    }

    [Fact]
    public async Task Sem_IA_lanca_indisponivel()
    {
        var act = () => new AnswerWriter(new ScriptedLlm { IsEnabled = false }).GenerateAsync("x", Lines, null, null);
        await act.Should().ThrowAsync<Zyft.Application.Common.Interfaces.LlmUnavailableException>();
    }

    [Fact]
    public async Task Prompt_contem_so_fatos_do_perfil()
    {
        var llm = new ScriptedLlm("""{"answer":"","usedFacts":[],"missingInfo":"Falta X"}""");
        var g = await new AnswerWriter(llm).GenerateAsync("Pergunta?", Lines, null, null);
        g.Answer.Should().BeEmpty();
        g.MissingInfo.Should().Be("Falta X");
        llm.Requests.Single().Prompt.Should().Contain("únicos fatos que você pode usar");
    }
}
