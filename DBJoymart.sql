CREATE DATABASE DBJoymart;
GO
USE DBJoymart;
GO

-- 1. Bảng Role (2 quyền: Admin, Customer)
CREATE TABLE Role (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL
);

-- 2. Bảng UserAccount (Tài khoản người dùng)
CREATE TABLE UserAccount (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    PasswordSalt NVARCHAR(255) NULL,
    RoleID INT NOT NULL FOREIGN KEY REFERENCES Role(RoleID),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- 3. Bảng Customer (Khách hàng)
CREATE TABLE Customer (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NULL FOREIGN KEY REFERENCES UserAccount(UserID),
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(255) NULL
);
GO

-- Index độc nhất cho UserID khi khác NULL (cho phép nhiều khách vãng lai có UserID = NULL)
CREATE UNIQUE INDEX UX_Customer_UserID
ON dbo.Customer(UserID)
WHERE UserID IS NOT NULL;
GO

-- 4. Bảng Category (4 danh mục Joymart)
CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL
);

-- 5. Bảng Product (18 sản phẩm mẫu)
CREATE TABLE Product (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    CategoryID INT NOT NULL FOREIGN KEY REFERENCES Category(CategoryID),
    Price DECIMAL(18, 2) NOT NULL,
    ImageUrl NVARCHAR(255) NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);

-- 6. Bảng OrderPro
CREATE TABLE OrderPro (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    CustomerID INT NOT NULL FOREIGN KEY REFERENCES Customer(CustomerID),
    RecipientName NVARCHAR(100) NULL,
    ShippingPhone NVARCHAR(20) NULL,
    ShippingAddress NVARCHAR(255) NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT N'Chờ xử lý'
);

-- 7. Bảng OrderDetail
CREATE TABLE OrderDetail (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL FOREIGN KEY REFERENCES OrderPro(OrderID),
    ProductID INT NOT NULL FOREIGN KEY REFERENCES Product(ProductID),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL
);
GO

-- ===== DỮ LIỆU MẪU =====
INSERT INTO Role (RoleName) VALUES (N'Admin'), (N'Customer');

-- Mật khẩu đã được mã hóa PBKDF2 chuẩn (100.000 vòng lặp)
-- admin / admin01: Mật khẩu là "Admin123"
-- khachhang1 / khachhang2: Mật khẩu là "123456"
INSERT INTO UserAccount (UserName, PasswordHash, PasswordSalt, RoleID, IsActive) VALUES
(N'admin', N'T9zFheoC0o++9Uwj0wHDfUXYNPbUMw1w2Ixl1Iz33TY=', N'A1Jbvcjn8doV1vNdZb8+ew==', 1, 1),
(N'admin01', N'T9zFheoC0o++9Uwj0wHDfUXYNPbUMw1w2Ixl1Iz33TY=', N'A1Jbvcjn8doV1vNdZb8+ew==', 1, 1),
(N'khachhang1', N'TiQvNqrIHZGO+NUgKE7qLR/1kLv0hTcaiOnrKtZEbqI=', N'Q/K+k2iR2FvNZLC0jEA7fQ==', 2, 1),
(N'khachhang2', N'TiQvNqrIHZGO+NUgKE7qLR/1kLv0hTcaiOnrKtZEbqI=', N'Q/K+k2iR2FvNZLC0jEA7fQ==', 2, 1);

INSERT INTO Customer (UserID, FullName, Phone, Email, Address) VALUES
(3, N'Nguyễn Minh Khoa', N'0901234567', N'khoanm@gmail.com', N'123 Nguyễn Văn Cừ, Quận 5, TP.HCM'),
(4, N'Trần Thảo Vy', N'0912345678', N'vytt@gmail.com', N'45 Lê Lợi, Quận 1, TP.HCM');

INSERT INTO Category (CategoryName) VALUES
(N'Thiết bị công nghệ'),
(N'Thời trang & Phong cách'),
(N'Gia dụng & Đời sống'),
(N'Phụ kiện tiện ích');

