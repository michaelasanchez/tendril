using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IRawEventRepository
{
    Task AddAsync(ScrapedEventRaw rawEvent, CancellationToken cancellationToken = default);
}