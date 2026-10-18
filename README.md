# ClothesApi

API для интернет-магазина одежды. Практическая работа №3.
Прасолова ЕА, Соболенко АР 4ИСИП-423

## Стек

- ASP.NET Core 8, минимальные API
- Entity Framework Core 8.0.11 (SqlServer)
- MS SQL Server

## Запуск

1. Выполнить `schema.sql` в SQL Server Management Studio
2. В файле `ShopDbContext.cs` указать своё имя сервера в строке подключения
3. Запустить проект, адрес появится в консоли

## Структура базы

- `ClothingTypes` — справочник типов одежды
- `Clothes` — товары
- `ClothingSizes` — размеры товара и остатки на складе
- `Users` — покупатели и менеджеры, пароль хранится как SHA-256
- `Orders` — заказы
- `OrderItems` — позиции заказа

## Эндпоинты

| Метод | Путь | Доступ |
|---|---|---|
| POST | /api/auth/register | все |
| POST | /api/auth/login | все |
| GET | /api/clothes | все |
| GET | /api/clothes/search?name= | все |
| POST | /api/orders | авторизованные |
| GET | /api/orders?status= | авторизованные |
| GET | /api/orders/user/{id} | авторизованные |
| PATCH | /api/orders/{id}/status | менеджер |
| PATCH | /api/sizes/{id}/quantity | менеджер |
| PATCH | /api/clothes/{id}/unpublish | менеджер |
| PATCH | /api/clothes/{id}/publish | менеджер |

## Авторизация

При входе выдаётся токен. Передаётся в заголовке:

Authorization: Bearer <токен>

## Тестирование

Коллекция запросов Postman — файл `ClothesApi.postman_collection.json`.
