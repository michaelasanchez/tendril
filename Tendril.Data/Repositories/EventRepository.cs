using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class EventRepository : IEventRepository
{
    private readonly TendrilDbContext _db;

    public EventRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task<List<Event>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Events
            .Include(e => e.Venue)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Event ev, CancellationToken cancellationToken = default)
    {
        _db.Events.Add(ev);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Event ev, CancellationToken cancellationToken = default)
    {
        _db.Events.Update(ev);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Event ev, CancellationToken cancellationToken = default)
    {
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
