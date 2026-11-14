USE QuanLyTiemBanh;
GO
-- Accounts are bootstrapped through the app, not placeholder hashes.
IF NOT EXISTS (SELECT 1 FROM dbo.KhachHang WHERE SoDienThoai='0901234567')
 INSERT dbo.KhachHang(HoTen,SoDienThoai,DiaChi) VALUES(N'Trần Thị Bích','0901234567',N'Quận 5, TP.HCM');
IF NOT EXISTS (SELECT 1 FROM dbo.KhachHang WHERE SoDienThoai='0912345678')
 INSERT dbo.KhachHang(HoTen,SoDienThoai,DiaChi) VALUES(N'Lê Quang Huy','0912345678',N'Quận 7, TP.HCM');
GO
