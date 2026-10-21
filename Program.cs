using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using ClothesApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// игнорируем циклы при сериализации связанных сущностей
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddDbContext<ShopDbContext>();

// хешер паролей
var hasher = new PasswordHasher<User>();

// секретный ключ сервера для подписи токенов
var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));

var validation = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = "ClothesShop",

    ValidateAudience = true,
    ValidAudience = "ClothesShopApi",

    ValidateIssuerSigningKey = true,
    IssuerSigningKey = key,

    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero,

    NameClaimType = "name",
    RoleClaimType = "role"
};

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = validation;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();


// ==================== АУТЕНТИФИКАЦИЯ ====================

app.MapPost("/api/auth/register", async (ShopDbContext context, RegisterRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "Логин и пароль обязательны" });

    if (await context.Users.AnyAsync(u => u.Login == request.Login))
        return Results.BadRequest(new { message = "Логин уже занят" });

    Role? role = await context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
    if (role == null)
        return Results.BadRequest(new { message = "Роль пользователя не найдена" });

    User user = new User
    {
        Login = request.Login,
        Phone = request.Phone,
        Address = request.Address,
        RoleId = role.Id
    };
    user.PasswordHash = hasher.HashPassword(user, request.Password);

    context.Users.Add(user);
    await context.SaveChangesAsync();

    return Results.Created($"/api/users/{user.Id}", user);
}).AllowAnonymous();


app.MapPost("/api/auth/login", async (ShopDbContext context, LoginRequest request) =>
{
    User? user = await context.Users
        .Include(u => u.Role)
        .FirstOrDefaultAsync(u => u.Login == request.Login);

    if (user == null || string.IsNullOrWhiteSpace(request.Password))
        return Results.Challenge();

    var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

    if (result == PasswordVerificationResult.Failed)
        return Results.Challenge();

    return Results.Ok(new
    {
        access_token = CreateToken(user),
        token_type = "Bearer"
    });
}).AllowAnonymous();


app.MapGet("/api/me", (ClaimsPrincipal principal) =>
{
    return Results.Ok(new
    {
        id = principal.FindFirst("sub")?.Value,
        login = principal.Identity?.Name,
        role = principal.FindFirst("role")?.Value
    });
}).RequireAuthorization();


// ==================== ОДЕЖДА ====================

// список всей одежды — доступен всем
app.MapGet("/api/clothes", async (ShopDbContext context) =>
{
    var clothes = await context.Clothes
        .Include(c => c.ClothingType)
        .Include(c => c.Brand)
        .Include(c => c.ClothingSizes).ThenInclude(cs => cs.Size)
        .Include(c => c.ClothingColors).ThenInclude(cc => cc.Color)
        .ToListAsync();

    return Results.Json(clothes);
});


// поиск по части названия — доступен всем
app.MapGet("/api/clothes/search", async (ShopDbContext context, string name) =>
{
    if (string.IsNullOrWhiteSpace(name))
        return Results.BadRequest(new { message = "Не указан параметр name" });

    var found = await context.Clothes
        .Include(c => c.ClothingType)
        .Include(c => c.Brand)
        .Include(c => c.ClothingSizes).ThenInclude(cs => cs.Size)
        .Where(c => c.Name.Contains(name))
        .ToListAsync();

    return Results.Json(found);
});


app.MapGet("/api/clothes/{id}", async (ShopDbContext context, int id) =>
{
    Clothing? clothing = await context.Clothes
        .Include(c => c.ClothingType)
        .Include(c => c.Brand)
        .Include(c => c.ClothingSizes).ThenInclude(cs => cs.Size)
        .Include(c => c.ClothingColors).ThenInclude(cc => cc.Color)
        .FirstOrDefaultAsync(c => c.Id == id);

    if (clothing == null)
        return Results.NotFound(new { message = "Товар не найден" });

    return Results.Json(clothing);
});


