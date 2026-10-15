using System.Text.RegularExpressions;

namespace Zyft.Application.Forms;

/// <summary>
/// Regras determinísticas PT/EN do de-para (porte de <c>mapper.js</c>, ADR-0006). Todas as regex rodam
/// sobre texto normalizado (sem acento, minúsculo, sem pontuação).
/// </summary>
internal static class FieldRules
{
    private const RegexOptions O = RegexOptions.Compiled | RegexOptions.CultureInvariant;

    public static readonly Regex Sensitive = new(@"\b(genero|sexo|raca|etnia|cor da pele|cor raca|deficiencia|pcd|pessoa com deficiencia|religi\w*|orientacao sexual|identidade de genero|saude|doenca|laudo|nome social|estado civil|gestante|filhos|gender|race|ethnicity|disability|religion|sexual orientation|health)\b", O);
    public static readonly Regex Declaration = new(@"\b(declaro|aceito|concordo|termos|politica de privacidade|autorizo|consentimento|lgpd|li e|i agree|i accept|terms|privacy policy)\b", O);
    public static readonly Regex Captcha = new(@"captcha|recaptcha|codigo de seguranca|verification code|codigo de verificacao|security code|nao sou um robo|not a robot", O);
    public static readonly Regex PasswordLike = new(@"\b(senha|password|passcode|pin)\b", O);

    /// <summary>Tipos que nunca recebem valor.</summary>
    public static readonly HashSet<string> IgnoredTypes = new(StringComparer.OrdinalIgnoreCase) { "password", "hidden", "submit", "button", "image", "reset" };

    /// <summary>Campos de nível superior em ordem de prioridade (o primeiro que casar vence).</summary>
    public static readonly (string Key, Regex Re)[] TopRules =
    {
        ("personal.lastName", new(@"\b(sobrenome|last name|surname|family name)\b", O)),
        ("personal.firstName", new(@"\b(primeiro nome|first name|given name)\b", O)),
        ("personal.fullName", new(@"\b(nome completo|full name|seu nome|nome do candidato|candidate name|your name)\b|^nome$|^name$", O)),
        ("personal.email", new(@"\be ?mail\b", O)),
        ("personal.phone", new(@"\b(telefone|celular|whatsapp|phone|fone|mobile|contato telefonico|telephone)\b", O)),
        ("personal.birthDate", new(@"\b(nascimento|birth|data de nasc|birthday|date of birth)\b", O)),
        ("personal.cpf", new(@"\bcpf\b", O)),
        ("personal.zipCode", new(@"\b(cep|zip|postal code|codigo postal|zip code)\b", O)),
        ("personal.linkedin", new(@"\blinkedin\b", O)),
        ("personal.github", new(@"\bgithub\b", O)),
        ("personal.portfolio", new(@"\b(portfolio|site pessoal|website|pagina pessoal|personal site)\b", O)),
        ("objective.headline", new(@"\b(titulo profissional|headline|titulo do perfil)\b", O)),
        ("objective.desiredRoles", new(@"\b(cargo desejado|cargo pretendido|cargos desejados|vaga pretendida|cargo de interesse|desired (role|position)|position applied)\b", O)),
        ("objective.areas", new(@"\b(areas? de interesse|area de atuacao|areas? pretendidas?|areas? of interest)\b", O)),
        ("objective.seniority", new(@"\b(senioridade|nivel profissional|nivel de experiencia|seniority|experience level)\b", O)),
        ("objective.workModel", new(@"\b(modelo de trabalho|modalidade|regime de trabalho|work model|trabalho remoto|work arrangement)\b", O)),
        ("objective.locationPreference", new(@"\b(preferencia de local|local de preferencia|onde deseja trabalhar|preferred location)\b", O)),
        ("objective.summary", new(@"\b(resumo|objetivo profissional|sobre voce|sobre mim|apresentacao|about you|about me|summary|perfil profissional|carta de apresentacao|cover letter)\b", O)),
        ("skills", new(@"\b(habilidades|competencias|skills|qualificacoes|conhecimentos)\b", O)),
        ("preferences.salaryExpectation", new(@"\b(pretensao salarial|salario pretendido|expectativa salarial|salary|remuneracao pretendida|salary expectation)\b", O)),
        ("preferences.availability", new(@"\b(disponibilidade para inicio|inicio imediato|availability|quando pode comecar|data de disponibilidade|disponibilidade de inicio|start date availability|notice period)\b", O)),
        ("preferences.travel", new(@"\b(viagens?|viajar|travel)\b", O)),
        ("preferences.relocation", new(@"\b(mudanca|mudar de cidade|relocat\w*|realocacao)\b", O)),
        ("preferences.contractTypes", new(@"\b(tipo de contrato|tipos de contrato|regime de contratacao|contract type|employment type)\b", O)),
        ("preferences.driverLicense", new(@"\b(cnh|carteira de motorista|carteira de habilitacao|habilitacao|driver\w*)\b", O)),
        ("personal.country", new(@"\b(pais|country)\b", O)),
        ("personal.city", new(@"\b(cidade|city|municipio)\b", O)),
        ("personal.state", new(@"\b(estado|uf|state|provincia)\b", O)),
        ("personal.address", new(@"\b(endereco|logradouro|rua|address)\b", O)),
    };

