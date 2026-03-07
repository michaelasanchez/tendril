using Microsoft.EntityFrameworkCore;
using Tendril.Core.Domain.Dtos;
using Tendril.Core.Domain.Entities;
using Tendril.Core.Interfaces.Repositories;

namespace Tendril.Data.Repositories;

public class UserRepository(TendrilDbContext _context) : IUserRepository
{
    public Task<User?> GetUserByGoogleSubAsync(string googleSub)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.GoogleSub == googleSub);
    }

    public Task UpdateRefreshTokenAsync(Guid id, string refreshToken)
    {
        return _context.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(u => u.SetProperty(x => x.RefreshToken, refreshToken));
    }

    public async Task<User> UpsertUserAsync(UserDto dto)
    {
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.GoogleSub == dto.GoogleSub);

        if (existing == null)
        {
            existing = new User
            {
                Id = Guid.NewGuid(),
                GoogleSub = dto.GoogleSub,
                Email = dto.Email,
                Name = dto.Name,
                PictureUrl = dto.PictureUrl,
                RefreshToken = dto.RefreshToken,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(existing);
        }
        else
        {
            existing.Name = dto.Name;
            existing.PictureUrl = dto.PictureUrl;

            if (!string.IsNullOrEmpty(dto.RefreshToken))
            {
                existing.RefreshToken = dto.RefreshToken;
            }
        }

        await _context.SaveChangesAsync();

        return existing;
    }
}
