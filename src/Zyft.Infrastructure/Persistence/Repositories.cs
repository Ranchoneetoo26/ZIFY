using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Zyft.Application.Common.Interfaces;
using Zyft.Domain.Common;
using Zyft.Domain.Profiles;

namespace Zyft.Infrastructure.Persistence;

internal sealed class EfRepository<T>(ZyftDbContext db) : IRepository<T> where T : Entity
{
    public Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => db.Set<T>().FirstOrDefaultAsync(predicate, ct);

    public Task<List<T>> ListAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => db.Set<T>().Where(predicate).ToListAsync(ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => db.Set<T>().AnyAsync(predicate, ct);

    public void Add(T entity) => db.Set<T>().Add(entity);

    public void AddRange(IEnumerable<T> entities) => db.Set<T>().AddRange(entities);

    public void Remove(T entity) => db.Set<T>().Remove(entity);

    public void RemoveRange(IEnumerable<T> entities) => db.Set<T>().RemoveRange(entities);
}

internal sealed class ProfileRepository(ZyftDbContext db) : IProfileRepository
{
    public Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Profiles
            .Include(p => p.Experiences)
            .Include(p => p.Educations)
            .Include(p => p.Courses)
            .Include(p => p.Skills)
            .Include(p => p.Languages)
            .Include(p => p.CustomFields)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

    public void Add(Profile profile) => db.Profiles.Add(profile);
}
