using FoodExpress.User.API.DTOs;

namespace FoodExpress.User.API.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterUserDto dto);
    Task<TokenResponseDto> LoginAsync(LoginDto dto);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto?> GetByEmailAsync(string email);
    Task<List<UserDto>> GetAllAsync();
    Task<AddressDto> AddAddressAsync(Guid userId, CreateAddressDto dto);
    Task<List<AddressDto>> GetAddressesAsync(Guid userId);
}