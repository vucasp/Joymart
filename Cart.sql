USE DBJoymart;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

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
        ShippingPhone NVARCHAR(20) NULL
    );
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.OrderPro', 'RecipientName') IS NULL
    BEGIN
        ALTER TABLE dbo.OrderPro ADD RecipientName NVARCHAR(100) NULL;
    END

    IF COL_LENGTH('dbo.OrderPro', 'ShippingPhone') IS NULL
    BEGIN
        ALTER TABLE dbo.OrderPro ADD ShippingPhone NVARCHAR(20) NULL;
    END
END
GO

UPDATE o
SET o.RecipientName = ISNULL(o.RecipientName, c.FullName),
    o.ShippingPhone = ISNULL(o.ShippingPhone, c.Phone)
FROM dbo.OrderPro o
JOIN dbo.Customer c ON o.CustomerID = c.CustomerID
WHERE o.RecipientName IS NULL OR o.ShippingPhone IS NULL;
GO

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
