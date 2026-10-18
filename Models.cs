using System.Text.Json.Serialization;

namespace ClothesApi;

public class ClothingType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    public List<Clothing> Clothes { get; set; } = new();
}

public class Clothing
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public int PurchaseCount { get; set; }
    public int ClothingTypeId { get; set; }
    public bool IsAvailable { get; set; }

    public ClothingType? ClothingType { get; set; }
    public List<ClothingSize> Sizes { get; set; } = new();
}

public class ClothingSize
{
    public int Id { get; set; }
    public int ClothingId { get; set; }
    public string LetterSize { get; set; } = "";
    public int NumericSize { get; set; }
    public int Quantity { get; set; }

    public Clothing? Clothing { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = "";

    [JsonIgnore]
    public string PasswordHash { get; set; } = "";

    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Role { get; set; } = "user";

    public List<Order> Orders { get; set; } = new();
}

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "new";
    public int UserId { get; set; }

    public User? User { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ClothingSizeId { get; set; }
    public int Quantity { get; set; }

    public Order? Order { get; set; }
    public ClothingSize? ClothingSize { get; set; }
}