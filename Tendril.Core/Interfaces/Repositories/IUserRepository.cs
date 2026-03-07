using Tendril.Core.Domain.Dtos;
using Tendril.Core.Domain.Entities;

namespace Tendril.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByGoogleSubAsync(string googleSub);
    Task UpdateRefreshTokenAsync(Guid id, string refreshToken);
    Task<User> UpsertUserAsync(UserDto userDto);
}
