using System.ComponentModel.DataAnnotations;

namespace DTO;

public record AuthResponseDTO(
    long UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string Role
);

public record AuthResultDTO(
    AuthResponseDTO UserInfo,
    string AccessToken,
    string RefreshToken
);
