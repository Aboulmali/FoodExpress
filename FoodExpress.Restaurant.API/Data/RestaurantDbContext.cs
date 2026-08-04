using FoodExpress.Restaurant.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodExpress.Restaurant.API.Data;

public class RestaurantDbContext : DbContext
{
    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Entities.Restaurant> Restaurants { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Dish> Dishes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== Restaurant =====
        modelBuilder.Entity<Models.Entities.Restaurant>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(150);
            entity.Property(r => r.Description).HasMaxLength(1000);
            entity.Property(r => r.Address).IsRequired().HasMaxLength(300);
            entity.Property(r => r.City).IsRequired().HasMaxLength(100);
            entity.Property(r => r.PhoneNumber).HasMaxLength(20);
            entity.Property(r => r.Email).HasMaxLength(150);
            entity.Property(r => r.LogoUrl).HasMaxLength(500);
            entity.Property(r => r.CoverImageUrl).HasMaxLength(500);

            entity.HasMany(r => r.Dishes)
                  .WithOne(d => d.Restaurant)
                  .HasForeignKey(d => d.RestaurantId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Category =====
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(500);
            entity.Property(c => c.IconUrl).HasMaxLength(500);

            entity.HasIndex(c => c.Name).IsUnique();

            entity.HasMany(c => c.Dishes)
                  .WithOne(d => d.Category)
                  .HasForeignKey(d => d.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Dish =====
        modelBuilder.Entity<Dish>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(150);
            entity.Property(d => d.Description).HasMaxLength(1000);
            entity.Property(d => d.Price).HasColumnType("decimal(10,2)");
            entity.Property(d => d.ImageUrl).HasMaxLength(500);
        });

        // ===== Seed Data (catégories par défaut) =====
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Pizza", Description = "Pizzas italiennes", DisplayOrder = 1, CreatedAt = seedDate },
            new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Burger", Description = "Burgers gourmands", DisplayOrder = 2, CreatedAt = seedDate },
            new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Sushi", Description = "Sushis japonais", DisplayOrder = 3, CreatedAt = seedDate },
            new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Tacos", Description = "Tacos mexicains", DisplayOrder = 4, CreatedAt = seedDate },
            new Category { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Salades", Description = "Salades fraîches", DisplayOrder = 5, CreatedAt = seedDate },
            new Category { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Desserts", Description = "Desserts sucrés", DisplayOrder = 6, CreatedAt = seedDate }
        );
    }
}