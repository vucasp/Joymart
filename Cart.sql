-- =====================================================================
-- SCRIPT CẬP NHẬT & ĐỒNG BỘ PHÂN HỆ GIỎ HÀNG - ĐẶT HÀNG - XUẤT KHO - ĐÁNH GIÁ
-- An toàn khi chạy nhiều lần (Idempotent), không gây lỗi nếu chạy trên CSDL
-- mới tạo từ DBJoymart.sql hoặc trên CSDL cũ cần nâng cấp thêm cột/bảng.
-- =====================================================================

USE DBJoymart;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Đảm bảo cột Customer.UserID cho phép NULL (phục vụ khách vãng lai đặt hàng không cần đăng nhập)
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Customer')
      AND name = N'UserID'
      AND is_nullable = 0
)
BEGIN
    ALTER TABLE dbo.Customer ALTER COLUMN UserID INT NULL;
END
GO

-- 2. Xóa ràng buộc UNIQUE trên Customer.Email (nếu có) để khách vãng lai đặt nhiều lần bằng cùng email
DECLARE @UQEmail SYSNAME;
SELECT TOP 1 @UQEmail = kc.name
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON kc.parent_object_id = ic.object_id
    AND kc.unique_index_id = ic.index_id
JOIN sys.columns c
    ON ic.object_id = c.object_id
    AND ic.column_id = c.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Customer')
  AND kc.type = 'UQ'
  AND c.name = N'Email';

IF @UQEmail IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.Customer DROP CONSTRAINT [' + @UQEmail + N']');
END
GO

-- 3. Xóa ràng buộc UNIQUE mặc định trên Customer.UserID (nếu có) và tạo Filtered Unique Index UX_Customer_UserID
DECLARE @UQUser SYSNAME;
SELECT TOP 1 @UQUser = kc.name
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON kc.parent_object_id = ic.object_id
    AND kc.unique_index_id = ic.index_id
JOIN sys.columns c
    ON ic.object_id = c.object_id
    AND ic.column_id = c.column_id
WHERE kc.parent_object_id = OBJECT_ID(N'dbo.Customer')
  AND kc.type = 'UQ'
  AND c.name = N'UserID';

IF @UQUser IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.Customer DROP CONSTRAINT [' + @UQUser + N']');
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Customer')
      AND name = N'UX_Customer_UserID'
)
BEGIN
    CREATE UNIQUE INDEX UX_Customer_UserID
    ON dbo.Customer(UserID)
    WHERE UserID IS NOT NULL;
END
GO

-- 4. Đảm bảo bảng OrderPro có đủ các cột: RecipientName, ShippingPhone, ShippingFee, ShippedDate, CompletedDate
IF OBJECT_ID(N'dbo.OrderPro', N'U') IS NULL
BEGIN
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
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.OrderPro', 'RecipientName') IS NULL
        ALTER TABLE dbo.OrderPro ADD RecipientName NVARCHAR(100) NULL;

    IF COL_LENGTH('dbo.OrderPro', 'ShippingPhone') IS NULL
        ALTER TABLE dbo.OrderPro ADD ShippingPhone NVARCHAR(20) NULL;

    IF COL_LENGTH('dbo.OrderPro', 'ShippingFee') IS NULL
        ALTER TABLE dbo.OrderPro ADD ShippingFee DECIMAL(18, 2) NOT NULL CONSTRAINT DF_OrderPro_ShippingFee DEFAULT 0;

    IF COL_LENGTH('dbo.OrderPro', 'ShippedDate') IS NULL
        ALTER TABLE dbo.OrderPro ADD ShippedDate DATETIME NULL;

    IF COL_LENGTH('dbo.OrderPro', 'CompletedDate') IS NULL
        ALTER TABLE dbo.OrderPro ADD CompletedDate DATETIME NULL;
END
GO

-- Đồng bộ thông tin người nhận & SĐT cho các đơn hàng cũ chưa có dữ liệu (nếu có)
UPDATE o
SET o.RecipientName = ISNULL(o.RecipientName, c.FullName),
    o.ShippingPhone = ISNULL(o.ShippingPhone, c.Phone)
FROM dbo.OrderPro o
JOIN dbo.Customer c ON o.CustomerID = c.CustomerID
WHERE o.RecipientName IS NULL OR o.ShippingPhone IS NULL;
GO

-- 5. Đảm bảo bảng OrderDetail tồn tại
IF OBJECT_ID(N'dbo.OrderDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderDetail (
        OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
        OrderID INT NOT NULL FOREIGN KEY REFERENCES dbo.OrderPro(OrderID),
        ProductID INT NOT NULL FOREIGN KEY REFERENCES dbo.Product(ProductID),
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18, 2) NOT NULL
    );
END
GO

-- 6. Đảm bảo bảng ProductReview tồn tại (Đánh giá & Bình luận sản phẩm)
IF OBJECT_ID(N'dbo.ProductReview', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductReview (
        ReviewID INT IDENTITY(1,1) PRIMARY KEY,
        ProductID INT NOT NULL FOREIGN KEY REFERENCES dbo.Product(ProductID),
        CustomerID INT NOT NULL FOREIGN KEY REFERENCES dbo.Customer(CustomerID),
        Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
        Comment NVARCHAR(500) NOT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO
