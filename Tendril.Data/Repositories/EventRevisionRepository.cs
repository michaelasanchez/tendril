using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class EventRevisionRepository(TendrilDbContext _context) : IEventRevisionRepository
{
    public async Task AddAsync(EventRevision ev, CancellationToken cancellationToken = default)
    {
        await _context.EventRevisions.AddAsync(ev, cancellationToken);

        await _context.SaveChangesAsync();
    }
}
