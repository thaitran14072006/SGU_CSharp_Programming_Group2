USE QuanLyTiemBanh;
GO
SELECT COUNT(*) AS BusinessTableCount FROM sys.tables WHERE name IN
 (N'TaiKhoan',N'KhachHang',N'LoaiBanh',N'SanPham',N'LoBanh',N'DonDatBanh',N'ChiTietDonDat',N'HoaDon',N'ChiTietHoaDon');
-- No password hashes displayed.
SELECT MaTaiKhoan,TenDangNhap,HoTen,VaiTro,TrangThai,NgayTao FROM dbo.TaiKhoan ORDER BY MaTaiKhoan;
SELECT MaKhachHang,HoTen,SoDienThoai,DiaChi,GhiChu FROM dbo.KhachHang ORDER BY MaKhachHang;
SELECT SoDienThoai,COUNT(*) AS CountRows FROM dbo.KhachHang GROUP BY SoDienThoai HAVING COUNT(*)>1;
SELECT VaiTro,COUNT(*) AS CountRows FROM dbo.TaiKhoan GROUP BY VaiTro;
GO
