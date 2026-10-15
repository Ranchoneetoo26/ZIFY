using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using Zyft.Application.Common;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Audit;
using Zyft.Domain.Extension;
using Zyft.Domain.Users;

namespace Zyft.Application.Extension;

public sealed record PairingCodeDto(string Code, DateTime ExpiresAt);

public sealed record DeviceDto(Guid Id, string Name, DateTime PairedAt, DateTime LastSeenAt, DateTime? RevokedAt, bool Active)
{
    public static DeviceDto From(ExtensionDevice d) => new(d.Id, d.Name, d.PairedAt, d.LastSeenAt, d.RevokedAt, d.IsActive);
}

public sealed record PairResult(DeviceDto Device, AuthResult Tokens);

// ------------------------------------------------------------------ POST /extension/pairing-codes (web logada)

public sealed record CreatePairingCodeCommand : IRequest<PairingCodeDto>;

public sealed class CreatePairingCodeHandler(IRepository<PairingCode> codes, ITokenService tokens, ICurrentUser current, IUnitOfWork uow, IClock clock)
    : IRequestHandler<CreatePairingCodeCommand, PairingCodeDto>
{
    public async Task<PairingCodeDto> Handle(CreatePairingCodeCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var now = clock.UtcNow;

        // Um código ativo por vez: os anteriores deixam de valer.
        foreach (var old in await codes.ListAsync(c => c.UserId == uid && c.UsedAt == null && c.ExpiresAt > now, ct))
            old.Use(now);

        string code;
        string hash;
        var attempts = 0;
        do
        {
            code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            hash = tokens.Hash(code);
        }
        while (await codes.AnyAsync(c => c.CodeHash == hash && c.UsedAt == null && c.ExpiresAt > now, ct) && ++attempts < 20);

        var pc = PairingCode.Create(uid, hash, now);
        codes.Add(pc);
        await uow.SaveChangesAsync(ct);
        return new PairingCodeDto(code, pc.ExpiresAt);
    }
}

// ------------------------------------------------------------------ POST /extension/pair (anônimo)

public sealed record PairDeviceCommand(string Code, string DeviceName) : IRequest<PairResult>;

public sealed class PairDeviceValidator : AbstractValidator<PairDeviceCommand>
{
    public PairDeviceValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Informe o código.").Matches(@"^\d{6}$").WithMessage("O código tem 6 dígitos.");
        RuleFor(x => x.DeviceName).NotEmpty().WithMessage("Dê um nome para este navegador (ex.: Chrome do notebook).").MaximumLength(80);
    }
}

public sealed class PairDeviceHandler(
    IRepository<PairingCode> codes,
    IRepository<ExtensionDevice> devices,
    IRepository<User> users,
    IRepository<AuditEvent> audit,
    ITokenService tokens,
    IAuthTokenIssuer issuer,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<PairDeviceCommand, PairResult>
{
    public async Task<PairResult> Handle(PairDeviceCommand cmd, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var hash = tokens.Hash(cmd.Code.Trim());
        var code = await codes.FindAsync(c => c.CodeHash == hash && c.UsedAt == null && c.ExpiresAt > now, ct)
                   ?? throw new UnauthorizedException("Código de pareamento inválido ou expirado. Gere um novo na plataforma.");
        var user = await users.FindAsync(u => u.Id == code.UserId, ct) ?? throw new UnauthorizedException("Código de pareamento inválido.");

        code.Use(now);
        var device = ExtensionDevice.Pair(user.Id, cmd.DeviceName, now);
        devices.Add(device);
        audit.Add(AuditEvent.Record(user.Id, AuditActions.DevicePaired, now));
        var result = issuer.Issue(user, device.Id);
        await uow.SaveChangesAsync(ct);
        return new PairResult(DeviceDto.From(device), result);
    }
}

// ------------------------------------------------------------------ GET/DELETE /extension/devices

public sealed record ListDevicesQuery : IRequest<IReadOnlyList<DeviceDto>>;

public sealed class ListDevicesHandler(IRepository<ExtensionDevice> devices, ICurrentUser current) : IRequestHandler<ListDevicesQuery, IReadOnlyList<DeviceDto>>
{
    public async Task<IReadOnlyList<DeviceDto>> Handle(ListDevicesQuery q, CancellationToken ct)
    {
        var uid = current.UserId;
        return (await devices.ListAsync(d => d.UserId == uid, ct)).OrderByDescending(d => d.PairedAt).Select(DeviceDto.From).ToList();
    }
}

/// <summary>Revoga a extensão: o dispositivo fica inativo e todos os refresh tokens dele são revogados.</summary>
public sealed record RevokeDeviceCommand(Guid Id) : IRequest<Unit>;

public sealed class RevokeDeviceHandler(
    IRepository<ExtensionDevice> devices,
    IRepository<RefreshToken> tokens,
    IRepository<AuditEvent> audit,
    ICurrentUser current,
    IUnitOfWork uow,
    IClock clock) : IRequestHandler<RevokeDeviceCommand, Unit>
{
    public async Task<Unit> Handle(RevokeDeviceCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var now = clock.UtcNow;
        var d = await devices.FindAsync(x => x.Id == cmd.Id && x.UserId == uid, ct) ?? throw new NotFoundException("Extensão não encontrada.");
        d.Revoke(now);
        foreach (var t in await tokens.ListAsync(t => t.DeviceId == d.Id && t.RevokedAt == null, ct)) t.Revoke(now);
        audit.Add(AuditEvent.Record(uid, AuditActions.DeviceRevoked, now));
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
