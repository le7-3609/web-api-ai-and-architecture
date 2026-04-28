// Partial class extending the auto-generated User entity.
// DO NOT add EF configuration here – see Repositories/MyShopContextExtensions.cs.
// Schema change: run Database/Migrations/AddUserRoleAndRefreshToken.sql first,
// then re-scaffold with EF Core Power Tools to merge this into the generated file.
namespace Entities;

public partial class User
{
    /// <summary>Roles: "User" | "Admin"</summary>
    public string Role { get; set; } = "User";

    /// <summary>SHA-256 hash of the current refresh token (never the raw token).</summary>
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }
}
