using FoodExpress.User.API.Data;
using FoodExpress.User.API.DTOs;
using FoodExpress.User.API.Models.Entities;
using FoodExpress.User.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FoodExpress.Tests.User;

public class UserServiceTests
{
    private readonly Mock<IKeycloakService> _keycloak;
    private readonly Mock<ILogger<UserService>> _logger;

    public UserServiceTests()
    {
        _keycloak = new Mock<IKeycloakService>();
        _logger = new Mock<ILogger<UserService>>();
    }

    private static UserDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new UserDbContext(options);
    }

    private static RegisterUserDto SampleDto() => new()
    {
        Email = "user@test.com",
        Password = "Secret123!",
        FirstName = "Test",
        LastName = "User",
        PhoneNumber = "0600000000",
        Role = UserRole.Customer
    };

    [Fact]
    public async Task Register_WithUniqueEmail_CreatesUserInDb()
    {
        var db = CreateDb("RegisterUnique");
        _keycloak.Setup(k => k.CreateUserAsync(It.IsAny<RegisterUserDto>()))
                 .ReturnsAsync("k123");

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        var result = await service.RegisterAsync(SampleDto());

        Assert.NotNull(result);
        Assert.Equal("user@test.com", result.Email);
        Assert.Equal(UserRole.Customer, result.Role);
        _keycloak.Verify(k => k.CreateUserAsync(It.IsAny<RegisterUserDto>()), Times.Once);
        Assert.Equal(1, db.Users.Count());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        var db = CreateDb(nameof(Register_DuplicateEmail_Throws));
        db.Users.Add(new AppUser { Id = Guid.NewGuid(), Email = "user@test.com", FirstName = "x", LastName = "y" });
        await db.SaveChangesAsync();

        var service = new UserService(db, _keycloak.Object, _logger.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(SampleDto()));
        _keycloak.Verify(k => k.CreateUserAsync(It.IsAny<RegisterUserDto>()), Times.Never);
    }

    [Fact]
    public async Task Register_ForcesCustomerRole_IgnoringRequestedRole()
    {
        var db = CreateDb("RegisterOwnerRole");
        _keycloak.Setup(k => k.CreateUserAsync(It.IsAny<RegisterUserDto>())).ReturnsAsync("k456");

        var dto = SampleDto();
        dto.Role = UserRole.RestaurantOwner; // tentative d'élévation de privilège

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        var result = await service.RegisterAsync(dto);

        // La sécurité force le rôle Client : l'utilisateur ne peut pas s'auto-proclamer Owner/Admin
        Assert.Equal(UserRole.Customer, result.Role);
    }

    [Fact]
    public async Task Login_DelegatesToKeycloak()
    {
        var db = CreateDb(nameof(Login_DelegatesToKeycloak));
        var expected = new TokenResponseDto
        {
            AccessToken = "jwt-token",
            RefreshToken = "refresh",
            ExpiresIn = 300,
            TokenType = "Bearer"
        };
        _keycloak.Setup(k => k.LoginAsync("user@test.com", "pass")).ReturnsAsync(expected);

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        var result = await service.LoginAsync(new LoginDto { Email = "user@test.com", Password = "pass" });

        Assert.Equal("jwt-token", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        _keycloak.Verify(k => k.LoginAsync("user@test.com", "pass"), Times.Once);
    }

    [Fact]
    public async Task Refresh_DelegatesToKeycloak()
    {
        var db = CreateDb(nameof(Refresh_DelegatesToKeycloak));
        var expected = new TokenResponseDto
        {
            AccessToken = "new-jwt",
            RefreshToken = "new-refresh",
            ExpiresIn = 300,
            TokenType = "Bearer"
        };
        _keycloak.Setup(k => k.RefreshAsync("old-refresh")).ReturnsAsync(expected);

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        var result = await service.RefreshAsync("old-refresh");

        Assert.Equal("new-jwt", result.AccessToken);
        _keycloak.Verify(k => k.RefreshAsync("old-refresh"), Times.Once);
    }

    [Fact]
    public async Task Logout_DelegatesToKeycloak()
    {
        var db = CreateDb(nameof(Logout_DelegatesToKeycloak));
        _keycloak.Setup(k => k.LogoutAsync("refresh-token")).Returns(Task.CompletedTask);

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        await service.LogoutAsync("refresh-token");

        _keycloak.Verify(k => k.LogoutAsync("refresh-token"), Times.Once);
    }

    [Fact]
    public async Task GetById_UserNotFound_ReturnsNull()
    {
        var db = CreateDb(nameof(GetById_UserNotFound_ReturnsNull));
        var service = new UserService(db, _keycloak.Object, _logger.Object);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsMappedUser()
    {
        var db = CreateDb(nameof(GetById_ExistingUser_ReturnsMappedUser));
        var id = Guid.NewGuid();
        db.Users.Add(new AppUser
        {
            Id = id,
            Email = "a@b.com",
            FirstName = "Ali",
            LastName = "Ben",
            Role = UserRole.Customer
        });
        await db.SaveChangesAsync();

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        var result = await service.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("a@b.com", result.Email);
        Assert.Equal("Ali", result.FirstName);
    }

    [Fact]
    public async Task AddAddress_AddsAddressToUser()
    {
        var db = CreateDb(nameof(AddAddress_AddsAddressToUser));
        var id = Guid.NewGuid();
        db.Users.Add(new AppUser { Id = id, Email = "a@b.com", FirstName = "A", LastName = "B" });
        await db.SaveChangesAsync();

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        var result = await service.AddAddressAsync(id, new CreateAddressDto
        {
            Label = "Maison",
            Street = "12 rue X",
            City = "Casablanca",
            Country = "Maroc",
            IsDefault = true
        });

        Assert.Equal("Casablanca", result.City);
        Assert.Equal(1, db.Addresses.Count());
        Assert.True(result.IsDefault);
    }

    [Fact]
    public async Task AddAddress_UnknownUser_Throws()
    {
        var db = CreateDb(nameof(AddAddress_UnknownUser_Throws));
        var service = new UserService(db, _keycloak.Object, _logger.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.AddAddressAsync(Guid.NewGuid(), new CreateAddressDto { Street = "Casablanca" }));
    }

    [Fact]
    public async Task GetDeliveryPersons_ReturnsOnlyActiveDeliveryRoles()
    {
        var db = CreateDb(nameof(GetDeliveryPersons_ReturnsOnlyActiveDeliveryRoles));
        db.Users.AddRange(
            new AppUser { Id = Guid.NewGuid(), Email = "livreur1@test.com", FirstName = "L1", LastName = "A", Role = UserRole.DeliveryPerson },
            new AppUser { Id = Guid.NewGuid(), Email = "livreur2@test.com", FirstName = "L2", LastName = "B", Role = UserRole.DeliveryPerson, IsActive = false },
            new AppUser { Id = Guid.NewGuid(), Email = "client@test.com", FirstName = "C", LastName = "D", Role = UserRole.Customer });
        await db.SaveChangesAsync();

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        var result = await service.GetDeliveryPersonsAsync();

        var courier = Assert.Single(result);
        Assert.Equal("livreur1@test.com", courier.Email);
    }

    [Fact]
    public async Task UpdateRole_SameRole_DoesNotCallKeycloak()
    {
        var db = CreateDb(nameof(UpdateRole_SameRole_DoesNotCallKeycloak));
        var id = Guid.NewGuid();
        db.Users.Add(new AppUser { Id = id, KeycloakId = "k1", Email = "a@b.com", FirstName = "A", LastName = "B", Role = UserRole.Customer });
        await db.SaveChangesAsync();

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        var result = await service.UpdateRoleAsync(id, UserRole.Customer);

        Assert.Equal(UserRole.Customer, result.Role);
        _keycloak.Verify(k => k.AssignRoleAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _keycloak.Verify(k => k.RemoveRoleAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRole_ChangesRole_KeycloakAndDbAligned()
    {
        var db = CreateDb(nameof(UpdateRole_ChangesRole_KeycloakAndDbAligned));
        var id = Guid.NewGuid();
        db.Users.Add(new AppUser { Id = id, KeycloakId = "k1", Email = "a@b.com", FirstName = "A", LastName = "B", Role = UserRole.Customer });
        await db.SaveChangesAsync();

        var service = new UserService(db, _keycloak.Object, _logger.Object);
        var result = await service.UpdateRoleAsync(id, UserRole.DeliveryPerson);

        Assert.Equal(UserRole.DeliveryPerson, result.Role);
        Assert.Equal(UserRole.DeliveryPerson, db.Users.Single().Role);
        _keycloak.Verify(k => k.RemoveRoleAsync("k1", UserRole.Customer.ToString()), Times.Once);
        _keycloak.Verify(k => k.AssignRoleAsync("k1", UserRole.DeliveryPerson.ToString()), Times.Once);
    }

    [Fact]
    public async Task UpdateRole_UnknownUser_Throws()
    {
        var db = CreateDb(nameof(UpdateRole_UnknownUser_Throws));
        var service = new UserService(db, _keycloak.Object, _logger.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateRoleAsync(Guid.NewGuid(), UserRole.Admin));
        _keycloak.Verify(k => k.AssignRoleAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}