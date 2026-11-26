-- =====================================================================
-- SCRIPT KHỞI TẠO CƠ SỞ DỮ LIỆU & DỮ LIỆU MẪU HỆ THỐNG JOYMART
-- Tương thích 100% với Entity Framework Model (DBJoymartEntities)
-- Bao gồm 8 bảng: Role, UserAccount, Customer, Category, Product,
--                 OrderPro, OrderDetail, ProductReview
-- =====================================================================

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

-- =====================================================================
-- PHẦN 1: TẠO CẤU TRÚC CÁC BẢNG (8 BẢNG)
-- =====================================================================

-- 1. Bảng Role (Phân quyền hệ thống: Admin, Customer)
CREATE TABLE dbo.Role (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE
);
GO

-- 2. Bảng UserAccount (Tài khoản người dùng - mật khẩu băm PBKDF2 + Salt)
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

-- 3. Bảng Customer (Hồ sơ khách hàng thành viên & khách vãng lai)
-- Lưu ý: Email không đặt UNIQUE để khách vãng lai (UserID = NULL) đặt nhiều đơn bằng cùng email
CREATE TABLE dbo.Customer (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NULL FOREIGN KEY REFERENCES dbo.UserAccount(UserID),
    FullName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(255) NULL
);
GO

-- Index độc nhất cho UserID khi khác NULL (mỗi UserAccount chỉ gắn tối đa 1 Customer, cho phép nhiều Guest có UserID = NULL)
CREATE UNIQUE INDEX UX_Customer_UserID
ON dbo.Customer(UserID)
WHERE UserID IS NOT NULL;
GO

-- 4. Bảng Category (Danh mục sản phẩm)
CREATE TABLE dbo.Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL
);
GO

-- 5. Bảng Product (Sản phẩm & Tồn kho đồng bộ trực tiếp giữa Kho hàng và Cửa hàng)
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

-- 6. Bảng OrderPro (Đơn đặt hàng, Xuất kho & Giao hàng)
-- Các trạng thái hợp lệ: N'Chờ xử lý' -> N'Đã xác nhận' -> N'Đang giao' -> N'Hoàn thành' (hoặc N'Đã hủy')
-- ShippingFee: Phí vận chuyển (0đ nếu tiền hàng >= 360.067đ, ngược lại 25.000đ)
-- ShippedDate: Ngày xuất kho giao hàng (quá 7 ngày chưa xác nhận sẽ tự động chuyển sang Hoàn thành)
-- CompletedDate: Thời điểm đơn hàng được xác nhận hoàn thành
CREATE TABLE dbo.OrderPro (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    OrderDate DATETIME NOT NULL DEFAULT GETDATE(),
    CustomerID INT NOT NULL FOREIGN KEY REFERENCES dbo.Customer(CustomerID),
    ShippingAddress NVARCHAR(255) NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT N'Chờ xử lý',
    RecipientName NVARCHAR(100) NULL,
    ShippingPhone NVARCHAR(20) NULL,
    ShippingFee DECIMAL(18, 2) NOT NULL DEFAULT 0,
    ShippedDate DATETIME NULL,
    CompletedDate DATETIME NULL
);
GO

-- 7. Bảng OrderDetail (Chi tiết từng dòng sản phẩm trong đơn hàng)
CREATE TABLE dbo.OrderDetail (
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL FOREIGN KEY REFERENCES dbo.OrderPro(OrderID),
    ProductID INT NOT NULL FOREIGN KEY REFERENCES dbo.Product(ProductID),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL
);
GO

