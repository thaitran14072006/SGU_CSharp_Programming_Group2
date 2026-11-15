-- Read-only verification after the documented GUI run. Never use against a business database.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE N'MocBakery[_]NhuGuiTest[_]%'
    THROW 50100, 'Select the isolated GUI test database.', 1;
DECLARE @product INT=(SELECT MaSanPham FROM dbo.SanPham WHERE TenSanPham=N'GUI - Bánh kem thử');
IF NOT EXISTS(SELECT 1 FROM dbo.LoaiBanh WHERE TenLoai=N'GUI - Bánh thử 15-11')
    THROW 50101, 'GUI category was not persisted.', 1;
PRINT 'PASS GUI category persisted';
IF NOT EXISTS(SELECT 1 FROM dbo.SanPham WHERE MaSanPham=@product AND GiaBan=40000 AND DangKinhDoanh=1)
    THROW 50102, 'GUI price update was not persisted.', 1;
PRINT 'PASS GUI product price updated from 35000 to 40000';
IF (SELECT COUNT(*) FROM dbo.LoBanh WHERE MaSanPham=@product)<>1 OR
   NOT EXISTS(SELECT 1 FROM dbo.LoBanh WHERE MaSanPham=@product AND SoLuongNhap=6 AND SoLuongCon=6
        AND TinhTrang=N'DangBan' AND NguonNhap=N'GUI Test 15-11')
    THROW 50103, 'GUI lot differs from expected fixture.', 1;
PRINT 'PASS GUI lot persisted once with quantity 6 and resumed state';
DECLARE @date DATE=CAST(GETDATE() AS DATE);
IF (SELECT SUM(l.SoLuongCon) FROM dbo.LoBanh l JOIN dbo.SanPham p ON p.MaSanPham=l.MaSanPham
    WHERE p.MaSanPham=@product AND p.DangKinhDoanh=1 AND l.TinhTrang=N'DangBan'
    AND l.SoLuongCon>0 AND l.NgaySanXuat<=@date AND l.HanSuDung>=@date)<>6
    THROW 50104, 'Sellable stock differs from GUI.', 1;
PRINT 'PASS GUI sellable stock matches SQL: 6';
SELECT p.MaSanPham,p.TenSanPham,p.GiaBan,l.MaLo,l.SoLuongNhap,l.SoLuongCon,l.TinhTrang,l.HanSuDung
FROM dbo.SanPham p JOIN dbo.LoBanh l ON p.MaSanPham=l.MaSanPham WHERE p.MaSanPham=@product;
