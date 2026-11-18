USE QuanLyTiemBanh;
GO
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS SqlInstance, SYSDATETIME() AS ServerTime;
SELECT name AS CoreTableName FROM sys.tables
WHERE name IN (N'TaiKhoan',N'LoaiBanh',N'SanPham',N'LoBanh',N'KhachHang',
               N'HoaDon',N'ChiTietHoaDon',N'DonDatBanh',N'ChiTietDonDat')
ORDER BY name;
SELECT COUNT(*) AS SoLoaiBanh FROM dbo.LoaiBanh;
SELECT COUNT(*) AS SoSanPham FROM dbo.SanPham;
SELECT COUNT(*) AS SoKhachHang FROM dbo.KhachHang;
SELECT COUNT(*) AS SoDonDat FROM dbo.DonDatBanh;
SELECT COUNT(*) AS SoHoaDon FROM dbo.HoaDon;
SELECT p.TenSanPham,l.MaLo,l.NguonNhap,l.HanSuDung,l.SoLuongCon,l.TinhTrang
FROM dbo.LoBanh l JOIN dbo.SanPham p ON p.MaSanPham=l.MaSanPham
WHERE l.NguonNhap LIKE N'DEMO-%'
ORDER BY p.TenSanPham,l.HanSuDung,l.MaLo;
-- Doanh thu ghi nhan khi lap hoa don; khong cong coc lan hai.
SELECT COALESCE(SUM(TongTien),0) AS DoanhThuHoaDon FROM dbo.HoaDon;
GO
