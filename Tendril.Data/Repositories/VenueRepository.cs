using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class VenueRepository : IVenueRepository
{
    private readonly TendrilDbContext _db;

    public VenueRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task<List<Venue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Venues
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Venues
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddAsync(Venue venue, CancellationToken cancellationToken = default)
    {
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Venue venue, CancellationToken cancellationToken = default)
    {
        _db.Venues.Update(venue);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Venue venue, CancellationToken cancellationToken = default)
    {
        _db.Venues.Remove(venue);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
