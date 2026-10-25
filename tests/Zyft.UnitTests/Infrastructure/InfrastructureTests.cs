using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zyft.Application.Common;
using Zyft.Application.Imports;
using Zyft.Domain.Profiles;
using Zyft.Domain.Users;
using Zyft.Infrastructure.Security;
using Zyft.UnitTests.Support;

namespace Zyft.UnitTests.Infrastructure;

public class InfrastructureTests
{
    private static readonly string Key = Convert.ToBase64String(new byte[32]);

    [Fact]
    public void AES_GCM_cifra_com_nonce_aleatorio_e_decifra()
    {
        var p = new AesGcmSensitiveDataProtector(Options.Create(new EncryptionOptions { Key = Key }));
        var a = p.Protect("Feminino");
        var b = p.Protect("Feminino");
        a.Should().NotBe(b).And.NotContain("Feminino");
        p.Unprotect(a).Should().Be("Feminino");
    }

    [Fact]
    public void Chave_AES_com_tamanho_errado_falha_cedo()
    {
        var act = () => new AesGcmSensitiveDataProtector(Options.Create(new EncryptionOptions { Key = Convert.ToBase64String(new byte[31]) }));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void JWT_tem_sub_scope_device_e_expira_em_15_minutos()
    {
        var now = DateTime.UtcNow;
        var svc = new JwtTokenService(Options.Create(new JwtOptions { Secret = new string('x', 40) }), new FixedClock(now));
        var user = User.Create("a@b.com", "A", "h");
        var device = Guid.NewGuid();
        var t = svc.CreateAccessToken(user, "extension", device);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(t.Token);
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "extension");
        jwt.Claims.Should().Contain(c => c.Type == "device_id" && c.Value == device.ToString());
        (t.ExpiresAt - now).Should().Be(TimeSpan.FromMinutes(15));
        svc.Hash("abc").Should().HaveLength(64).And.Be(svc.Hash("abc"));
    }

    [Fact]
    public void BCrypt_verifica_e_nao_quebra_com_hash_invalido()
    {
        var h = new BCryptPasswordHasher();
        var hash = h.Hash("Senha123");
        h.Verify("Senha123", hash).Should().BeTrue();
        h.Verify("Senha124", hash).Should().BeFalse();
        h.Verify("Senha123", "nao-e-hash").Should().BeFalse();
    }

    [Theory]
    [InlineData("07/2023", 2023, 7)]
    [InlineData("2023-07", 2023, 7)]
    [InlineData("2023-07-15", 2023, 7)]
    [InlineData("15/07/2023", 2023, 7)]
    [InlineData("2023", 2023, 1)]
    public void Datas_de_curriculo_sao_interpretadas(string text, int year, int month)
    {
        Dates.TryParseLoose(text, out var d).Should().BeTrue();
        (d.Year, d.Month).Should().Be((year, month));
    }

    [Theory]
    [InlineData("Superior incompleto", EducationStatus.Incomplete)]
    [InlineData("Concluído", EducationStatus.Completed)]
    [InlineData("cursando", EducationStatus.InProgress)]
    [InlineData("InProgress", EducationStatus.InProgress)]
    public void Rotulos_pt_BR_viram_enum(string text, EducationStatus expected)
    {
        DisplayNames.TryParse<EducationStatus>(text, out var v).Should().BeTrue();
        v.Should().Be(expected);
    }

    [Fact]
    public async Task Importacao_sem_IA_extrai_contatos_competencias_e_idiomas_sem_sensiveis()
    {
        const string cv = """
            ANA PAULA SOUZA
            ana.souza@email.com | (44) 99999-2222 | linkedin.com/in/ana-exemplo
            Gênero: feminino
            Competências
            Excel, Contas a pagar; Atendimento ao cliente
            Idiomas
            Inglês básico
            """;
        var (p, ai) = await new ResumeParser(new ScriptedLlm { IsEnabled = false }, NullLogger<ResumeParser>.Instance).ParseAsync(cv);
        ai.Should().BeFalse();
        p.Profile.FullName.Should().Be("Ana Paula Souza");
        p.Profile.Email.Should().Be("ana.souza@email.com");
        p.Profile.Phone.Should().Be("(44) 99999-2222");
        p.Skills.Select(s => s.Name).Should().Contain(new[] { "Excel", "Contas a pagar", "Atendimento ao cliente" });
        p.Languages.Should().ContainSingle(l => l.Name == "Inglês" && l.Proficiency == "Basic");
        System.Text.Json.JsonSerializer.Serialize(p).Should().NotContainEquivalentOf("feminino");
    }
}
