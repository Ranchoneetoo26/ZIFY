using System.Text.Json.Serialization;
using MediatR;
using Zyft.Application.Common.Text;

namespace Zyft.Application.Catalog;

public sealed record RegistrationHintDto(string Label, string Placeholder);

/// <summary>Área profissional com as sugestões do onboarding e do perfil (mesmo conteúdo do mockup BoasVindas).</summary>
public sealed record AreaDto(
    string Id,
    string Name,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Sectors,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> Skills,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? LinkLabel,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] RegistrationHintDto? Registration);

/// <summary>Catálogo estático por área (contrato v2). As listas são sugestões: a pessoa pode cadastrar valores próprios.</summary>
public static class AreaCatalog
{
    public static readonly IReadOnlyList<AreaDto> All = new AreaDto[]
    {
        new("tecnologia", "Tecnologia",
            new[] { "Desenvolvimento", "Dados", "Suporte técnico", "Infraestrutura", "QA e testes", "Produto", "Design de interfaces" },
            new[] { "Fintech", "Agro", "Varejo", "Saúde", "Educação", "Governo" },
            new[] { "Git", "SQL", "Excel", "Jira", "Docker", "Python", "Java", "C#", "JavaScript" },
            new[] { "Lógica de programação", "Banco de dados", "APIs", "Testes", "Metodologias ágeis", "Atendimento a usuários" },
            "GitHub", null),
        new("saude", "Saúde",
            new[] { "Técnico(a) de enfermagem", "Enfermeiro(a)", "Recepção hospitalar", "Farmácia", "Fisioterapia", "Análises clínicas" },
            new[] { "Hospital", "Clínica", "Laboratório", "Home care", "Saúde pública" },
            new[] { "Prontuário eletrônico", "Tasy", "MV Soul", "Pacote Office" },
            new[] { "Atendimento humanizado", "Administração de medicamentos", "Primeiros socorros", "Biossegurança", "Triagem", "Trabalho em equipe" },
            "Currículo Lattes", new("Registro no conselho", "Ex.: COREN-MG 123456")),
        new("vendas-e-comercial", "Vendas e comercial",
            new[] { "Vendedor(a)", "Representante comercial", "Executivo(a) de contas", "Pré-vendas", "Gerente de loja" },
            new[] { "Varejo", "Indústria", "Serviços", "Tecnologia", "Imobiliário" },
            new[] { "CRM", "Salesforce", "RD Station", "Excel", "WhatsApp Business" },
            new[] { "Negociação", "Prospecção", "Pós-venda", "Bater metas", "Atendimento ao cliente", "Apresentação" },
            null, null),
        new("administrativo-e-financeiro", "Administrativo e financeiro",
            new[] { "Assistente administrativo", "Analista financeiro", "Contas a pagar", "Recursos humanos", "Faturamento" },
            new[] { "Indústria", "Serviços", "Varejo", "Contabilidade", "Setor público" },
            new[] { "Excel", "SAP", "TOTVS", "Power BI", "Pacote Office" },
            new[] { "Conciliação bancária", "Rotinas de DP", "Organização", "Relatórios", "Atendimento a fornecedores" },
            null, new("Registro profissional (se tiver)", "Ex.: CRC-MG 000000")),
        new("educacao", "Educação",
            new[] { "Professor(a)", "Auxiliar de classe", "Coordenação pedagógica", "Monitor(a)", "Tutor(a) EAD" },
            new[] { "Escola particular", "Escola pública", "Ensino superior", "Cursos livres", "EAD" },
            new[] { "Google Classroom", "Moodle", "Canva", "Pacote Office" },
            new[] { "Planejamento de aulas", "Gestão de sala", "Avaliação", "Inclusão", "Comunicação com famílias" },
            "Currículo Lattes", null),
        new("logistica-e-operacoes", "Logística e operações",
            new[] { "Auxiliar de logística", "Motorista", "Conferente", "Operador(a) de empilhadeira", "Almoxarife" },
            new[] { "Transporte", "Varejo", "Indústria", "E-commerce" },
            new[] { "WMS", "Coletor de dados", "Excel", "SAP" },
            new[] { "Conferência de carga", "Controle de estoque", "Roteirização", "Segurança do trabalho" },
            null, new("Cursos obrigatórios (NR, MOPP…)", "Ex.: NR-11, MOPP")),
        new("juridico", "Jurídico",
            new[] { "Advogado(a)", "Estagiário(a) de Direito", "Paralegal", "Assistente jurídico" },
            new[] { "Escritório", "Departamento jurídico", "Setor público" },
            new[] { "PJe", "Projuris", "Pacote Office" },
            new[] { "Peticionamento", "Pesquisa jurisprudencial", "Contratos", "Audiências" },
            null, new("Inscrição na OAB", "Ex.: OAB/MG 123.456")),
        new("outra-area", "Outra área", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), null, null),
    };

    /// <summary>Resolve a área pelo id ("saude") ou pelo nome ("Saúde"), sem diferenciar acento e maiúsculas.</summary>
    public static AreaDto? Resolve(string? workArea)
    {
        var n = TextTools.Norm(workArea);
        if (n.Length == 0) return null;
        return All.FirstOrDefault(a => TextTools.Norm(a.Id) == n || TextTools.Norm(a.Name) == n);
    }
}

public sealed record ListAreasQuery : IRequest<IReadOnlyList<AreaDto>>;

public sealed class ListAreasHandler : IRequestHandler<ListAreasQuery, IReadOnlyList<AreaDto>>
{
    public Task<IReadOnlyList<AreaDto>> Handle(ListAreasQuery q, CancellationToken ct) => Task.FromResult(AreaCatalog.All);
}
