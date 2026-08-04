using FoodExpress.Order.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodExpress.Order.API.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Entities.Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Delivery> Deliveries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== Order =====
        modelBuilder.Entity<Models.Entities.Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(20);
            entity.Property(o => o.CustomerName).IsRequired().HasMaxLength(150);
            entity.Property(o => o.CustomerPhone).HasMaxLength(20);
            entity.Property(o => o.RestaurantName).IsRequired().HasMaxLength(150);
            entity.Property(o => o.DeliveryAddress).IsRequired().HasMaxLength(500);
            entity.Property(o => o.Status).HasConversion<string>();
            entity.Property(o => o.Subtotal).HasColumnType("decimal(10,2)");
            entity.Property(o => o.DeliveryFee).HasColumnType("decimal(10,2)");
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(10,2)");
            entity.Property(o => o.Notes).HasMaxLength(500);
            entity.Property(o => o.CancellationReason).HasMaxLength(500);

            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasIndex(o => o.CustomerId);
            entity.HasIndex(o => o.RestaurantId);
            entity.HasIndex(o => o.Status);

            entity.HasMany(o => o.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.Delivery)
                  .WithOne(d => d.Order)
                  .HasForeignKey<Delivery>(d => d.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== OrderItem =====
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.DishName).IsRequired().HasMaxLength(150);
            entity.Property(i => i.DishImageUrl).HasMaxLength(500);
            entity.Property(i => i.UnitPrice).HasColumnType("decimal(10,2)");
            entity.Property(i => i.SpecialInstructions).HasMaxLength(300);

            entity.Ignore(i => i.Subtotal); // Calculé côté C#
        });

        // ===== Delivery =====
        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DeliveryPersonName).HasMaxLength(150);
            entity.Property(d => d.DeliveryPersonPhone).HasMaxLength(20);
            entity.Property(d => d.Status).HasConversion<string>();
        });
    }
}
