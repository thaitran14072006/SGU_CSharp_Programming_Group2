-- NAM: Read-only validation on the selected shared test database.
-- Run in SSMS connected to the correct SQL Server. No writes/DDL.
USE QuanLyTiemBanh;
GO
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS SqlServerName;

-- Schema required for Nam's web order delivery.
SELECT t.name AS TableName FROM sys.tables t
WHERE t.name IN (N'DonDatBanh',N'ChiTietDonDat',N'TaiKhoanKhachHang',N'HoaDon')
ORDER BY t.name;
SELECT c.name AS OrderColumn FROM sys.columns c
WHERE c.object_id=OBJECT_ID(N'dbo.DonDatBanh')
AND c.name IN (N'MaKhachHang',N'MaTaiKhoan',N'WebRequestKey',N'TrangThai')
ORDER BY c.name;

-- Latest web orders and employee acceptance status.
IF COL_LENGTH(N'dbo.DonDatBanh',N'WebRequestKey') IS NOT NULL
    EXEC sp_executesql N'
    SELECT TOP (30) MaDon,MaKhachHang,MaTaiKhoan,WebRequestKey,NgayDat,
           NgayNhan,TrangThai,TongTien
    FROM dbo.DonDatBanh WHERE WebRequestKey IS NOT NULL
    ORDER BY MaDon DESC';

-- Detect duplicate Web request keys (expected 0 rows).
IF COL_LENGTH(N'dbo.DonDatBanh',N'WebRequestKey') IS NOT NULL
    EXEC sp_executesql N'
    SELECT MaKhachHang,WebRequestKey,COUNT_BIG(*) AS SoBanGhi
    FROM dbo.DonDatBanh WHERE WebRequestKey IS NOT NULL
    GROUP BY MaKhachHang,WebRequestKey HAVING COUNT_BIG(*) > 1';

-- A request must not generate two invoices (expected 0 rows).
SELECT MaDon,COUNT_BIG(*) AS SoHoaDon
FROM dbo.HoaDon WHERE MaDon IS NOT NULL
GROUP BY MaDon HAVING COUNT_BIG(*) > 1;
GO
