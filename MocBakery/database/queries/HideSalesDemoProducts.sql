-- Hide old sales fixtures without deleting invoice/lot history.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
UPDATE dbo.SanPham SET DangKinhDoanh = 0
WHERE TenSanPham IN (N'[TEST] Bánh Sales KT06', N'DEMO - Bánh mì bơ tỏi');
COMMIT;
SELECT MaSanPham, TenSanPham, DangKinhDoanh FROM dbo.SanPham
WHERE TenSanPham IN (N'[TEST] Bánh Sales KT06', N'DEMO - Bánh mì bơ tỏi');
