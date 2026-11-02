using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Zyft.Application.Auth;
using Zyft.Application.Common.Exceptions;
using Zyft.Domain.Audit;
using Zyft.Domain.Users;
using Zyft.Infrastructure.Email;
using Zyft.UnitTests.Support;

namespace Zyft.UnitTests.Auth;

/// <summary>Verificação de e-mail, termos, troca e recuperação de senha (domínio e handlers).</summary>
public class AccountSecurityTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    // ------------------------------------------------------------------ verificação de e-mail

    [Fact]
    public void Token_de_uso_unico_expira_e_nao_pode_ser_reusado()
    {
        var t = UserToken.Issue(Guid.NewGuid(), UserTokenPurpose.EmailVerification, "hash", Now.AddHours(24));
        t.IsUsable(Now).Should().BeTrue();
        t.IsUsable(Now.AddHours(24)).Should().BeFalse();
        t.Consume(Now);
        t.IsUsable(Now.AddMinutes(1)).Should().BeFalse();
        t.Consume(Now.AddHours(1));
        t.UsedAt.Should().Be(Now); // mantém a primeira data
    }

    [Fact]
    public void Marcar_email_verificado_e_idempotente()
    {
        var u = User.Create("a@b.com", "Ana", "hash");
        u.IsEmailVerified.Should().BeFalse();
        u.MarkEmailVerified(Now);
        u.MarkEmailVerified(Now.AddDays(1));
        u.EmailVerifiedAt.Should().Be(Now);
    }

    // ------------------------------------------------------------------ aceite dos termos

    [Fact]
    public void Cadastro_exige_aceite_e_versao_vigente_dos_termos()
    {
        var v = new RegisterValidator();
        v.TestValidate(new RegisterCommand("Ana", "ana@b.com", "Senha123")).ShouldHaveValidationErrorFor(x => x.AcceptTerms)
            .WithErrorMessage("Aceite os termos de uso e a política de privacidade.");
        v.TestValidate(new RegisterCommand("Ana", "ana@b.com", "Senha123", true, "1999-01-01")).ShouldHaveValidationErrorFor(x => x.TermsVersion);
        var ok = v.TestValidate(new RegisterCommand("Ana", "ana@b.com", "Senha123", true, Terms.CurrentVersion));
        ok.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Aceite_dos_termos_guarda_versao_e_data()
    {
        var u = User.Create("a@b.com", "Ana", "hash");
        u.AcceptTerms(Terms.CurrentVersion, Now);
        u.TermsVersion.Should().Be("2026-10-22");
        u.TermsAcceptedAt.Should().Be(Now);
    }

    private sealed class Ctx
    {
        public InMemoryRepository<User> Users { get; } = new();
        public InMemoryRepository<UserToken> Tokens { get; } = new();
        public InMemoryRepository<AuditEvent> Audit { get; } = new();
        public FakeTokenService TokenService { get; } = new();
        public CapturingEmailSender Emails { get; } = new();
        public FixedClock Clock { get; } = new(Now);
        public User User { get; }

        public Ctx()
        {
            User = User.Create("a@b.com", "Ana", "hash");
            Users.Add(User);
        }
    }

    [Fact]
    public async Task Verificar_com_token_valido_marca_a_conta_e_consome_o_token()
    {
        var c = new Ctx();
        var raw = await UserTokens.IssueAsync(c.Tokens, c.TokenService, Now, c.User.Id, UserTokenPurpose.EmailVerification, UserTokens.EmailVerificationLifetime, default);
        var handler = new VerifyEmailHandler(c.Tokens, c.Users, c.Audit, c.TokenService, new FakeUnitOfWork(), c.Clock);

        await handler.Handle(new VerifyEmailCommand(raw), default);

        c.User.EmailVerifiedAt.Should().Be(Now);
        c.Tokens.Items.Single().UsedAt.Should().Be(Now);
        c.Audit.Items.Should().ContainSingle(a => a.Action == AuditActions.EmailVerified);
        var again = () => handler.Handle(new VerifyEmailCommand(raw), default);
        (await again.Should().ThrowAsync<ValidationException>()).Which.Errors.Single().PropertyName.Should().Be("Token");
    }

    [Fact]
    public async Task Verificar_com_token_expirado_ou_desconhecido_e_erro_de_validacao()
    {
        var c = new Ctx();
        var raw = await UserTokens.IssueAsync(c.Tokens, c.TokenService, Now, c.User.Id, UserTokenPurpose.EmailVerification, UserTokens.EmailVerificationLifetime, default);
        c.Clock.UtcNow = Now.AddHours(25);
        var handler = new VerifyEmailHandler(c.Tokens, c.Users, c.Audit, c.TokenService, new FakeUnitOfWork(), c.Clock);

        await FluentActions.Invoking(() => handler.Handle(new VerifyEmailCommand(raw), default)).Should().ThrowAsync<ValidationException>();
        await FluentActions.Invoking(() => handler.Handle(new VerifyEmailCommand("nao-existe"), default)).Should().ThrowAsync<ValidationException>();
        c.User.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Reenviar_invalida_o_link_anterior_e_recusa_conta_ja_verificada()
    {
        var c = new Ctx();
        var first = await UserTokens.IssueAsync(c.Tokens, c.TokenService, Now, c.User.Id, UserTokenPurpose.EmailVerification, UserTokens.EmailVerificationLifetime, default);
        var resend = new ResendEmailVerificationHandler(c.Tokens, c.Users, c.TokenService, c.Emails, new FakeCurrentUser(c.User.Id), new FakeUnitOfWork(), c.Clock);

        await resend.Handle(new ResendEmailVerificationCommand(), default);

        c.Emails.Sent.Should().ContainSingle();
        c.Tokens.Items.Single(t => t.TokenHash == "h:" + first).IsUsable(Now).Should().BeFalse();
        c.Tokens.Items.Single(t => t.TokenHash == "h:" + c.Emails.Sent[0].Token).IsUsable(Now).Should().BeTrue();

        c.User.MarkEmailVerified(Now);
        (await FluentActions.Invoking(() => resend.Handle(new ResendEmailVerificationCommand(), default)).Should().ThrowAsync<ConflictException>())
            .Which.Code.Should().Be("email.already_verified");
    }

    // ------------------------------------------------------------------ troca de senha

    [Fact]
    public async Task Trocar_senha_revoga_refresh_tokens_e_extensoes_e_emite_par_novo()
    {
        var c = new Ctx();
        var tokens = new InMemoryRepository<RefreshToken>();
        tokens.Add(RefreshToken.Issue(c.User.Id, "a", Now.AddDays(7)));
        tokens.Add(RefreshToken.Issue(c.User.Id, "b", Now.AddDays(7), Guid.NewGuid()));
        var otherUser = RefreshToken.Issue(Guid.NewGuid(), "c", Now.AddDays(7));
        tokens.Add(otherUser);
        var devices = new InMemoryRepository<Zyft.Domain.Extension.ExtensionDevice>();
        devices.Add(Zyft.Domain.Extension.ExtensionDevice.Pair(c.User.Id, "Chrome", Now));
        var hasher = new Moq.Mock<Zyft.Application.Common.Interfaces.IPasswordHasher>();
        hasher.Setup(h => h.Verify("Senha123", "hash")).Returns(true);
        hasher.Setup(h => h.Hash("NovaSenha456")).Returns("novo-hash");
        var issuer = new Moq.Mock<Zyft.Application.Common.IAuthTokenIssuer>();
        var handler = new ChangePasswordHandler(c.Users, tokens, devices, c.Audit, hasher.Object, issuer.Object, new FakeCurrentUser(c.User.Id), new FakeUnitOfWork(), c.Clock);

        var wrong = () => handler.Handle(new ChangePasswordCommand("Errada123", "NovaSenha456"), default);
        (await wrong.Should().ThrowAsync<ValidationException>()).Which.Errors.Single().PropertyName.Should().Be("CurrentPassword");
        c.User.PasswordHash.Should().Be("hash");

        await handler.Handle(new ChangePasswordCommand("Senha123", "NovaSenha456"), default);

        c.User.PasswordHash.Should().Be("novo-hash");
        tokens.Items.Where(t => t.UserId == c.User.Id).Should().OnlyContain(t => t.RevokedAt == Now);
        otherUser.RevokedAt.Should().BeNull();
        devices.Items.Single().IsActive.Should().BeFalse();
        c.Audit.Items.Select(a => a.Action).Should().Contain(new[] { AuditActions.PasswordChanged, AuditActions.DeviceRevoked });
        issuer.Verify(i => i.Issue(c.User, null), Moq.Times.Once);
    }

    [Fact]
    public void Nova_senha_igual_a_atual_e_recusada()
    {
        new ChangePasswordValidator().TestValidate(new ChangePasswordCommand("Senha123", "Senha123"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword).WithErrorMessage("A nova senha deve ser diferente da senha atual.");
        new ChangePasswordValidator().TestValidate(new ChangePasswordCommand("", "Senha456")).ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    // ------------------------------------------------------------------ recuperação de senha

    private static ResetPasswordHandler ResetHandler(Ctx c, InMemoryRepository<RefreshToken> tokens, string newHash = "novo-hash")
    {
        var hasher = new Moq.Mock<Zyft.Application.Common.Interfaces.IPasswordHasher>();
        hasher.Setup(h => h.Hash(Moq.It.IsAny<string>())).Returns(newHash);
        return new ResetPasswordHandler(c.Tokens, c.Users, tokens, new InMemoryRepository<Zyft.Domain.Extension.ExtensionDevice>(), c.Audit, c.TokenService, hasher.Object, new FakeUnitOfWork(), c.Clock);
    }

    [Fact]
    public async Task Esqueci_a_senha_so_envia_para_conta_existente_com_validade_de_30_minutos()
    {
        var c = new Ctx();
        var handler = new ForgotPasswordHandler(c.Users, c.Tokens, c.TokenService, c.Emails, new FakeUnitOfWork(), c.Clock);

        await handler.Handle(new ForgotPasswordCommand("ninguem@b.com"), default);
        c.Emails.Sent.Should().BeEmpty();
        c.Tokens.Items.Should().BeEmpty();

        await handler.Handle(new ForgotPasswordCommand(" A@B.com "), default);
        c.Emails.Sent.Should().ContainSingle(e => e.Kind == "reset");
        var t = c.Tokens.Items.Single();
        t.Purpose.Should().Be(UserTokenPurpose.PasswordReset);
        t.ExpiresAt.Should().Be(Now.AddMinutes(30));
    }

    [Fact]
    public async Task Redefinir_expirado_falha_e_valido_troca_senha_e_revoga_sessoes()
    {
        var c = new Ctx();
        var tokens = new InMemoryRepository<RefreshToken>();
        tokens.Add(RefreshToken.Issue(c.User.Id, "a", Now.AddDays(7)));
        var raw = await UserTokens.IssueAsync(c.Tokens, c.TokenService, Now, c.User.Id, UserTokenPurpose.PasswordReset, UserTokens.PasswordResetLifetime, default);

        c.Clock.UtcNow = Now.AddMinutes(31);
        await FluentActions.Invoking(() => ResetHandler(c, tokens).Handle(new ResetPasswordCommand(raw, "Nova12345"), default)).Should().ThrowAsync<ValidationException>();
        c.User.PasswordHash.Should().Be("hash");

        c.Clock.UtcNow = Now.AddMinutes(10);
        await ResetHandler(c, tokens).Handle(new ResetPasswordCommand(raw, "Nova12345"), default);
        c.User.PasswordHash.Should().Be("novo-hash");
        c.User.IsEmailVerified.Should().BeTrue();
        tokens.Items.Single().RevokedAt.Should().NotBeNull();
        c.Tokens.Items.Single().UsedAt.Should().NotBeNull();
        c.Audit.Items.Should().Contain(a => a.Action == AuditActions.PasswordReset);
    }

    [Fact]
    public async Task Envio_de_desenvolvimento_registra_so_o_link_sem_email_nem_nome()
    {
        var logger = new ListLogger<LoggingEmailSender>();
        var sender = new LoggingEmailSender(Options.Create(new AppOptions { WebBaseUrl = "https://zyft.exemplo/" }), logger);

        await sender.SendEmailVerificationAsync("pessoa@privado.com", "Nome Privado", "tok123");

        var line = logger.Lines.Should().ContainSingle().Subject;
        line.Should().Contain("https://zyft.exemplo/#/verificar?token=tok123");
        line.Should().NotContain("pessoa@privado.com").And.NotContain("Nome Privado");

        await sender.SendPasswordResetAsync("pessoa@privado.com", "Nome Privado", "tok456");
        logger.Lines[^1].Should().Contain("https://zyft.exemplo/#/redefinir-senha?token=tok456").And.NotContain("pessoa@privado.com");
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullLogger.Instance.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Lines.Add(formatter(state, exception));
    }
}
