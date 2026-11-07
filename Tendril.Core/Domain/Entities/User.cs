namespace Tendril.Core.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string GoogleSub { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;

    public string? PictureUrl { get; set; }
    public string? RefreshToken { get; set; }

    public bool IsAdmin { get; set; } = false;

    public DateTime CreatedAt { get; set; }
}