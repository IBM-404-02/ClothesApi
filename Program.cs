using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using ClothesApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<ShopDbContext>();

var app = builder.Build();

var tokens = new Dictionary<string, int>();


app.MapPost("/api/auth/register", async (ShopDbContext db, RegisterRequest data) =>
{
    if (string.IsNullOrWhiteSpace(data.Login) || string.IsNullOrWhiteSpace(data.Password))
        return Results.BadRequest(new { message = "Логин и пароль обязательны" });

    if (await db.Users.AnyAsync(u => u.Login == data.Login))
        return Results.BadRequest(new { message = "Такой логин уже занят" });

    User user = new User
    {
        Login = data.Login,
        PasswordHash = Hash(data.Password),
        Phone = data.Phone,
        Address = data.Address,
        Role = "user"
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", user);
});


app.MapPost("/api/auth/login", async (ShopDbContext db, LoginRequest data) =>
{
    string hash = Hash(data.Password ?? "");

    User? user = await db.Users
        .FirstOrDefaultAsync(u => u.Login == data.Login && u.PasswordHash == hash);

    if (user == null)
        return Results.Json(new { message = "Неверный логин или пароль" }, statusCode: 401);

    string token = Guid.NewGuid().ToString();
    tokens[token] = user.Id;

    return Results.Ok(new { token, role = user.Role, userId = user.Id });
});


app.MapGet("/api/clothes", async (ShopDbContext db) =>
{
    var clothes = await db.Clothes
        .Include(c => c.ClothingType)
        .Include(c => c.Sizes)
        .ToListAsync();

    return Results.Json(clothes);
});


app.MapGet("/api/clothes/search", async (ShopDbContext db, string name) =>
{
    if (string.IsNullOrWhiteSpace(name))
        return Results.BadRequest(new { message = "Укажите параметр name" });

    var found = await db.Clothes
        .Include(c => c.Sizes)
        .Where(c => c.Name.Contains(name))
        .ToListAsync();

    return Results.Json(found);
});


app.MapPost("/api/orders", async (HttpRequest request, ShopDbContext db, CreateOrderRequest data) =>
{
    User? current = CurrentUser(request, db);
    if (current == null)
        return Results.Json(new { message = "Требуется авторизация" }, statusCode: 401);

    if (data.Items == null || data.Items.Count == 0)
        return Results.BadRequest(new { message = "Заказ пустой" });

    Order order = new Order
    {
        UserId = current.Id,
        CreatedAt = DateTime.Now,
        Status = "new"
    };

    foreach (var item in data.Items)
    {
        ClothingSize? size = await db.ClothingSizes
            .FirstOrDefaultAsync(s => s.Id == item.ClothingSizeId);

        if (size == null)
            return Results.BadRequest(new { message = $"Размер с Id {item.ClothingSizeId} не найден" });

        if (size.Quantity < item.Quantity)
            return Results.BadRequest(new { message = $"Недостаточно товара размера {size.LetterSize}" });

        size.Quantity -= item.Quantity;

        Clothing? clothing = await db.Clothes.FirstOrDefaultAsync(c => c.Id == size.ClothingId);
        if (clothing != null)
            clothing.PurchaseCount += item.Quantity;

        order.Items.Add(new OrderItem
        {
            ClothingSizeId = size.Id,
            Quantity = item.Quantity
        });
    }

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Created($"/api/orders/{order.Id}", order);
});


app.MapGet("/api/orders", async (HttpRequest request, ShopDbContext db, string status) =>
{
    User? current = CurrentUser(request, db);
    if (current == null)
        return Results.Json(new { message = "Требуется авторизация" }, statusCode: 401);

    var orders = await db.Orders
        .Include(o => o.Items)
        .Where(o => o.Status == status)
        .ToListAsync();

    return Results.Json(orders);
});


app.MapGet("/api/orders/user/{userId}", async (HttpRequest request, ShopDbContext db, int userId) =>
{
    User? current = CurrentUser(request, db);
    if (current == null)
        return Results.Json(new { message = "Требуется авторизация" }, statusCode: 401);

    var orders = await db.Orders
        .Include(o => o.Items)
        .Where(o => o.UserId == userId)
        .ToListAsync();

    return Results.Json(orders);
});


app.MapPatch("/api/orders/{id}/status", async (HttpRequest request, ShopDbContext db, int id, StatusRequest data) =>
{
    User? current = CurrentUser(request, db);
    if (current == null)
        return Results.Json(new { message = "Требуется авторизация" }, statusCode: 401);
    if (current.Role != "manager")
        return Results.Json(new { message = "Доступно только менеджеру" }, statusCode: 403);

    Order? order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id);
    if (order == null)
        return Results.NotFound(new { message = "Заказ не найден" });

    order.Status = data.Status;
    await db.SaveChangesAsync();

    return Results.Json(order);
});


app.MapPatch("/api/sizes/{id}/quantity", async (HttpRequest request, ShopDbContext db, int id, QuantityRequest data) =>
{
    User? current = CurrentUser(request, db);
    if (current == null)
        return Results.Json(new { message = "Требуется авторизация" }, statusCode: 401);
    if (current.Role != "manager")
        return Results.Json(new { message = "Доступно только менеджеру" }, statusCode: 403);

    ClothingSize? size = await db.ClothingSizes.FirstOrDefaultAsync(s => s.Id == id);
    if (size == null)
        return Results.NotFound(new { message = "Размер не найден" });

    if (data.Quantity < 0)
        return Results.BadRequest(new { message = "Количество не может быть отрицательным" });

    size.Quantity = data.Quantity;
    await db.SaveChangesAsync();

    return Results.Json(size);
});


app.MapPatch("/api/clothes/{id}/unpublish", async (HttpRequest request, ShopDbContext db, int id) =>
{
    return await SetAvailability(request, db, id, false);
});

app.MapPatch("/api/clothes/{id}/publish", async (HttpRequest request, ShopDbContext db, int id) =>
{
    return await SetAvailability(request, db, id, true);
});


app.Run();


string Hash(string password)
{
    byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
    return Convert.ToHexString(bytes);
}

User? CurrentUser(HttpRequest request, ShopDbContext db)
{
    string? header = request.Headers.Authorization;
    if (string.IsNullOrWhiteSpace(header)) return null;

    string token = header.Replace("Bearer ", "").Trim();

    if (!tokens.TryGetValue(token, out int userId)) return null;

    return db.Users.FirstOrDefault(u => u.Id == userId);
}

async Task<IResult> SetAvailability(HttpRequest request, ShopDbContext db, int id, bool available)
{
    User? current = CurrentUser(request, db);
    if (current == null)
        return Results.Json(new { message = "Требуется авторизация" }, statusCode: 401);
    if (current.Role != "manager")
        return Results.Json(new { message = "Доступно только менеджеру" }, statusCode: 403);

    Clothing? clothing = await db.Clothes.FirstOrDefaultAsync(c => c.Id == id);
    if (clothing == null)
        return Results.NotFound(new { message = "Товар не найден" });

    clothing.IsAvailable = available;
    await db.SaveChangesAsync();

    return Results.Json(clothing);
}



public record RegisterRequest(string Login, string Password, string? Phone, string? Address);
public record LoginRequest(string Login, string Password);
public record CreateOrderRequest(List<OrderItemRequest> Items);
public record OrderItemRequest(int ClothingSizeId, int Quantity);
public record StatusRequest(string Status);
public record QuantityRequest(int Quantity);