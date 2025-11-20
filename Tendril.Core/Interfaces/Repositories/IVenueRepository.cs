using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IVenueRepository
{
    Task<List<Venue>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Venue venue, CancellationToken cancellationToken = default);
    Task UpdateAsync(Venue venue, CancellationToken cancellationToken = default);
    Task DeleteAsync(Venue venue, CancellationToken cancellationToken = default);
}