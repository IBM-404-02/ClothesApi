using System.Text.Json.Serialization;

namespace ClothesApi;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Title { get; set; } = null!;

    public List<User> Users { get; set; } = new();
}

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = null!;

    [JsonIgnore]
    public string PasswordHash { get; set; } = null!;

    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int RoleId { get; set; }

    public Role? Role { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();
}

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Country { get; set; }

    public List<Clothing> Clothes { get; set; } = new();
}

public class ClothingType
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public List<Clothing> Clothes { get; set; } = new();
}

public class Color
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? HexCode { get; set; }

    public List<ClothingColor> ClothingColors { get; set; } = new();
}

public class Size
{
    public int Id { get; set; }
    public string LetterSize { get; set; } = null!;
    public int NumericSize { get; set; }

    public List<ClothingSize> ClothingSizes { get; set; } = new();
}

public class OrderStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public List<Order> Orders { get; set; } = new();
}

public class Clothing
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public int PurchaseCount { get; set; }
    public int ClothingTypeId { get; set; }
    public int BrandId { get; set; }
    public bool IsAvailable { get; set; }

    public ClothingType? ClothingType { get; set; }
    public Brand? Brand { get; set; }
    public List<ClothingSize> ClothingSizes { get; set; } = new();
    public List<ClothingColor> ClothingColors { get; set; } = new();
    public List<Favorite> Favorites { get; set; } = new();
}

public class ClothingColor
{
    public int Id { get; set; }
    public int ClothingId { get; set; }
    public int ColorId { get; set; }

    public Clothing? Clothing { get; set; }
    public Color? Color { get; set; }
}

public class ClothingSize
{
    public int Id { get; set; }
    public int ClothingId { get; set; }
    public int SizeId { get; set; }
    public int Quantity { get; set; }

    public Clothing? Clothing { get; set; }
    public Size? Size { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();
}

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderStatusId { get; set; }
    public int UserId { get; set; }
    public decimal TotalPrice { get; set; }

    public OrderStatus? OrderStatus { get; set; }
    public User? User { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ClothingSizeId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }

    public Order? Order { get; set; }
    public ClothingSize? ClothingSize { get; set; }
}

public class Favorite
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ClothingId { get; set; }
    public DateTime AddedAt { get; set; }

    public User? User { get; set; }
    public Clothing? Clothing { get; set; }
}