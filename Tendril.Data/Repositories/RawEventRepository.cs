using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class RawEventRepository : IRawEventRepository
{
    private readonly TendrilDbContext _db;

    public RawEventRepository(TendrilDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ScrapedEventRaw rawEvent, CancellationToken cancellationToken = default)
    {
        _db.RawEvents.Add(rawEvent);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
