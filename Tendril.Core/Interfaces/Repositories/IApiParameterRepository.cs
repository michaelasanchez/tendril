using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IApiParameterRepository
{
    Task<List<ApiParameter>> GetByScraperIdAsync(Guid scraperId, CancellationToken cancellationToken = default);
    Task<ApiParameter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ApiParameter parameter, CancellationToken cancellationToken = default);
    Task UpdateAsync(ApiParameter parameter, CancellationToken cancellationToken = default);
    Task DeleteAsync(ApiParameter parameter, CancellationToken cancellationToken = default);
}
