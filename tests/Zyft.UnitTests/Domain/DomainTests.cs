using Zyft.Domain.Common;
using Zyft.Domain.Extension;
using Zyft.Domain.Imports;
using Zyft.Domain.Profiles;
using Zyft.Domain.Users;

namespace Zyft.UnitTests.Domain;

public class ProfileTests
{
    private static ExperienceData Exp(DateOnly start, DateOnly? end, bool current = false) =>
        new("Empresa", "Cargo", start, end, current, null, null, null);

    [Fact]
    public void Experiencia_com_termino_antes_do_inicio_viola_regra()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        var act = () => p.AddExperience(Exp(new DateOnly(2024, 5, 1), new DateOnly(2023, 1, 1)));
        act.Should().Throw<DomainException>().Which.Code.Should().Be("experience.invalid_period");
        p.Experiences.Should().BeEmpty();
    }

    [Fact]
    public void Experiencia_atual_nao_pode_ter_termino()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        var act = () => p.AddExperience(Exp(new DateOnly(2024, 5, 1), new DateOnly(2024, 6, 1), current: true));
        act.Should().Throw<DomainException>().Which.Code.Should().Be("experience.current_with_end");
    }

    [Fact]
    public void Termino_igual_ao_inicio_e_valido()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        p.AddExperience(Exp(new DateOnly(2024, 5, 1), new DateOnly(2024, 5, 1))).ProfileId.Should().Be(p.Id);
    }

    [Fact]
    public void Competencia_duplicada_ignorando_maiusculas_viola_regra()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        p.AddSkill("Excel", null);
        var act = () => p.AddSkill(" excel ", SkillLevel.Advanced);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("skill.duplicated");
    }

    [Fact]
    public void Remover_item_inexistente_devolve_falso()
    {
        Profile.CreateFor(Guid.NewGuid()).RemoveExperience(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Formacao_com_conclusao_antes_do_inicio_viola_regra()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        var act = () => p.AddEducation(new EducationData("UEM", "ADM", EducationLevel.Undergraduate, EducationStatus.Completed, new DateOnly(2020, 1, 1), new DateOnly(2019, 1, 1)));
        act.Should().Throw<DomainException>().Which.Code.Should().Be("education.invalid_period");
    }

    [Fact]
    public void Categoria_de_CNH_sem_possuir_CNH_viola_regra()
    {
        var p = Profile.CreateFor(Guid.NewGuid());
        var act = () => p.UpdateBasics(new ProfileBasicData { HasDriverLicense = false, DriverLicenseCategory = "B" });
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Pretensao_salarial_negativa_viola_regra()
    {
        var act = () => Profile.CreateFor(Guid.NewGuid()).UpdateBasics(new ProfileBasicData { SalaryExpectation = -1 });
        act.Should().Throw<DomainException>().Which.Code.Should().Be("profile.invalid_salary");
    }

    [Fact]
    public void Curso_com_carga_horaria_zero_viola_regra()
    {
        var act = () => Profile.CreateFor(Guid.NewGuid()).AddCourse(new CourseData("Excel", null, 0, null));
        act.Should().Throw<DomainException>();
    }
}

public class TokenAndCodeTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Refresh_token_expirado_ou_revogado_nao_esta_ativo()
    {
        var t = RefreshToken.Issue(Guid.NewGuid(), "h", Now.AddDays(7));
        t.IsActive(Now).Should().BeTrue();
        t.IsActive(Now.AddDays(8)).Should().BeFalse();
        t.Revoke(Now, "novo");
        t.IsActive(Now).Should().BeFalse();
        t.WasRotated.Should().BeTrue();
        t.ReplacedByHash.Should().Be("novo");
    }

    [Fact]
    public void Codigo_de_pareamento_expira_em_10_minutos_e_e_de_uso_unico()
    {
        var c = PairingCode.Create(Guid.NewGuid(), "hash", Now);
        c.IsValid(Now.AddMinutes(9)).Should().BeTrue();
        c.IsValid(Now.AddMinutes(10)).Should().BeFalse();
        c.Use(Now);
        var again = () => c.Use(Now);
        again.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("https://vagas.exemplo.com/candidatura/1?x=1", "https://vagas.exemplo.com")]
    [InlineData("http://localhost:5080/demo/", "http://localhost:5080")]
    public void Site_autorizado_guarda_so_a_origem(string input, string expected) =>
        AuthorizedSite.Create(Guid.NewGuid(), input).Origin.Should().Be(expected);

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://x.com")]
    [InlineData("nao e url")]
    public void Site_autorizado_recusa_origem_invalida(string input)
    {
        var act = () => AuthorizedSite.Create(Guid.NewGuid(), input);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Importacao_so_pode_ser_aplicada_uma_vez()
    {
        var i = ResumeImport.Create(Guid.NewGuid(), "cv.pdf", "{}");
        i.MarkApplied();
        var act = () => i.Discard();
        act.Should().Throw<DomainException>().Which.Code.Should().Be("import.not_pending");
    }

    [Fact]
    public void Email_do_usuario_e_normalizado() =>
        User.Create("  Ana@Exemplo.COM ", "Ana", "hash").Email.Should().Be("ana@exemplo.com");
}
