using FoodExpress.User.API.DTOs;
using FoodExpress.User.API.Models.Entities;

namespace FoodExpress.User.API.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserDto dto);
    Task<TokenResponseDto> LoginAsync(LoginDto dto);
    Task<TokenResponseDto> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<List<UserDto>> GetAllAsync();
    Task<List<UserDto>> GetDeliveryPersonsAsync();
    Task<UserDto> UpdateRoleAsync(Guid userId, UserRole role);
    Task<AddressDto> AddAddressAsync(Guid userId, CreateAddressDto dto);
    Task<List<AddressDto>> GetAddressesAsync(Guid userId);
}