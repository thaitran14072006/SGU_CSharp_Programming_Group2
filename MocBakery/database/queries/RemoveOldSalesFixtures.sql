-- Explicit cleanup of obsolete local fixtures; select the intended database first.
-- Back up the database before executing this file.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
DECLARE @products TABLE (Id int PRIMARY KEY);
INSERT @products SELECT MaSanPham FROM dbo.SanPham
WHERE TenSanPham IN (N'[TEST] Bánh Sales KT06',N'DEMO - Bánh mì bơ tỏi',N'DEMO - Bánh ngừng kinh doanh');
DECLARE @lots TABLE (Id int PRIMARY KEY);
INSERT @lots SELECT MaLo FROM dbo.LoBanh WHERE MaSanPham IN (SELECT Id FROM @products);
DECLARE @invoices TABLE (Id int PRIMARY KEY);
INSERT @invoices SELECT DISTINCT MaHoaDon FROM dbo.ChiTietHoaDon WHERE MaLo IN (SELECT Id FROM @lots);
IF EXISTS (SELECT 1 FROM dbo.ChiTietHoaDon WHERE MaHoaDon IN(SELECT Id FROM @invoices) AND MaLo NOT IN(SELECT Id FROM @lots))
    THROW 51000,N'Cleanup stopped: mixed real/test invoice.',1;
IF EXISTS (SELECT 1 FROM dbo.HoaDon WHERE MaHoaDon IN(SELECT Id FROM @invoices) AND MaDon IS NOT NULL)
    THROW 51000,N'Cleanup stopped: invoice linked to an order.',1;
IF EXISTS (SELECT 1 FROM dbo.ChiTietDonDat WHERE MaSanPham IN(SELECT Id FROM @products))
 OR EXISTS (SELECT 1 FROM dbo.ChiTietPhieuNhap WHERE MaSanPham IN(SELECT Id FROM @products))
 OR EXISTS (SELECT 1 FROM dbo.BienDongKho WHERE MaLoBanh IN(SELECT Id FROM @lots))
    THROW 51000,N'Cleanup stopped: fixture used by another workflow.',1;
DELETE FROM dbo.ChiTietHoaDon WHERE MaHoaDon IN (SELECT Id FROM @invoices);
DELETE FROM dbo.HoaDon WHERE MaHoaDon IN (SELECT Id FROM @invoices);
DELETE FROM dbo.LoBanh WHERE MaLo IN (SELECT Id FROM @lots);
DELETE FROM dbo.SanPham WHERE MaSanPham IN (SELECT Id FROM @products);
DELETE c FROM dbo.LoaiBanh c
WHERE c.TenLoai IN (N'[TEST] Sales KT06',N'DEMO - Bánh Mộc')
AND NOT EXISTS (SELECT 1 FROM dbo.SanPham p WHERE p.MaLoai=c.MaLoai);
COMMIT;
SELECT COUNT(*) AS RemainingFixtureProducts FROM dbo.SanPham
WHERE TenSanPham IN(N'[TEST] Bánh Sales KT06',N'DEMO - Bánh mì bơ tỏi',N'DEMO - Bánh ngừng kinh doanh');
SELECT COUNT(*) AS RemainingFixtureCategories FROM dbo.LoaiBanh
WHERE TenLoai IN(N'[TEST] Sales KT06',N'DEMO - Bánh Mộc');
