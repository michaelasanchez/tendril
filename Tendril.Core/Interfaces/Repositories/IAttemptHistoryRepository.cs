using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IAttemptHistoryRepository
{
    Task Add(ScraperAttemptHistory attempt, CancellationToken ct = default);

    Task<List<ScraperAttemptHistory>> GetAttemptHistories(Guid scraperId, CancellationToken ct = default);
}