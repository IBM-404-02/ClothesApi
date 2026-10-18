using Microsoft.EntityFrameworkCore;

namespace ClothesApi;

public class ShopDbContext : DbContext
{
    public DbSet<ClothingType> ClothingTypes => Set<ClothingType>();
    public DbSet<Clothing> Clothes => Set<Clothing>();
    public DbSet<ClothingSize> ClothingSizes => Set<ClothingSize>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer(
            "Server=LAPTOP-Q68JF043;Database=ClothesShop;Trusted_Connection=True;TrustServerCertificate=True;");
    }
}