    /// <summary>Rótulos genéricos que também aparecem dentro de grupos (ex.: "Cidade" da empresa).</summary>
    public static readonly HashSet<string> GenericTop = new() { "personal.fullName", "personal.city", "personal.state", "personal.country", "personal.address", "skills" };

    public sealed record Group(string Name, Regex Section, Regex Unique, (string Sub, Regex Re)[] Fields, string IdentityField);

    /// <summary>Grupos repetíveis. O grupo vem da seção da página (fieldset/título) e o subcampo do rótulo.</summary>
    public static readonly Group[] Groups =
    {
        new("experiences",
            new(@"\b(experiencias?|experiencia profissional|historico profissional|empregos?|work experience|employment|professional experience)\b", O),
            new(@"\b(empresa|empregador|company|employer)\b", O),
            new (string, Regex)[]
            {
                ("company", new(@"\b(empresa|empregador|company|employer|nome da empresa|organizacao|organization)\b", O)),
                ("activities", new(@"\b(atividades|atribuicoes|responsabilidades|descricao|realizacoes|description|responsibilities)\b", O)),
                ("results", new(@"\b(resultados|conquistas|achievements)\b", O)),
                ("tools", new(@"\b(ferramentas|tecnologias|tools)\b", O)),
                ("end", new(@"\b(fim|saida|termino|desligamento|end date|data final)\b", O)),
                ("start", new(@"\b(inicio|entrada|admissao|start date|data inicial)\b", O)),
                ("role", new(@"\b(cargo|funcao|position|job title|titulo|title|role)\b", O)),
            },
            "company"),
        new("education",
            new(@"\b(formacao|escolaridade|educacao|academica|education|academic)\b", O),
            new(@"\b(instituicao de ensino|faculdade|universidade|escola|university|school)\b", O),
            new (string, Regex)[]
            {
                ("institution", new(@"\b(instituicao|escola|faculdade|universidade|institution|school|university)\b", O)),
                ("degree", new(@"\b(nivel|grau|escolaridade|degree)\b", O)),
                ("status", new(@"\b(situacao|status|andamento)\b", O)),
                ("end", new(@"\b(conclusao|termino|fim|end date|formatura|previsao|graduation)\b", O)),
                ("start", new(@"\b(inicio|start date|ingresso)\b", O)),
                ("course", new(@"\b(curso|graduacao|course|field of study|area de estudo|major)\b", O)),
            },
            "institution"),
        new("courses",
            new(@"\b(cursos|cursos complementares|cursos livres|capacitac\w*|qualificacoes complementares)\b", O),
            new(@"\b(carga horaria|curso complementar)\b", O),
            new (string, Regex)[]
            {
                ("hours", new(@"\b(carga horaria|horas|hours)\b", O)),
                ("institution", new(@"\b(instituicao|escola|plataforma|institution)\b", O)),
                ("year", new(@"\b(ano|conclusao|year)\b", O)),
                ("name", new(@"\b(curso|nome|course|name)\b", O)),
            },
            "name"),
        new("certifications",
            new(@"\b(certificac\w*|certificados|certifications)\b", O),
            new(@"\b(certificacao|certification)\b", O),
            new (string, Regex)[]
            {
                ("issuer", new(@"\b(emissor|emitido|issuer|instituicao)\b", O)),
                ("year", new(@"\b(ano|data|year)\b", O)),
                ("name", new(@"\b(certificacao|nome|name|certification)\b", O)),
            },
            "name"),
        new("languages",
            new(@"\b(idiomas?|linguas?|languages?)\b", O),
            new(@"\b(idioma|lingua estrangeira|language)\b", O),
            new (string, Regex)[]
            {
                ("level", new(@"\b(nivel|proficiencia|fluencia|level|proficiency)\b", O)),
                ("language", new(@"\b(idioma|lingua|language)\b", O)),
            },
            "language"),
    };

    public static readonly Regex Yes = new(@"^(sim|yes|s|true|disponivel|tenho|possuo)\b", O);
    public static readonly Regex No = new(@"^(nao|no|n|false|indisponivel|nao tenho|nao possuo)\b", O);
    public static readonly Regex Placeholder = new(@"^(selecione|escolha|select|choose)", O);
    public static readonly Regex AchievementsHint = new(@"realiza|resultado|conquista|achievement", O);
}
