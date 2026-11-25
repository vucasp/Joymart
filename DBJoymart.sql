IF DB_ID(N'DBJoymart') IS NULL
BEGIN
    CREATE DATABASE DBJoymart;
END
GO

USE DBJoymart;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE dbo.Role (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);
GO

CREATE TABLE dbo.UserAccount (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    PasswordSalt NVARCHAR(255) NULL,
    RoleID INT NOT NULL FOREIGN KEY REFERENCES dbo.Role(RoleID),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
GO

CREATE TABLE dbo.Customer (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NULL FOREIGN KEY REFERENCES dbo.UserAccount(UserID),
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(255) NULL
);
GO

CREATE UNIQUE INDEX UX_Customer_UserID
ON dbo.Customer(UserID)
WHERE UserID IS NOT NULL;
GO

CREATE TABLE dbo.Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE dbo.Product (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    CategoryID INT NOT NULL FOREIGN KEY REFERENCES dbo.Category(CategoryID),
    Price DECIMAL(18, 2) NOT NULL,
    ImageUrl NVARCHAR(255) NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE dbo.OrderPro (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    CustomerID INT NOT NULL FOREIGN KEY REFERENCES dbo.Customer(CustomerID),
    ShippingAddress NVARCHAR(255) NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT N'Chờ xử lý',
    RecipientName NVARCHAR(100) NULL,
    ShippingPhone NVARCHAR(20) NULL
);
GO

CREATE TABLE dbo.OrderDetail (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL FOREIGN KEY REFERENCES dbo.OrderPro(OrderID),
    ProductID INT NOT NULL FOREIGN KEY REFERENCES dbo.Product(ProductID),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL
);
GO

INSERT INTO dbo.Role (RoleName) VALUES
(N'Admin'),
(N'Customer');
GO

INSERT INTO dbo.UserAccount (UserName, PasswordHash, PasswordSalt, RoleID, IsActive) VALUES
(N'FroggyPrz',  N'WhUhcGo4qSMNrtfJ0bRhbq2DTxqOOdSzcq8pp0GBqq8=', N'tCbW7H6O9RQ1XBfqxvn0jw==', 1, 1),
(N'admin',      N'T9zFheoC0o++9Uwj0wHDfUXYNPbUMw1w2Ixl1Iz33TY=', N'A1Jbvcjn8doV1vNdZb8+ew==', 1, 1),
(N'admin01',    N'T9zFheoC0o++9Uwj0wHDfUXYNPbUMw1w2Ixl1Iz33TY=', N'A1Jbvcjn8doV1vNdZb8+ew==', 1, 1),
(N'testkh',     N'Yqf3r/d6ICSv0pt2y8pJmu5rlNpBA5rDI7COeAYQo/I=', N'W+q8Ds6ku1jMlmCjAnSWmQ==', 2, 1),
(N'khachhang1', N'TiQvNqrIHZGO+NUgKE7qLR/1kLv0hTcaiOnrKtZEbqI=', N'Q/K+k2iR2FvNZLC0jEA7fQ==', 2, 1),
(N'khachhang2', N'TiQvNqrIHZGO+NUgKE7qLR/1kLv0hTcaiOnrKtZEbqI=', N'Q/K+k2iR2FvNZLC0jEA7fQ==', 2, 1);
GO

INSERT INTO dbo.Customer (UserID, FullName, Phone, Email, Address) VALUES
(1,    N'Lê Tuấn Vũ',       N'0934154153', N'vucasp@gmail.com',   N'3/20 Ấp 13, Xã Hiệp Phước, TP. Hồ Chí Minh'),
(4,    N'Test Khách Hàng',  N'0234567891', N'testkh@gmail.com',   N'828 Sư Vạn Hạnh, Quận 10, TP.HCM'),
(5,    N'Nguyễn Minh Khoa', N'0901234567', N'khoanm@gmail.com',   N'123 Nguyễn Văn Cừ, Quận 5, TP.HCM'),
(6,    N'Trần Thảo Vy',     N'0912345678', N'vytt@gmail.com',     N'45 Lê Lợi, Quận 1, TP.HCM'),
(NULL, N'Phạm Gia Hân',     N'0987654321', N'giahan@gmail.com',   N'19 Nguyễn Hữu Thọ, Quận 7, TP.HCM');
GO

INSERT INTO dbo.Category (CategoryName) VALUES
(N'Thiết bị công nghệ'),
(N'Thời trang & Phong cách'),
(N'Gia dụng & Đời sống'),
(N'Phụ kiện tiện ích');
GO

INSERT INTO dbo.Product (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive) VALUES
(N'Chuột không dây chủ đề Chiikawa', N'Chuột không dây chủ đề Chiikawa cho máy tính', 1, 260000, N'products/tech-01.jpg', 72, 1),
(N'Bộ keycap Chiikawa',              N'Bộ keycap bàn phím chủ đề nhân vật Chiikawa',  1,  40000, N'products/tech-02.jpg', 21, 1),

(N'Xi líp Usagi ve vẩy',             N'Quần nhỏ nhân vật Usagi ve vẩy cực đáng yêu',  2,  50000, N'products/clo-01.jpg',  29, 1),
(N'Đồ ngủ Chiikawa',                 N'Đồ ngủ in hình nhân vật Usagi',                2, 140000, N'products/clo-02.jpg',  16, 1),
(N'Vớ Chiikawa',                     N'Tất ngắn chủ đề nhân vật Chiikawa',            2,  40000, N'products/clo-03.jpg',  33, 1),

(N'Bát hốc sứ Chiikawa',             N'Bát hốc làm bằng sứ chủ đề nhân vật Chiikawa cực xinh xắn', 3, 60000, N'products/home-01.jpg', 45, 1),
(N'Dĩa sứ Chiikawa',                 N'Dĩa sứ đựng đồ ăn Chiikawa',                   3,  20000, N'products/home-02.jpg', 20, 1),
(N'Ly sứ Chiikawa',                  N'Ly sứ Chiikawa dùng để đựng đồ uống',          3,  35000, N'products/home-03.jpg', 30, 1),

(N'Gấu bông Chiikawa Gyaru',         N'Gấu bông Chiikawa chủ đề Gyaru cực xinh xắn',  4,  60000, N'products/acc-01.jpg',  10, 1),
(N'Mũ bảo hiểm Chiikawa',            N'Mũ bảo hiểm Chiikawa đạt chứng nhận tiêu chuẩn 3C', 4, 120000, N'products/acc-02.jpg', 22, 1),
(N'Gấu bông chủ đề Noel Chiikawa',   N'Gấu bông chủ đề Noel Chiikawa',                4,  60000, N'products/acc-03.jpg',   6, 1),
(N'Gấu bông Momonga',                N'Gấu bông Momonga size lớn',                    4,  80000, N'products/acc-04.jpg',  11, 1),
(N'Dây buộc tóc Chiikawa',           N'Dây buộc tóc chủ đề nhân vật Chiikawa',        4,  21000, N'products/acc-05.jpg',  48, 1);
GO

INSERT INTO dbo.OrderPro (OrderDate, CustomerID, RecipientName, ShippingPhone, ShippingAddress, TotalAmount, Status) VALUES
(DATEADD(DAY, -3, GETDATE()), 3, N'Nguyễn Minh Khoa', N'0901234567', N'123 Nguyễn Văn Cừ, Quận 5, TP.HCM', 300000, N'Hoàn thành'),
(DATEADD(DAY, -1, GETDATE()), 4, N'Trần Thảo Vy',     N'0912345678', N'45 Lê Lợi, Quận 1, TP.HCM',         200000, N'Chờ xử lý'),
(GETDATE(),                   5, N'Phạm Gia Hân',     N'0987654321', N'19 Nguyễn Hữu Thọ, Quận 7, TP.HCM', 130000, N'Đang giao');
GO

INSERT INTO dbo.OrderDetail (OrderID, ProductID, Quantity, UnitPrice) VALUES
(1, 1, 1, 260000),
(1, 2, 1,  40000),

(2, 4, 1, 140000),
(2, 9, 1,  60000),

(3, 6, 1,  60000),
(3, 8, 2,  35000);
GO
