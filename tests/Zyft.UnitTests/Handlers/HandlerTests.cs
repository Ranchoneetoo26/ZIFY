using FluentValidation.TestHelper;
using Moq;
using Zyft.Application.Auth;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Application.Consents;
using Zyft.Application.Jobs;
using Zyft.Domain.Audit;
using Zyft.Domain.Consents;
using Zyft.Domain.Profiles;
using Zyft.Domain.Sensitive;
using Zyft.Domain.Users;
using Zyft.UnitTests.Support;

namespace Zyft.UnitTests.Handlers;

public class HandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Analise_de_vaga_sem_consentimento_e_bloqueada_na_API_e_nao_chama_a_IA()
    {
        var uid = Guid.NewGuid();
        var consents = new InMemoryRepository<Consent>();
        consents.Add(Consent.Create(uid, ConsentPurpose.JobAnalysis, false));
        var analyzer = new Mock<IJobAnalyzer>(MockBehavior.Strict);
        var handler = new AnalyzeJobHandler(new InMemoryProfileRepository(), new InMemoryRepository<AuditEvent>(), new ConsentGuard(consents),
            analyzer.Object, new FakeCurrentUser(uid), new FakeUnitOfWork(), new FixedClock(Now), Mock.Of<Zyft.Application.Billing.IUsageMeter>());

        var act = () => handler.Handle(new AnalyzeJobCommand("texto da vaga"), default);

        (await act.Should().ThrowAsync<ConsentRequiredException>()).Which.Code.Should().Be("consent_required");
        analyzer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Desligar_consentimento_de_sensiveis_apaga_os_dados_sensiveis()
    {
        var uid = Guid.NewGuid();
        var consents = new InMemoryRepository<Consent>();
        consents.Add(Consent.Create(uid, ConsentPurpose.SensitiveData, true));
        var sensitive = new InMemoryRepository<SensitiveData>();
        sensitive.Add(SensitiveData.Create(uid, SensitiveDataKind.Gender, "cifrado"));
        sensitive.Add(SensitiveData.Create(Guid.NewGuid(), SensitiveDataKind.Gender, "de-outra-pessoa"));
        var audit = new InMemoryRepository<AuditEvent>();
        var uow = new FakeUnitOfWork();

        var dto = await new SetConsentHandler(consents, sensitive, audit, new FakeCurrentUser(uid), uow, new FixedClock(Now))
            .Handle(new SetConsentCommand(ConsentPurpose.SensitiveData, false), default);

        dto.Granted.Should().BeFalse();
        sensitive.Items.Should().ContainSingle().Which.UserId.Should().NotBe(uid);
        audit.Items.Should().Contain(a => a.Action == AuditActions.SensitiveDataErased && a.FieldCount == 1);
        uow.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Refresh_token_reutilizado_revoga_a_cadeia_inteira()
    {
        var user = User.Create("a@b.com", "A", "h");
        var users = new InMemoryRepository<User>();
        users.Add(user);
        var tokens = new InMemoryRepository<RefreshToken>();
        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(t => t.Hash(It.IsAny<string>())).Returns<string>(s => "h:" + s);
        var n = 0;
        tokenService.Setup(t => t.GenerateOpaqueToken()).Returns(() => $"rt{++n}");
        tokenService.Setup(t => t.CreateAccessToken(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<Guid?>())).Returns(new AccessToken("jwt", Now.AddMinutes(15)));
        tokenService.SetupGet(t => t.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));
        var clock = new FixedClock(Now);
        var issuer = new AuthTokenIssuer(tokenService.Object, tokens, clock);
        var handler = new RefreshTokenHandler(tokens, users, new InMemoryRepository<Zyft.Domain.Extension.ExtensionDevice>(), tokenService.Object, issuer, new FakeUnitOfWork(), clock);

        var first = issuer.Issue(user);                                            // rt1
        var second = await handler.Handle(new RefreshTokenCommand(first.RefreshToken), default);   // rt1 -> rt2
        var third = await handler.Handle(new RefreshTokenCommand(second.RefreshToken), default);   // rt2 -> rt3

        // Atacante reusa rt1 (já trocado): 401 e toda a cadeia (rt2, rt3) é revogada.
        var reuse = () => handler.Handle(new RefreshTokenCommand(first.RefreshToken), default);
        await reuse.Should().ThrowAsync<UnauthorizedException>();
        tokens.Items.Should().OnlyContain(t => t.RevokedAt != null);
        var legit = () => handler.Handle(new RefreshTokenCommand(third.RefreshToken), default);
        await legit.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_com_senha_errada_e_401_sem_revelar_se_o_email_existe()
    {
        var users = new InMemoryRepository<User>();
        users.Add(User.Create("a@b.com", "A", "hash-certo"));
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("dummy");
        var handler = new LoginHandler(users, hasher.Object, Mock.Of<IAuthTokenIssuer>(), new FakeUnitOfWork());

        var wrongPass = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new LoginCommand("a@b.com", "x"), default));
        var noUser = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new LoginCommand("z@b.com", "x"), default));
        wrongPass.Message.Should().Be(noUser.Message);
        hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2)); // gasta o mesmo tempo nos dois casos
    }

    [Fact]
    public async Task Cadastro_cria_perfil_e_consentimentos_com_sensiveis_desligado()
    {
        var users = new InMemoryRepository<User>();
        var consents = new InMemoryRepository<Consent>();
        var profiles = new InMemoryProfileRepository();
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        var issuer = new Mock<IAuthTokenIssuer>();
        var handler = new RegisterHandler(users, consents, new InMemoryRepository<AuditEvent>(), profiles, hasher.Object, issuer.Object, new FakeUnitOfWork(), new FixedClock(Now));

        await handler.Handle(new RegisterCommand("Ana", "Ana@B.com", "Senha123"), default);

        users.Items.Single().Email.Should().Be("ana@b.com");
        profiles.Items.Single().FullName.Should().Be("Ana");
        consents.Items.Should().HaveCount(3).And.OnlyContain(c => !c.Granted); // opt-in (D-01)
        var dup = () => handler.Handle(new RegisterCommand("Outra", "ana@b.com", "Senha123"), default);
        await dup.Should().ThrowAsync<ConflictException>();
    }

    [Theory]
    [InlineData("curta1", false)]
    [InlineData("semnumeros", false)]
    [InlineData("12345678", false)]
    [InlineData("Senha123", true)]
    public void Senha_exige_8_caracteres_com_letra_e_numero(string password, bool valid)
    {
        var r = new RegisterValidator().TestValidate(new RegisterCommand("Ana", "ana@b.com", password));
        if (valid) r.ShouldNotHaveValidationErrorFor(x => x.Password);
        else r.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Mapeamento_recusa_mais_de_300_campos()
    {
        var fields = Enumerable.Range(0, 301).Select(i => new Zyft.Application.Forms.FormField { Id = $"f{i}" }).ToList();
        new Zyft.Application.Forms.MapFieldsValidator().TestValidate(new Zyft.Application.Forms.MapFieldsCommand(null, fields))
            .ShouldHaveValidationErrorFor(x => x.Fields);
    }
}
