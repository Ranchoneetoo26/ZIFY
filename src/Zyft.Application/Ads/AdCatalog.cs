namespace Zyft.Application.Ads;

/// <summary>Anúncio do catálogo de demonstração. Nenhum anúncio é real: todos são de "Anunciante de demonstração".</summary>
public sealed record AdDefinition(
    string Id,
    string? AreaId,
    string Topic,
    string TopicLabel,
    string Title,
    string Body,
    string Cta,
    string Url,
    IReadOnlyList<string> Keywords);

/// <summary>
/// Catálogo seed (~15 anúncios) para várias áreas. Fica no código (não em tabela) porque é fixo na demonstração;
/// só contadores agregados (<c>AdStat</c>) e os "ocultar" de cada pessoa vão para o banco.
/// </summary>
public static class AdCatalog
{
    public const string Advertiser = "Anunciante de demonstração";

    public static readonly IReadOnlyList<AdDefinition> All = new AdDefinition[]
    {
        // ---- Tecnologia
        new("demo-tec-sql", "tecnologia", "cursos-tecnologia", "cursos de tecnologia",
            "SQL do zero ao relatório em 4 semanas", "Curso on-line com exercícios em banco real e certificado. Turmas mensais.",
            "Conhecer o curso", "https://example.com/zyft-demo/curso-sql",
            new[] { "sql", "banco de dados", "dados", "consultas", "postgres", "mysql" }),
        new("demo-tec-cloud", "tecnologia", "cursos-tecnologia", "cursos de tecnologia",
            "Docker e nuvem na prática", "Laboratórios guiados de contêineres, CI/CD e deploy. Comece grátis.",
            "Ver laboratórios", "https://example.com/zyft-demo/docker-nuvem",
            new[] { "docker", "kubernetes", "nuvem", "cloud", "aws", "azure", "devops", "infraestrutura", "ci cd" }),
        new("demo-tec-ingles", "tecnologia", "idiomas", "cursos de idiomas",
            "Inglês para reuniões técnicas", "Aulas curtas focadas em daily, code review e entrevistas em inglês.",
            "Fazer aula teste", "https://example.com/zyft-demo/ingles-tech",
            new[] { "ingles", "english", "idioma", "conversacao" }),
        new("demo-tec-teclado", "tecnologia", "equipamentos", "equipamentos de trabalho",
            "Monte seu setup de home office", "Cadeiras, monitores e teclados com desconto para quem trabalha remoto.",
            "Ver ofertas", "https://example.com/zyft-demo/setup",
            new[] { "remoto", "home office", "desenvolvimento", "suporte tecnico" }),

        // ---- Saúde
        new("demo-sau-sbv", "saude", "cursos-saude", "cursos da área da saúde",
            "Suporte Básico de Vida com certificado", "Curso presencial de 16 h com simulação prática. Turmas aos sábados.",
            "Ver turmas", "https://example.com/zyft-demo/sbv",
            new[] { "suporte basico de vida", "sbv", "primeiros socorros", "emergencia", "urgencia", "bls" }),
        new("demo-sau-uti", "saude", "cursos-saude", "cursos da área da saúde",
            "Especialização em UTI para técnicos", "Atualização em cuidados intensivos com estágio supervisionado.",
            "Conhecer o curso", "https://example.com/zyft-demo/uti",
            new[] { "uti", "terapia intensiva", "enfermagem", "tecnico de enfermagem", "enfermeiro" }),
        new("demo-sau-jaleco", "saude", "uniformes", "uniformes e acessórios",
            "Jalecos e pijamas cirúrgicos sob medida", "Tecido antimicrobiano e bordado do nome. Entrega em todo o Brasil.",
            "Ver modelos", "https://example.com/zyft-demo/jalecos",
            new[] { "hospital", "clinica", "enfermagem", "farmacia", "plantao" }),

        // ---- Vendas e comercial
        new("demo-ven-crm", "vendas-e-comercial", "cursos-vendas", "cursos de vendas",
            "CRM na prática para vendedores", "Aprenda a organizar o funil e o pós-venda em ferramentas de CRM.",
            "Começar agora", "https://example.com/zyft-demo/crm",
            new[] { "crm", "salesforce", "rd station", "funil", "pos venda", "prospeccao" }),
        new("demo-ven-negociacao", "vendas-e-comercial", "cursos-vendas", "cursos de vendas",
            "Negociação: feche mais sem baixar o preço", "Workshop de 6 horas com casos reais de varejo e B2B.",
            "Garantir vaga", "https://example.com/zyft-demo/negociacao",
            new[] { "negociacao", "metas", "vendas", "representante comercial", "vendedor" }),

        // ---- Administrativo e financeiro
        new("demo-adm-excel", "administrativo-e-financeiro", "cursos-escritorio", "cursos de escritório",
            "Excel avançado com tabelas dinâmicas", "Do PROCV ao painel em Power BI, com planilhas para praticar.",
            "Conhecer o curso", "https://example.com/zyft-demo/excel",
            new[] { "excel", "planilhas", "procv", "power bi", "tabela dinamica", "pacote office" }),
        new("demo-adm-dp", "administrativo-e-financeiro", "cursos-escritorio", "cursos de escritório",
            "Rotinas de DP e folha de pagamento", "Curso rápido de departamento pessoal atualizado com o eSocial.",
            "Ver programa", "https://example.com/zyft-demo/dp",
            new[] { "departamento pessoal", "dp", "folha", "esocial", "recursos humanos", "rh" }),

        // ---- Educação
        new("demo-edu-ead", "educacao", "cursos-educacao", "cursos para educadores",
            "Ferramentas digitais para professores", "Google Classroom, Moodle e Canva para aulas mais dinâmicas.",
            "Fazer inscrição", "https://example.com/zyft-demo/edu-digital",
            new[] { "google classroom", "moodle", "canva", "ead", "professor", "tutor" }),

        // ---- Logística e operações
        new("demo-log-nr11", "logistica-e-operacoes", "cursos-logistica", "cursos de logística",
            "NR-11 e operação de empilhadeira", "Curso com aula prática e certificado válido em todo o país.",
            "Ver datas", "https://example.com/zyft-demo/nr11",
            new[] { "nr 11", "nr11", "empilhadeira", "mopp", "logistica", "almoxarife", "conferente" }),

        // ---- Jurídico
        new("demo-jur-pje", "juridico", "cursos-juridico", "cursos jurídicos",
            "PJe sem mistério: peticionamento eletrônico", "Passo a passo do processo eletrônico com exercícios guiados.",
            "Conhecer o curso", "https://example.com/zyft-demo/pje",
            new[] { "pje", "peticionamento", "processo eletronico", "advogado", "juridico" }),

        // ---- Geral (qualquer área)
        new("demo-ger-curriculo", null, "carreira", "carreira e entrevistas",
            "Treine para entrevistas com simulações", "Perguntas comuns por área e dicas de como contar suas experiências.",
            "Treinar agora", "https://example.com/zyft-demo/entrevistas",
            new[] { "entrevista", "comunicacao", "apresentacao" }),
        new("demo-ger-libras", null, "idiomas", "cursos de idiomas",
            "Libras básico on-line", "Curso introdutório de Libras com certificado de 40 horas.",
            "Ver curso", "https://example.com/zyft-demo/libras",
            new[] { "libras", "inclusao", "acessibilidade", "atendimento" }),
    };

    public static AdDefinition? Find(string id) => All.FirstOrDefault(a => a.Id == id);
}
