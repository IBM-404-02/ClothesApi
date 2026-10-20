USE ClothesShop;
GO

CREATE TABLE ClothingTypes (
Id INT IDENTITY(1,1) PRIMARY KEY,
Name NVARCHAR(50) NOT NULL
);

CREATE TABLE Clothes (
Id INT IDENTITY(1,1) PRIMARY KEY,
Name NVARCHAR(200) NOT NULL,
Price DECIMAL(10,2) NOT NULL,
Description NVARCHAR(MAX) NULL,
PurchaseCount INT NOT NULL DEFAULT 0,
ClothingTypeId INT NOT NULL,
IsAvailable BIT NOT NULL DEFAULT 1,

CONSTRAINT FK_Clothes_Types
FOREIGN KEY (ClothingTypeId) REFERENCES ClothingTypes(Id)
);

CREATE TABLE ClothingSizes (
Id INT IDENTITY(1,1) PRIMARY KEY,
ClothingId INT NOT NULL,
LetterSize NVARCHAR(10) NOT NULL,
NumericSize INT NOT NULL,
Quantity INT NOT NULL DEFAULT 0,

CONSTRAINT FK_Sizes_Clothes
FOREIGN KEY (ClothingId) REFERENCES Clothes(Id)
);

CREATE TABLE Users (
Id INT IDENTITY(1,1) PRIMARY KEY,
Login NVARCHAR(50) NOT NULL UNIQUE,
PasswordHash NVARCHAR(255) NOT NULL,
Phone NVARCHAR(20) NULL,
Address NVARCHAR(300) NULL,
Role NVARCHAR(20) NOT NULL DEFAULT 'user'
);

CREATE TABLE Orders (
Id INT IDENTITY(1,1) PRIMARY KEY,
CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
Status NVARCHAR(30) NOT NULL DEFAULT 'new',
UserId INT NOT NULL,

CONSTRAINT FK_Orders_Users
FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE OrderItems (
Id INT IDENTITY(1,1) PRIMARY KEY,
OrderId INT NOT NULL,
ClothingSizeId INT NOT NULL,
Quantity INT NOT NULL DEFAULT 1,

CONSTRAINT FK_Items_Orders
FOREIGN KEY (OrderId) REFERENCES Orders(Id),
CONSTRAINT FK_Items_Sizes
FOREIGN KEY (ClothingSizeId) REFERENCES ClothingSizes(Id)
);
GO

INSERT INTO ClothingTypes (Name) VALUES
(N'Футболка'), -- 1
(N'Худи'), -- 2
(N'Штаны'), -- 3
(N'Лонгслив'), -- 4
(N'Свитшот'), -- 5
(N'Шорты'); -- 6

INSERT INTO Clothes (Name, Price, Description, ClothingTypeId, IsAvailable) VALUES
(N'Футболка Oversize', 1990.00, N'Хлопок 100%', 1, 1), -- 1
(N'Футболка Basic', 1290.00, N'Базовая модель', 1, 1), -- 2
(N'Худи Classic', 3990.00, N'Тёплое худи', 2, 1), -- 3
(N'Футболка Oversize Black', 2190.00, N'Плотный хлопок, свободный крой', 1, 1), -- 4
(N'Футболка Printed', 1690.00, N'С принтом', 1, 1), -- 5
(N'Худи Zip', 4590.00, N'На молнии, с капюшоном', 2, 1), -- 6
(N'Лонгслив Basic', 2290.00, N'Длинный рукав', 4, 1), -- 7
(N'Свитшот Grey', 3390.00, N'Без капюшона', 5, 1), -- 8
(N'Шорты Sport', 1890.00, N'Для тренировок', 6, 0); -- 9

INSERT INTO ClothingSizes (ClothingId, LetterSize, NumericSize, Quantity) VALUES
(1, N'S', 44, 10),
(1, N'M', 46, 5),
(1, N'L', 48, 0),
(2, N'M', 46, 12),
(3, N'L', 48, 3),
(4, N'S', 44, 7),
(4, N'M', 46, 12),
(4, N'L', 48, 4),
(5, N'M', 46, 9),
(5, N'L', 48, 6),
(6, N'L', 48, 2),
(6, N'XL', 50, 5),
(7, N'M', 46, 15),
(8, N'L', 48, 8),
(9, N'S', 44, 0),
(9, N'M', 46, 3);
GO

SELECT c.Id, c.Name, t.Name AS Type, c.Price, c.IsAvailable
FROM Clothes c
JOIN ClothingTypes t ON c.ClothingTypeId = t.Id;

SELECT s.Id, c.Name, s.LetterSize, s.NumericSize, s.Quantity
FROM ClothingSizes s
JOIN Clothes c ON s.ClothingId = c.Id;
GO
