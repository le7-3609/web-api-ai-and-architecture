namespace Entities;

public partial class User
{
    public string Role { get; set; } = "User";

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }
}
