using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class TagRepository(TendrilDbContext db) : ITagRepository
{
    public async Task<List<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await db.Tags
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Tags
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        db.Tags.Add(tag);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        db.Tags.Update(tag);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        db.Tags.Remove(tag);
        await db.SaveChangesAsync(cancellationToken);
    }
}
