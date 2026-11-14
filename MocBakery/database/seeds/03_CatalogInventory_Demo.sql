-- Optional demo data. Run on a TEST copy after 01_CreateDatabase.sql.
-- Dates are relative to the SQL Server date. Existing demo stock is never reset.
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
DECLARE @category INT, @product INT, @inactive INT, @today DATE=CAST(GETDATE() AS DATE);
SELECT @category=MaLoai FROM dbo.LoaiBanh WITH (UPDLOCK,HOLDLOCK) WHERE TenLoai=N'DEMO - Bánh Mộc';
IF @category IS NULL
BEGIN
    INSERT dbo.LoaiBanh(TenLoai,MoTa) VALUES(N'DEMO - Bánh Mộc',N'Dữ liệu thử Catalog/Inventory');
    SET @category=SCOPE_IDENTITY();
END;
SELECT @product=MaSanPham FROM dbo.SanPham WITH (UPDLOCK,HOLDLOCK) WHERE TenSanPham=N'DEMO - Bánh mì bơ tỏi';
IF @product IS NULL
BEGIN
    INSERT dbo.SanPham(MaLoai,TenSanPham,DonViTinh,GiaBan,DangKinhDoanh,MoTa)
    VALUES(@category,N'DEMO - Bánh mì bơ tỏi',N'Cái',25000,1,N'Tồn ban đầu: 14; có thể bán: 9');
    SET @product=SCOPE_IDENTITY();
    INSERT dbo.LoBanh(MaSanPham,NguonNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon,TinhTrang) VALUES
    (@product,N'DEMO',DATEADD(day,-1,@today),@today,4,4,N'DangBan'),
    (@product,N'DEMO',@today,DATEADD(day,3,@today),5,5,N'DangBan'),
    (@product,N'DEMO',DATEADD(day,-3,@today),DATEADD(day,-1,@today),3,3,N'DangBan'),
    (@product,N'DEMO',@today,DATEADD(day,2,@today),2,2,N'NgungBan'),
    (@product,N'DEMO',@today,DATEADD(day,3,@today),1,0,N'DangBan');
END;
SELECT @inactive=MaSanPham FROM dbo.SanPham WITH (UPDLOCK,HOLDLOCK) WHERE TenSanPham=N'DEMO - Bánh ngừng kinh doanh';
IF @inactive IS NULL
BEGIN
    INSERT dbo.SanPham(MaLoai,TenSanPham,DonViTinh,GiaBan,DangKinhDoanh)
    VALUES(@category,N'DEMO - Bánh ngừng kinh doanh',N'Cái',30000,0);
    SET @inactive=SCOPE_IDENTITY();
    INSERT dbo.LoBanh(MaSanPham,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon)
    VALUES(@inactive,@today,DATEADD(day,3,@today),7,7);
END;
COMMIT;
GO
