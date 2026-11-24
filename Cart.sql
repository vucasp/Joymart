USE DBJoymart;
GO

-- 1. Xóa ràng buộc UNIQUE trên Customer.Email (nếu có) để khách vãng lai đặt nhiều lần bằng cùng email
DECLARE @UQName SYSNAME;
SELECT TOP 1 @UQName = kc.name
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

IF @UQName IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.Customer DROP CONSTRAINT [' + @UQName + N']');
END
GO

-- 2. Bổ sung cột RecipientName và ShippingPhone vào bảng OrderPro
IF COL_LENGTH('dbo.OrderPro', 'RecipientName') IS NULL
BEGIN
    ALTER TABLE dbo.OrderPro ADD RecipientName NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('dbo.OrderPro', 'ShippingPhone') IS NULL
BEGIN
    ALTER TABLE dbo.OrderPro ADD ShippingPhone NVARCHAR(20) NULL;
END
GO

-- 3. Cho phép nhiều Customer guest có UserID = NULL, nhưng UserID khác NULL phải duy nhất
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

CREATE UNIQUE INDEX UX_Customer_UserID
ON dbo.Customer(UserID)
WHERE UserID IS NOT NULL;
GO