INSERT INTO Product (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive) VALUES
-- Danh mục 1: Thiết bị công nghệ
(N'Tai nghe Bluetooth Sony WH-CH520', N'Pin bền bỉ 50 giờ, âm thanh DSEE sống động, thiết kế gọn nhẹ.', 1, 1290000, N'products/tech-01.jpg', 25, 1),
(N'Bàn phím cơ không dây Keychron K2', N'Layout 75% gọn gàng, đèn nền LED trắng, tương thích Windows và macOS.', 1, 1850000, N'products/tech-02.jpg', 15, 1),
(N'Chuột không dây Logitech MX Master 3S', N'Cảm biến 8000 DPI siêu nhạy, cuộn điện từ MagSpeed êm ái.', 1, 2190000, N'products/tech-01.jpg', 18, 1),
(N'Loa Bluetooth JBL Go 4', N'Chống nước bụi chuẩn IP67, âm bass mạnh mẽ, nhỏ gọn mang theo.', 1, 990000, N'products/tech-02.jpg', 30, 1),
(N'Đồng hồ thông minh Amazfit Bip 5', N'Màn hình lớn 1.91 inch, theo dõi nhịp tim và giấc ngủ 24/7.', 1, 1490000, N'products/tech-01.jpg', 20, 1),

-- Danh mục 2: Thời trang & Phong cách
(N'Áo khoác Bomber Minimalist', N'Chất vải dù trượt nước hai lớp, form dáng trẻ trung dễ phối đồ.', 2, 590000, N'products/clo-01.jpg', 40, 1),
(N'Áo Polo Cotton Pique Joymart', N'Vải cá sấu co giãn 4 chiều, thấm hút mồ hôi tốt, giữ form chuẩn.', 2, 320000, N'products/clo-02.jpg', 50, 1),
(N'Quần Jeans Slim-Fit Classic', N'Chất denim mềm mại, co giãn nhẹ, phù hợp đi học và đi làm.', 2, 480000, N'products/clo-03.jpg', 35, 1),
(N'Giày Sneaker Canvas Daily', N'Đế cao su lưu hóa chống trượt, lót êm ái cho cả ngày năng động.', 2, 650000, N'products/clo-01.jpg', 22, 1),
(N'Túi đeo chéo Unisex Urban', N'Thiết kế nhiều ngăn tiện lợi, vải polyester chống thấm nhẹ.', 2, 290000, N'products/clo-02.jpg', 45, 1),

-- Danh mục 3: Gia dụng & Đời sống
(N'Bình giữ nhiệt Lock&Lock 800ml', N'Thép không gỉ 304 cao cấp, giữ nóng lạnh lên đến 24 giờ.', 3, 350000, N'products/home-01.jpg', 60, 1),
(N'Đèn bàn học chống cận LED Pro', N'3 chế độ ánh sáng bảo vệ mắt, cảm ứng điều chỉnh độ sáng linh hoạt.', 3, 420000, N'products/home-02.jpg', 28, 1),
(N'Máy xay sinh tố cầm tay JoyBlend', N'Lưỡi dao inox 6 cánh sắc bén, sạc USB tiện lợi mang đi làm.', 3, 540000, N'products/home-03.jpg', 19, 1),
(N'Bộ hộp cơm giữ nhiệt văn phòng', N'Gồm 3 ngăn inox kèm túi giữ nhiệt và bộ muỗng đũa.', 3, 380000, N'products/home-01.jpg', 32, 1),

-- Danh mục 4: Phụ kiện tiện ích
(N'Balo Laptop chống nước JoyPack', N'Ngăn chống sốc vừa laptop 15.6 inch, tích hợp cổng sạc USB.', 4, 490000, N'products/acc-01.jpg', 27, 1),
(N'Sạc dự phòng Anker 10000mAh PD', N'Hỗ trợ sạc nhanh 22.5W hai chiều, thiết kế siêu mỏng nhẹ.', 4, 620000, N'products/acc-02.jpg', 34, 1),
(N'Giá đỡ Laptop nhôm tản nhiệt', N'Hợp kim nhôm nguyên khối, điều chỉnh 6 mức độ cao chống mỏi cổ.', 4, 260000, N'products/acc-03.jpg', 42, 1),
(N'Cáp sạc nhanh Type-C bọc dù 2m', N'Truyền dữ liệu tốc độ cao, lõi đồng nguyên chất chống đứt gãy.', 4, 150000, N'products/acc-04.jpg', 80, 1);

INSERT INTO OrderPro (CustomerID, RecipientName, ShippingPhone, ShippingAddress, TotalAmount, Status) VALUES
(1, N'Nguyễn Minh Khoa', N'0901234567', N'123 Nguyễn Văn Cừ, Quận 5, TP.HCM', 1880000, N'Hoàn thành');

INSERT INTO OrderDetail (OrderID, ProductID, Quantity, UnitPrice) VALUES
(1, 1, 1, 1290000),
(1, 2, 1, 590000);
GO
