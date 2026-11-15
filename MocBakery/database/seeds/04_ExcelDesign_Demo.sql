-- Optional opening-balance fixture matching the workbook's example screens.
-- TEST DATABASE ONLY. No accounts/passwords are created; existing stock is never reset.
USE QuanLyTiemBanh;
GO
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
DECLARE @today DATE=CAST(GETDATE() AS DATE);
IF NOT EXISTS(SELECT 1 FROM dbo.NhaCungCap WHERE TenNCC=N'An Phú')
 INSERT dbo.NhaCungCap(TenNCC,DienThoai,DiaChi,Email) VALUES(N'An Phú','02812345678',N'TP.HCM','contact@example.test');
DECLARE @cakes TABLE(Category NVARCHAR(100),Name NVARCHAR(100),Price DECIMAL(18,2),Qty INT,Shelf INT,Active BIT);
INSERT @cakes VALUES(N'Bánh mì',N'Croissant bơ',28000,32,1,1),(N'Bánh lạnh',N'Tiramisu mini',45000,18,2,1),
 (N'Bánh kem',N'Bánh kem dâu',320000,5,5,1),(N'Bánh quy',N'Cookie chocolate',18000,48,14,1),(N'Bánh ngọt',N'Muffin việt quất',35000,0,2,0);
INSERT dbo.LoaiBanh(TenLoai) SELECT DISTINCT c.Category FROM @cakes c WHERE NOT EXISTS(SELECT 1 FROM dbo.LoaiBanh l WHERE l.TenLoai=c.Category);
DECLARE @new TABLE(Id INT,Name NVARCHAR(150));
INSERT dbo.SanPham(MaLoai,TenSanPham,DonViTinh,GiaBan,DangKinhDoanh,HanDungNgay)
 OUTPUT INSERTED.MaSanPham,INSERTED.TenSanPham INTO @new
 SELECT l.MaLoai,c.Name,N'Cái',c.Price,c.Active,c.Shelf FROM @cakes c JOIN dbo.LoaiBanh l ON l.TenLoai=c.Category
 WHERE NOT EXISTS(SELECT 1 FROM dbo.SanPham p WHERE p.TenSanPham=c.Name);
INSERT dbo.LoBanh(MaSanPham,SoLo,NguonNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon,TinhTrang)
 SELECT n.Id,N'L'+CONVERT(NVARCHAR(20),n.Id),N'DEMO - tồn đầu kỳ',@today,DATEADD(day,c.Shelf,@today),CASE WHEN c.Qty=0 THEN 1 ELSE c.Qty END,c.Qty,N'DangBan'
 FROM @new n JOIN @cakes c ON c.Name=n.Name;
DECLARE @raw TABLE(Name NVARCHAR(150),Qty DECIMAL(18,3),LowQty DECIMAL(18,3),Days INT);
INSERT @raw VALUES(N'Bột mì số 13',8,10,35),(N'Bơ lạt',12,5,15);
DECLARE @newRaw TABLE(Id INT,Name NVARCHAR(150));
INSERT dbo.NguyenLieu(TenNguyenLieu,DonViTinh,NguongCanhBao)
 OUTPUT INSERTED.MaNguyenLieu,INSERTED.TenNguyenLieu INTO @newRaw
 SELECT r.Name,N'Kg',r.LowQty FROM @raw r WHERE NOT EXISTS(SELECT 1 FROM dbo.NguyenLieu n WHERE n.TenNguyenLieu=r.Name);
INSERT dbo.LoNguyenLieu(MaNguyenLieu,SoLo,NgayNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon)
 SELECT n.Id,N'NL'+CONVERT(NVARCHAR(20),n.Id),SYSDATETIME(),@today,DATEADD(day,r.Days,@today),r.Qty,r.Qty FROM @newRaw n JOIN @raw r ON r.Name=n.Name;
COMMIT;
GO