-- 8. Bảng ProductReview (Đánh giá & Bình luận của khách hàng đã mua sản phẩm hoàn thành)
CREATE TABLE dbo.ProductReview (
    ReviewID INT IDENTITY(1,1) PRIMARY KEY,
    ProductID INT NOT NULL FOREIGN KEY REFERENCES dbo.Product(ProductID),
    CustomerID INT NOT NULL FOREIGN KEY REFERENCES dbo.Customer(CustomerID),
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- =====================================================================
-- PHẦN 2: DỮ LIỆU MẪU ĐỒNG BỘ VỚI PROJECT JOYMART (CHỦ ĐỀ CHIIKAWA)
-- =====================================================================

-- 1. Quyền hệ thống
INSERT INTO dbo.Role (RoleName) VALUES
(N'Admin'),
(N'Customer');
GO

-- 2. Tài khoản mẫu (Mật khẩu băm chuẩn PBKDF2 - 100.000 vòng lặp)
-- Tài khoản Admin: "admin" / "admin01" (Mật khẩu: Admin123), "FroggyPrz"
-- Tài khoản Customer: "khachhang1" / "khachhang2" (Mật khẩu: 123456), "testkh"
INSERT INTO dbo.UserAccount (UserName, PasswordHash, PasswordSalt, RoleID, IsActive) VALUES
(N'FroggyPrz',  N'WhUhcGo4qSMNrtfJ0bRhbq2DTxqOOdSzcq8pp0GBqq8=', N'tCbW7H6O9RQ1XBfqxvn0jw==', 1, 1),
(N'admin',      N'T9zFheoC0o++9Uwj0wHDfUXYNPbUMw1w2Ixl1Iz33TY=', N'A1Jbvcjn8doV1vNdZb8+ew==', 1, 1),
(N'admin01',    N'T9zFheoC0o++9Uwj0wHDfUXYNPbUMw1w2Ixl1Iz33TY=', N'A1Jbvcjn8doV1vNdZb8+ew==', 1, 1),
(N'testkh',     N'Yqf3r/d6ICSv0pt2y8pJmu5rlNpBA5rDI7COeAYQo/I=', N'W+q8Ds6ku1jMlmCjAnSWmQ==', 2, 1),
(N'khachhang1', N'TiQvNqrIHZGO+NUgKE7qLR/1kLv0hTcaiOnrKtZEbqI=', N'Q/K+k2iR2FvNZLC0jEA7fQ==', 2, 1),
(N'khachhang2', N'TiQvNqrIHZGO+NUgKE7qLR/1kLv0hTcaiOnrKtZEbqI=', N'Q/K+k2iR2FvNZLC0jEA7fQ==', 2, 1);
GO

-- 3. Hồ sơ khách hàng (Họ tên <= 20 ký tự, SĐT 10 số bắt đầu bằng 0, Email @gmail.com)
INSERT INTO dbo.Customer (UserID, FullName, Phone, Email, Address) VALUES
(1,    N'Lê Tuấn Vũ',       N'0934154153', N'vucasp@gmail.com',   N'3/20 Ấp 13, Xã Hiệp Phước, TP. Hồ Chí Minh'),
(4,    N'Test Khách Hàng',  N'0234567891', N'testkh@gmail.com',   N'828 Sư Vạn Hạnh, Quận 10, TP.HCM'),
(5,    N'Nguyễn Minh Khoa', N'0901234567', N'khoanm@gmail.com',   N'123 Nguyễn Văn Cừ, Quận 5, TP.HCM'),
(6,    N'Trần Thảo Vy',     N'0912345678', N'vytt@gmail.com',     N'45 Lê Lợi, Quận 1, TP.HCM'),
(NULL, N'Phạm Gia Hân',     N'0987654321', N'giahan@gmail.com',   N'19 Nguyễn Hữu Thọ, Quận 7, TP.HCM');
GO

-- 4. Danh mục sản phẩm (4 danh mục Joymart)
INSERT INTO dbo.Category (CategoryName) VALUES
(N'Thiết bị công nghệ'),
(N'Thời trang & Phong cách'),
(N'Gia dụng & Đời sống'),
(N'Phụ kiện tiện ích');
GO

-- 5. Sản phẩm mẫu (13 sản phẩm Chiikawa khớp 100% với thư mục Content/images/products)
INSERT INTO dbo.Product (ProductName, Description, CategoryID, Price, ImageUrl, StockQuantity, IsActive) VALUES
-- Danh mục 1: Thiết bị công nghệ
(N'Chuột không dây chủ đề Chiikawa', N'Chuột không dây chủ đề Chiikawa cho máy tính', 1, 260000, N'products/tech-01.jpg', 72, 1),
(N'Bộ keycap Chiikawa',              N'Bộ keycap bàn phím chủ đề nhân vật Chiikawa',  1,  40000, N'products/tech-02.jpg', 21, 1),

-- Danh mục 2: Thời trang & Phong cách
(N'Xi líp Usagi ve vẩy',             N'Quần nhỏ nhân vật Usagi ve vẩy cực đáng yêu',  2,  50000, N'products/clo-01.jpg',  29, 1),
(N'Đồ ngủ Chiikawa',                 N'Đồ ngủ in hình nhân vật Usagi',                2, 140000, N'products/clo-02.jpg',  16, 1),
(N'Vớ Chiikawa',                     N'Tất ngắn chủ đề nhân vật Chiikawa',            2,  40000, N'products/clo-03.jpg',  33, 1),

-- Danh mục 3: Gia dụng & Đời sống
(N'Bát hốc sứ Chiikawa',             N'Bát hốc làm bằng sứ chủ đề nhân vật Chiikawa cực xinh xắn', 3, 60000, N'products/home-01.jpg', 45, 1),
(N'Dĩa sứ Chiikawa',                 N'Dĩa sứ đựng đồ ăn Chiikawa',                   3,  20000, N'products/home-02.jpg', 20, 1),
(N'Ly sứ Chiikawa',                  N'Ly sứ Chiikawa dùng để đựng đồ uống',          3,  35000, N'products/home-03.jpg', 30, 1),

-- Danh mục 4: Phụ kiện tiện ích
(N'Gấu bông Chiikawa Gyaru',         N'Gấu bông Chiikawa chủ đề Gyaru cực xinh xắn',  4,  60000, N'products/acc-01.jpg',  10, 1),
(N'Mũ bảo hiểm Chiikawa',            N'Mũ bảo hiểm Chiikawa đạt chứng nhận tiêu chuẩn 3C', 4, 120000, N'products/acc-02.jpg', 22, 1),
(N'Gấu bông chủ đề Noel Chiikawa',   N'Gấu bông chủ đề Noel Chiikawa',                4,  60000, N'products/acc-03.jpg',   4, 1),
(N'Gấu bông Momonga',                N'Gấu bông Momonga size lớn',                    4,  80000, N'products/acc-04.jpg',  11, 1),
(N'Dây buộc tóc Chiikawa',           N'Dây buộc tóc chủ đề nhân vật Chiikawa',        4,  21000, N'products/acc-05.jpg',  48, 1);
GO

-- 6. Đơn hàng mẫu (Gồm đơn Hoàn thành, Chờ xử lý, và Đang giao)
INSERT INTO dbo.OrderPro (OrderDate, CustomerID, RecipientName, ShippingPhone, ShippingAddress, ShippingFee, TotalAmount, Status, ShippedDate, CompletedDate) VALUES
(DATEADD(DAY, -5, GETDATE()), 3, N'Nguyễn Minh Khoa', N'0901234567', N'123 Nguyễn Văn Cừ, Quận 5, TP.HCM', 25000, 325000, N'Hoàn thành', DATEADD(DAY, -4, GETDATE()), DATEADD(DAY, -2, GETDATE())),
(DATEADD(DAY, -1, GETDATE()), 4, N'Trần Thảo Vy',     N'0912345678', N'45 Lê Lợi, Quận 1, TP.HCM',         25000, 225000, N'Chờ xử lý',  NULL,                        NULL),
(GETDATE(),                   5, N'Phạm Gia Hân',     N'0987654321', N'19 Nguyễn Hữu Thọ, Quận 7, TP.HCM', 25000, 155000, N'Đang giao',  GETDATE(),                   NULL);
GO

-- 7. Chi tiết đơn hàng mẫu
INSERT INTO dbo.OrderDetail (OrderID, ProductID, Quantity, UnitPrice) VALUES
(1, 1, 1, 260000),
(1, 2, 1,  40000),
(2, 4, 1, 140000),
(2, 9, 1,  60000),
(3, 6, 1,  60000),
(3, 8, 2,  35000);
GO

-- 8. Đánh giá sản phẩm mẫu từ khách hàng đã mua hàng hoàn thành
INSERT INTO dbo.ProductReview (ProductID, CustomerID, Rating, Comment, CreatedAt) VALUES
(1, 3, 5, N'Chuột không dây thiết kế chủ đề Chiikawa rất đẹp, cầm vừa tay và bấm êm.', DATEADD(DAY, -1, GETDATE())),
(2, 3, 5, N'Bộ keycap in hình sắc nét, lắp vào bàn phím cơ gõ rất thích.', DATEADD(DAY, -1, GETDATE()));
GO
