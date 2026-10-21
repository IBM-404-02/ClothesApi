using Microsoft.EntityFrameworkCore;

namespace ClothesApi;

public class ShopDbContext : DbContext
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ClothingType> ClothingTypes => Set<ClothingType>();
    public DbSet<Color> Colors => Set<Color>();
    public DbSet<Size> Sizes => Set<Size>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<Clothing> Clothes => Set<Clothing>();
    public DbSet<ClothingColor> ClothingColors => Set<ClothingColor>();
    public DbSet<ClothingSize> ClothingSizes => Set<ClothingSize>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer(
            "Server=LAPTOP-Q68JF043;Database=ClothesShop;Trusted_Connection=True;TrustServerCertificate=True;");
    }
}