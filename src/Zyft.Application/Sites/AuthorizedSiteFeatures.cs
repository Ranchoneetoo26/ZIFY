using FluentValidation;
using MediatR;
using Zyft.Application.Common.Exceptions;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Extension;

namespace Zyft.Application.Sites;

public sealed record AuthorizedSiteDto(Guid Id, string Origin, DateTime CreatedAt)
{
    public static AuthorizedSiteDto From(AuthorizedSite s) => new(s.Id, s.Origin, s.CreatedAt);
}

public sealed record ListAuthorizedSitesQuery : IRequest<IReadOnlyList<AuthorizedSiteDto>>;

/// <summary>Autoriza a extensão a atuar numa origem (RF-10). Aceita URL completa; guarda só esquema://host[:porta].</summary>
public sealed record AddAuthorizedSiteCommand(string Origin) : IRequest<AuthorizedSiteDto>;

public sealed record RemoveAuthorizedSiteCommand(Guid Id) : IRequest<Unit>;

public sealed class AddAuthorizedSiteValidator : AbstractValidator<AddAuthorizedSiteCommand>
{
    public AddAuthorizedSiteValidator() =>
        RuleFor(x => x.Origin).NotEmpty().WithMessage("Informe o endereço do site.").MaximumLength(300)
            .Must(o => Uri.TryCreate(o, UriKind.Absolute, out var u) && (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp))
            .WithMessage("Informe um endereço http(s) válido, por exemplo https://vagas.exemplo.com.");
}

public sealed class AuthorizedSiteHandlers(IRepository<AuthorizedSite> sites, ICurrentUser current, IUnitOfWork uow) :
    IRequestHandler<ListAuthorizedSitesQuery, IReadOnlyList<AuthorizedSiteDto>>,
    IRequestHandler<AddAuthorizedSiteCommand, AuthorizedSiteDto>,
    IRequestHandler<RemoveAuthorizedSiteCommand, Unit>
{
    public async Task<IReadOnlyList<AuthorizedSiteDto>> Handle(ListAuthorizedSitesQuery q, CancellationToken ct)
    {
        var uid = current.UserId;
        return (await sites.ListAsync(s => s.UserId == uid, ct)).OrderBy(s => s.Origin).Select(AuthorizedSiteDto.From).ToList();
    }

    public async Task<AuthorizedSiteDto> Handle(AddAuthorizedSiteCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var site = AuthorizedSite.Create(uid, cmd.Origin);
        if (await sites.AnyAsync(s => s.UserId == uid && s.Origin == site.Origin, ct))
            throw new ConflictException("Este site já está autorizado.");
        sites.Add(site);
        await uow.SaveChangesAsync(ct);
        return AuthorizedSiteDto.From(site);
    }

    public async Task<Unit> Handle(RemoveAuthorizedSiteCommand cmd, CancellationToken ct)
    {
        var uid = current.UserId;
        var s = await sites.FindAsync(x => x.Id == cmd.Id && x.UserId == uid, ct) ?? throw new NotFoundException("Site não encontrado.");
        sites.Remove(s);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
