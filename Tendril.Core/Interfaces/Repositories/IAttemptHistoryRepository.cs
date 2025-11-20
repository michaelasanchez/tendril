using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IAttemptHistoryRepository
{
    Task AddAsync(ScraperAttemptHistory attempt, CancellationToken cancellationToken = default);
}