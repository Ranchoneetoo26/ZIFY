using System.Reflection;
using FluentValidation;
using Zyft.Application;
using Zyft.Application.Answers;
using Zyft.Application.Common;
using Zyft.Application.Sites;
using Zyft.Domain.Answers;
using Zyft.Domain.Common;
using Zyft.Domain.Profiles;

namespace Zyft.UnitTests.V2;

public class QaValidationTests
{
    [Fact]
    public void ToBasicData_devolve_todos_os_campos_de_ProfileBasicData()
    {
        object filled = new ProfileBasicData();
        foreach (var prop in typeof(ProfileBasicData).GetProperties().Where(p => p.CanWrite && p.GetIndexParameters().Length == 0))
            prop.SetValue(filled, Sample(prop));

        var profile = Profile.CreateFor(Guid.NewGuid());
        profile.UpdateBasics((ProfileBasicData)filled);

        profile.ToBasicData().Should().Be((ProfileBasicData)filled, "campo novo precisa entrar em Profile.ToBasicData e UpdateBasics");
    }

    private static object Sample(PropertyInfo prop)
    {
        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        if (prop.Name == nameof(ProfileBasicData.SalaryType)) return "PJ";
        if (prop.Name == nameof(ProfileBasicData.Email)) return "ana@exemplo.com";
        if (prop.Name == nameof(ProfileBasicData.DriverLicenseCategory)) return "AB";
        if (type == typeof(string)) return prop.Name;
        if (type == typeof(DateOnly)) return new DateOnly(2000, 1, 2);
        if (type == typeof(decimal)) return 123.45m;
        if (type == typeof(bool)) return true;
        if (type.IsEnum) return Enum.GetValues(type).GetValue(1)!;
        throw new InvalidOperationException($"Sem valor de exemplo para {prop.Name} ({type}).");
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" \t\n", true)]
    [InlineData("​‌‍﻿⁠", true)]
    [InlineData("­ㅤ⠀", true)]
    [InlineData("Ana", false)]
    [InlineData("​A", false)]
    public void VisibleText_identifica_texto_sem_nada_visivel(string? text, bool blank) => VisibleText.IsBlank(text).Should().Be(blank);

    [Fact]
    public void Dominio_recusa_texto_so_com_invisiveis()
    {
        var act = () => ApprovedAnswer.Approve(Guid.NewGuid(), "​​", "Resposta", DateTime.UtcNow);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("answer.question_required");
    }

    [Theory]
    [InlineData("ana@exemplo.com")]
    [InlineData("ana.souza+vagas@exemplo.com.br")]
    [InlineData("demo@zyft.local")]
    [InlineData("joão@exemplo.com")]
    public void Emails_validos(string email) => Emails.IsValid(email).Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("a@b")]
    [InlineData("a@b..com")]
    [InlineData("a@b.co.")]
    [InlineData("a@.com")]
    [InlineData("a..b@x.com")]
    [InlineData(".a@x.com")]
    [InlineData("<b>@x.com")]
    [InlineData("a b@x.com")]
    [InlineData("a@b@x.com")]
    [InlineData("a@-b.com")]
    [InlineData("a@b.c")]
    [InlineData("a​@x.com")]
    public void Emails_invalidos(string email) => Emails.IsValid(email).Should().BeFalse();

    [Theory]
    [InlineData("linkedin.com/in/ana", "https://linkedin.com/in/ana")]
    [InlineData("  www.ana.dev ", "https://www.ana.dev")]
    [InlineData("localhost:3000", "https://localhost:3000")]
    [InlineData("http://ana.dev", "http://ana.dev")]
    [InlineData("ftp://x.com", "ftp://x.com")]
    [InlineData("javascript:alert(1)", "javascript:alert(1)")]
    public void Urls_completa_https_so_sem_esquema(string input, string expected) => Urls.Complete(input).Should().Be(expected);

    [Fact]
    public void Validadores_tem_nome_em_portugues_para_todo_campo()
    {
        ValidationLocalization.Configure();
        var validators = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsGenericType: false } && typeof(IValidator).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) is not null)
            .Select(t => (IValidator)Activator.CreateInstance(t)!);

        var unnamed = validators.SelectMany(v => v.CreateDescriptor().GetMembersWithValidators().Select(m => m.Key))
            .Where(m => !string.IsNullOrEmpty(m) && ValidationLocalization.DisplayName(m) == ValidationLocalization.Fallback)
            .Distinct().ToList();

        string.Join(", ", unnamed).Should().BeEmpty();
    }

    [Fact]
    public void Nome_sem_cadastro_nunca_vira_identificador_em_ingles() =>
        ValidationLocalization.DisplayName("SomethingNew").Should().Be(ValidationLocalization.Fallback);

    [Fact]
    public void Mensagem_padrao_em_portugues()
    {
        ValidationLocalization.Configure();
        var message = new AddAuthorizedSiteValidator().Validate(new AddAuthorizedSiteCommand(new string('a', 301))).Errors.First().ErrorMessage;
        message.Should().Contain("Endereço do site").And.Contain("300").And.NotContain("characters");

        var text = new CreateAnswerValidator().Validate(new CreateAnswerCommand { Question = "P", Text = new string('a', 5001) }).Errors.Single().ErrorMessage;
        text.Should().Contain("Texto").And.Contain("5000");
    }
}
