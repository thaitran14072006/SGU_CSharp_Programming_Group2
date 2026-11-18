-- Du lieu DEMO phuc vu FEFO, ban hang, Orders va Reports.
-- Chay tren database thu nghiem; khong dung de seed database san xuat.
-- Khong tao mat khau. Chu tiem tao qua man hinh Thiet lap lan dau.
USE QuanLyTiemBanh;
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.LoaiBanh WHERE TenLoai=N'DEMO - Banh kem')
    INSERT dbo.LoaiBanh(TenLoai,MoTa) VALUES (N'DEMO - Banh kem',N'Du lieu test FEFO va giao don');
IF NOT EXISTS (SELECT 1 FROM dbo.LoaiBanh WHERE TenLoai=N'DEMO - Banh mi')
    INSERT dbo.LoaiBanh(TenLoai,MoTa) VALUES (N'DEMO - Banh mi',N'Du lieu test ban hang');
DECLARE @loaiKem int = (SELECT MaLoai FROM dbo.LoaiBanh WHERE TenLoai=N'DEMO - Banh kem');
DECLARE @loaiMi int = (SELECT MaLoai FROM dbo.LoaiBanh WHERE TenLoai=N'DEMO - Banh mi');
IF NOT EXISTS (SELECT 1 FROM dbo.SanPham WHERE TenSanPham=N'DEMO - Banh kem dau')
    INSERT dbo.SanPham(MaLoai,TenSanPham,DonViTinh,GiaBan,DangKinhDoanh,MoTa)
    VALUES(@loaiKem,N'DEMO - Banh kem dau',N'cai',100000,1,N'Test chia 4+2 qua hai lo');
IF NOT EXISTS (SELECT 1 FROM dbo.SanPham WHERE TenSanPham=N'DEMO - Banh mi sua')
    INSERT dbo.SanPham(MaLoai,TenSanPham,DonViTinh,GiaBan,DangKinhDoanh,MoTa)
    VALUES(@loaiMi,N'DEMO - Banh mi sua',N'cai',20000,1,N'Test ban hang va bao cao');
DECLARE @kem int = (SELECT MaSanPham FROM dbo.SanPham WHERE TenSanPham=N'DEMO - Banh kem dau');
DECLARE @mi int = (SELECT MaSanPham FROM dbo.SanPham WHERE TenSanPham=N'DEMO - Banh mi sua');
DECLARE @today date = CONVERT(date, SYSDATETIME());
-- Ba lo DEMO: hai lo con han (4 va 5), mot lo het han khong duoc tinh vao ton.
IF NOT EXISTS(SELECT 1 FROM dbo.LoBanh WHERE MaSanPham=@kem AND NguonNhap=N'DEMO-FEFO-NEAR')
    INSERT dbo.LoBanh(MaSanPham,NguonNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon,TinhTrang)
    VALUES(@kem,N'DEMO-FEFO-NEAR',DATEADD(day,-1,@today),DATEADD(day,2,@today),4,4,N'DangBan');
IF NOT EXISTS(SELECT 1 FROM dbo.LoBanh WHERE MaSanPham=@kem AND NguonNhap=N'DEMO-FEFO-LATER')
    INSERT dbo.LoBanh(MaSanPham,NguonNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon,TinhTrang)
    VALUES(@kem,N'DEMO-FEFO-LATER',@today,DATEADD(day,6,@today),5,5,N'DangBan');
IF NOT EXISTS(SELECT 1 FROM dbo.LoBanh WHERE MaSanPham=@kem AND NguonNhap=N'DEMO-EXPIRED')
    INSERT dbo.LoBanh(MaSanPham,NguonNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon,TinhTrang)
    VALUES(@kem,N'DEMO-EXPIRED',DATEADD(day,-10,@today),DATEADD(day,-3,@today),3,3,N'DangBan');
IF NOT EXISTS(SELECT 1 FROM dbo.LoBanh WHERE MaSanPham=@mi AND NguonNhap=N'DEMO-BREAD')
    INSERT dbo.LoBanh(MaSanPham,NguonNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon,TinhTrang)
    VALUES(@mi,N'DEMO-BREAD',@today,DATEADD(day,5,@today),20,20,N'DangBan');
COMMIT;
GO
