using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IEventRevisionRepository
{
    Task AddAsync(EventRevision ev, CancellationToken cancellationToken = default);
}
