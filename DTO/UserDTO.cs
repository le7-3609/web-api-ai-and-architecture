using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public record RegisterDTO(
        [Required, EmailAddress]
        string Email,
        string FirstName,
        string LastName,
        [Phone]
        string Phone,
        [Required]
        string Password,
        string Provider
    );
    public record UpdateUserDTO(
        [EmailAddress]
        string Email,
        string FirstName,
        string LastName,
        [Phone]
        string Phone,
        string Password
    );
    public record UpdateExternalUserDTO(
        string FirstName,
        string LastName,
        [Phone]
        string Phone
    );
    public record UserProfileDTO(
        long UserId,
        [Required,EmailAddress]
        string Email,
        string FirstName,
        string LastName,
        [Phone]
        string Phone
    );

    public record LoginDTO(
        [Required, EmailAddress]
        string Email,
        [Required]
        string Password
    );

    public record SocialLoginDTO(
        [Required]
        string Token,
        [Required]
        string Provider // "Google" or "Microsoft"
    );

    public record UserDTO(
       long UserId,
       [Required,EmailAddress]
       string Email,
       string FirstName,
       string LastName,
       [Phone]
       string Phone,
       [Required]
       string Provider,
       DateTime? CreatedAt,
       DateTime? LastLogin
   );

    public record ExternalUserInfo
    {
        public string ProviderId { get; set; }
        [Required]
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [Required]
        public string Provider { get; set; }// "Google" or "Microsoft"
    }
   
}
