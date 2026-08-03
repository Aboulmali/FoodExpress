using FoodExpress.User.API.DTOs;

namespace FoodExpress.User.API.Services;

public interface IKeycloakService
{
    Task<string> CreateUserAsync(RegisterUserDto dto);
    Task<TokenResponseDto> LoginAsync(string email, string password);
    Task AssignRoleAsync(string userId, string roleName);
}