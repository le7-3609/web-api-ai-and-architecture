using DTO;
using Entities;

namespace Services
{
    public interface IPasswordValidityService
    {
        PasswordDTO? PasswordStrength(string password);
    }
}