// создание товара — только менеджер
app.MapPost("/api/clothes", async (ShopDbContext context, ClothingRequest request) =>
{
    Clothing clothing = new Clothing
    {
        Name = request.Name,
        Price = request.Price,
        Description = request.Description,
        ClothingTypeId = request.ClothingTypeId,
        BrandId = request.BrandId,
        IsAvailable = true
    };

    context.Clothes.Add(clothing);
    await context.SaveChangesAsync();

    return Results.Created($"/api/clothes/{clothing.Id}", clothing);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Manager"));


// изменение товара — только менеджер
app.MapPut("/api/clothes", async (ShopDbContext context, ClothingUpdateRequest request) =>
{
    Clothing? clothing = await context.Clothes.FirstOrDefaultAsync(c => c.Id == request.Id);

    if (clothing == null)
        return Results.NotFound(new { message = "Товар не найден" });

    clothing.Name = request.Name;
    clothing.Price = request.Price;
    clothing.Description = request.Description;
    clothing.ClothingTypeId = request.ClothingTypeId;
    clothing.BrandId = request.BrandId;

    await context.SaveChangesAsync();

    return Results.Json(clothing);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Manager"));


// удаление товара — только менеджер
app.MapDelete("/api/clothes/{id}", async (ShopDbContext context, int id) =>
{
    Clothing? clothing = await context.Clothes.FirstOrDefaultAsync(c => c.Id == id);

    if (clothing == null)
        return Results.NotFound(new { message = "Товар не найден" });

    bool used = await context.OrderItems
        .AnyAsync(oi => oi.ClothingSize!.ClothingId == id);

    if (used)
        return Results.BadRequest(new { message = "Товар присутствует в заказах, удаление невозможно" });

    context.ClothingColors.RemoveRange(context.ClothingColors.Where(cc => cc.ClothingId == id));
    context.ClothingSizes.RemoveRange(context.ClothingSizes.Where(cs => cs.ClothingId == id));
    context.Favorites.RemoveRange(context.Favorites.Where(f => f.ClothingId == id));
    context.Clothes.Remove(clothing);

    await context.SaveChangesAsync();

    return Results.Json(clothing);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Manager"));


// снять с продажи / вернуть в продажу — только менеджер
app.MapPut("/api/clothes/availability", async (ShopDbContext context, AvailabilityRequest request) =>
{
    Clothing? clothing = await context.Clothes.FirstOrDefaultAsync(c => c.Id == request.ClothingId);

    if (clothing == null)
        return Results.NotFound(new { message = "Товар не найден" });

    clothing.IsAvailable = request.IsAvailable;
    await context.SaveChangesAsync();

    return Results.Json(clothing);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Manager"));


// ==================== СКЛАД ====================

app.MapGet("/api/stock/{clothingId}", async (ShopDbContext context, int clothingId) =>
{
    var stock = await context.ClothingSizes
        .Include(cs => cs.Size)
        .Where(cs => cs.ClothingId == clothingId)
        .ToListAsync();

    return Results.Json(stock);
});


// изменение количества товара определённого размера — только менеджер
app.MapPut("/api/stock", async (ShopDbContext context, StockRequest request) =>
{
    ClothingSize? stock = await context.ClothingSizes
        .Include(cs => cs.Size)
        .FirstOrDefaultAsync(cs => cs.Id == request.ClothingSizeId);

    if (stock == null)
        return Results.NotFound(new { message = "Позиция склада не найдена" });

    if (request.Quantity < 0)
        return Results.BadRequest(new { message = "Количество не может быть отрицательным" });

    stock.Quantity = request.Quantity;
    await context.SaveChangesAsync();

    return Results.Json(stock);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Manager"));


// ==================== ЗАКАЗЫ ====================

// создание заказа — любой авторизованный
app.MapPost("/api/orders", async (ShopDbContext context, ClaimsPrincipal principal, CreateOrderRequest request) =>
{
    int userId = GetUserId(principal);

    if (request.Items == null || request.Items.Count == 0)
        return Results.BadRequest(new { message = "Заказ не содержит позиций" });

    OrderStatus? status = await context.OrderStatuses.FirstOrDefaultAsync(s => s.Name == "new");
    if (status == null)
        return Results.BadRequest(new { message = "Статус заказа не найден" });

    Order order = new Order
    {
        UserId = userId,
        OrderStatusId = status.Id,
        CreatedAt = DateTime.Now
    };

    decimal total = 0;

    foreach (var item in request.Items)
    {
        ClothingSize? stock = await context.ClothingSizes
            .Include(cs => cs.Clothing)
            .Include(cs => cs.Size)
            .FirstOrDefaultAsync(cs => cs.Id == item.ClothingSizeId);

        if (stock == null)
            return Results.BadRequest(new { message = $"Позиция склада {item.ClothingSizeId} не найдена" });

        if (stock.Clothing!.IsAvailable == false)
            return Results.BadRequest(new { message = $"Товар \"{stock.Clothing.Name}\" снят с продажи" });

        if (stock.Quantity < item.Quantity)
            return Results.BadRequest(new { message = $"Недостаточно товара \"{stock.Clothing.Name}\" размера {stock.Size!.LetterSize}" });

        stock.Quantity -= item.Quantity;
        stock.Clothing.PurchaseCount += item.Quantity;

        decimal price = stock.Clothing.Price;
        total += price * item.Quantity;

        order.OrderItems.Add(new OrderItem
        {
            ClothingSizeId = stock.Id,
            Quantity = item.Quantity,
            Price = price
        });
    }

    order.TotalPrice = total;

    context.Orders.Add(order);
    await context.SaveChangesAsync();

    return Results.Created($"/api/orders/{order.Id}", order);
}).RequireAuthorization();


// список заказов, при указании статуса — с фильтрацией
app.MapGet("/api/orders", async (ShopDbContext context, string? status) =>
{
    var query = context.Orders
        .Include(o => o.OrderStatus)
        .Include(o => o.OrderItems).ThenInclude(oi => oi.ClothingSize).ThenInclude(cs => cs!.Clothing)
        .AsQueryable();

    if (string.IsNullOrWhiteSpace(status) == false)
        query = query.Where(o => o.OrderStatus!.Name == status);

    return Results.Json(await query.ToListAsync());
}).RequireAuthorization();


// заказы конкретного покупателя
app.MapGet("/api/orders/user/{userId}", async (ShopDbContext context, int userId) =>
{
    var orders = await context.Orders
        .Include(o => o.OrderStatus)
        .Include(o => o.OrderItems).ThenInclude(oi => oi.ClothingSize).ThenInclude(cs => cs!.Clothing)
        .Where(o => o.UserId == userId)
        .ToListAsync();

    return Results.Json(orders);
}).RequireAuthorization();


// изменение статуса заказа — только менеджер
app.MapPut("/api/orders/status", async (ShopDbContext context, OrderStatusRequest request) =>
{
    Order? order = await context.Orders
        .Include(o => o.OrderStatus)
        .FirstOrDefaultAsync(o => o.Id == request.OrderId);

    if (order == null)
        return Results.NotFound(new { message = "Заказ не найден" });

    OrderStatus? status = await context.OrderStatuses
        .FirstOrDefaultAsync(s => s.Name == request.Status);

    if (status == null)
        return Results.BadRequest(new { message = "Такого статуса не существует" });

    order.OrderStatusId = status.Id;
    await context.SaveChangesAsync();

    return Results.Json(order);
}).RequireAuthorization(policy => policy.RequireAuthenticatedUser().RequireRole("Manager"));


// ==================== ИЗБРАННОЕ ====================

app.MapGet("/api/favorites", async (ShopDbContext context, ClaimsPrincipal principal) =>
{
    int userId = GetUserId(principal);

    var favorites = await context.Favorites
        .Include(f => f.Clothing).ThenInclude(c => c!.Brand)
        .Where(f => f.UserId == userId)
        .ToListAsync();

    return Results.Json(favorites);
}).RequireAuthorization();


app.MapPost("/api/favorites", async (ShopDbContext context, ClaimsPrincipal principal, FavoriteRequest request) =>
{
    int userId = GetUserId(principal);

    if (await context.Clothes.AnyAsync(c => c.Id == request.ClothingId) == false)
        return Results.NotFound(new { message = "Товар не найден" });

    if (await context.Favorites.AnyAsync(f => f.UserId == userId && f.ClothingId == request.ClothingId))
        return Results.BadRequest(new { message = "Товар уже в избранном" });

    Favorite favorite = new Favorite
    {
        UserId = userId,
        ClothingId = request.ClothingId,
        AddedAt = DateTime.Now
    };

    context.Favorites.Add(favorite);
    await context.SaveChangesAsync();

    return Results.Created($"/api/favorites/{favorite.Id}", favorite);
}).RequireAuthorization();


app.MapDelete("/api/favorites/{clothingId}", async (ShopDbContext context, ClaimsPrincipal principal, int clothingId) =>
{
    int userId = GetUserId(principal);

    Favorite? favorite = await context.Favorites
        .FirstOrDefaultAsync(f => f.UserId == userId && f.ClothingId == clothingId);

    if (favorite == null)
        return Results.NotFound(new { message = "Товар не найден в избранном" });

    context.Favorites.Remove(favorite);
    await context.SaveChangesAsync();

    return Results.Json(favorite);
}).RequireAuthorization();


app.Run();


// ==================== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ====================

string CreateToken(User user)
{
    var claims = new[]
    {
        new Claim("sub", user.Id.ToString()),
        new Claim("name", user.Login),
        new Claim("role", user.Role?.Name ?? "User")
    };

    var token = new JwtSecurityToken(
        issuer: "ClothesShop",
        audience: "ClothesShopApi",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(60),
        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

    return new JwtSecurityTokenHandler().WriteToken(token);
}

int GetUserId(ClaimsPrincipal principal)
{
    return int.Parse(principal.FindFirst("sub")!.Value);
}


// ==================== КЛАССЫ ЗАПРОСОВ ====================

public record RegisterRequest(string Login, string Password, string? Phone, string? Address);
public record LoginRequest(string Login, string Password);
public record ClothingRequest(string Name, decimal Price, string? Description, int ClothingTypeId, int BrandId);
public record ClothingUpdateRequest(int Id, string Name, decimal Price, string? Description, int ClothingTypeId, int BrandId);
public record AvailabilityRequest(int ClothingId, bool IsAvailable);
public record StockRequest(int ClothingSizeId, int Quantity);
public record CreateOrderRequest(List<OrderItemRequest> Items);
public record OrderItemRequest(int ClothingSizeId, int Quantity);
public record OrderStatusRequest(int OrderId, string Status);
public record FavoriteRequest(int ClothingId);