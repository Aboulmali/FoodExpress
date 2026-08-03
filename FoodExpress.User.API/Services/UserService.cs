using FoodExpress.User.API.Data;
using FoodExpress.User.API.DTOs;
using FoodExpress.User.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodExpress.User.API.Services;

public class UserService : IUserService
{
    private readonly UserDbContext _db;
    private readonly IKeycloakService _keycloak;
    private readonly ILogger<UserService> _logger;

    public UserService(UserDbContext db, IKeycloakService keycloak, ILogger<UserService> logger)
    {
        _db = db;
        _keycloak = keycloak;
        _logger = logger;
    }

    public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Un utilisateur avec cet email existe déjà.");

        // 1. Créer dans Keycloak
        var keycloakId = await _keycloak.CreateUserAsync(dto);

        // 2. Créer dans la BDD locale
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            KeycloakId = keycloakId,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            Role = dto.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User created: {Email}", dto.Email);

        return MapToDto(user);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        return await _keycloak.LoginAsync(dto.Email, dto.Password);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _db.Users.ToListAsync();
        return users.Select(MapToDto).ToList();
    }

    public async Task<AddressDto> AddAddressAsync(Guid userId, CreateAddressDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Utilisateur introuvable");

        var address = new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Label = dto.Label,
            Street = dto.Street,
            City = dto.City,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            IsDefault = dto.IsDefault
        };

        _db.Addresses.Add(address);
        await _db.SaveChangesAsync();

        return MapAddressToDto(address);
    }

    public async Task<List<AddressDto>> GetAddressesAsync(Guid userId)
    {
        var addresses = await _db.Addresses
            .Where(a => a.UserId == userId)
            .ToListAsync();
        return addresses.Select(MapAddressToDto).ToList();
    }

    private static UserDto MapToDto(AppUser u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FirstName = u.FirstName,
        LastName = u.LastName,
        PhoneNumber = u.PhoneNumber,
        Role = u.Role,
        CreatedAt = u.CreatedAt
    };

    private static AddressDto MapAddressToDto(Address a) => new()
    {
        Id = a.Id,
        Label = a.Label,
        Street = a.Street,
        City = a.City,
        PostalCode = a.PostalCode,
        Country = a.Country,
        Latitude = a.Latitude,
        Longitude = a.Longitude,
        IsDefault = a.IsDefault
    };
}