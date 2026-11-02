using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using FluentValidation;

namespace Zyft.Application.Common;

/// <summary>
/// Toda mensagem padrão do FluentValidation sai em pt-BR, com o nome do campo em português. Campo sem nome cadastrado
/// aparece como "Campo", nunca como o identificador em inglês. Precisa rodar antes de criar os validadores.
/// </summary>
public static class ValidationLocalization
{
    public const string Fallback = "Campo";

    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal)
    {
        ["Activities"] = "Atividades", ["Areas"] = "Setores de interesse", ["Availability"] = "Disponibilidade para início", ["City"] = "Cidade",
        ["Code"] = "Código", ["Company"] = "Empresa", ["Content"] = "Arquivo", ["ContractTypes"] = "Tipos de contrato aceitos", ["Country"] = "País",
        ["Course"] = "Curso", ["CurrentValue"] = "Valor atual", ["DesiredRoles"] = "Cargos desejados", ["DeviceName"] = "Nome do navegador",
        ["DriverLicenseCategory"] = "Categoria da CNH", ["Email"] = "E-mail", ["FileName"] = "Arquivo", ["FullName"] = "Nome completo",
        ["GithubUrl"] = "GitHub", ["Headline"] = "Título profissional", ["Hours"] = "Carga horária", ["Id"] = "Identificador",
        ["Institution"] = "Instituição", ["JobText"] = "Texto da vaga", ["JobTitle"] = "Cargo", ["JobUrl"] = "Endereço da vaga", ["Kind"] = "Tipo de dado",
        ["Label"] = "Nome do campo", ["Level"] = "Nível", ["LinkedinUrl"] = "LinkedIn", ["Name"] = "Nome", ["Notes"] = "Anotações",
        ["Origin"] = "Endereço do site", ["PageUrl"] = "Endereço da página", ["Password"] = "Senha", ["Phone"] = "Telefone", ["Placement"] = "Posição do anúncio",
        ["Portal"] = "Portal", ["PortfolioUrl"] = "Portfólio", ["ProfessionalRegistration"] = "Registro profissional", ["Question"] = "Pergunta",
        ["RefreshToken"] = "Token de sessão", ["Results"] = "Resultados", ["Role"] = "Cargo", ["Section"] = "Seção", ["Seniority"] = "Momento da carreira",
        ["State"] = "Estado", ["Summary"] = "Resumo profissional", ["Text"] = "Texto", ["Title"] = "Título", ["Tools"] = "Ferramentas", ["Topic"] = "Assunto",
        ["Url"] = "Endereço", ["Value"] = "Valor", ["WorkArea"] = "Área de atuação", ["ZipCode"] = "CEP", ["WorkModel"] = "Modelo de trabalho",
        ["SalaryExpectation"] = "Pretensão salarial", ["SalaryType"] = "Tipo de pretensão", ["BirthDate"] = "Data de nascimento", ["Status"] = "Situação",
        ["Proficiency"] = "Nível", ["StartDate"] = "Início", ["EndDate"] = "Fim", ["CompletedAt"] = "Concluído em", ["Type"] = "Tipo", ["Fields"] = "Campos",
        ["MaxLength"] = "Limite de caracteres", ["FieldsFilled"] = "Campos preenchidos", ["Period"] = "Período", ["Scope"] = "Opção", ["Purpose"] = "Finalidade",
        ["Answered"] = "Etapas respondidas", ["Skipped"] = "Etapas puladas", ["LastStep"] = "Última etapa", ["Options"] = "Opções", ["Profile"] = "Dados básicos", ["Experiences"] = "Experiências", ["Educations"] = "Formações", ["Courses"] = "Cursos", ["Skills"] = "Competências", ["Languages"] = "Idiomas",
        ["Token"] = "Código de verificação",
    };

    public static void Configure()
    {
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("pt-BR");
        ValidatorOptions.Global.DisplayNameResolver = ResolveName;
    }

    /// <summary>Nome em português do campo; <see cref="Fallback"/> quando não há cadastro.</summary>
    public static string DisplayName(string member) => Names.GetValueOrDefault(member, Fallback);

    private static string ResolveName(Type type, MemberInfo? member, LambdaExpression? expression) => DisplayName(member?.Name ?? string.Empty);
}
