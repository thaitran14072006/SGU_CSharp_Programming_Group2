USE QuanLyTiemBanh;
GO
-- Doanh thu theo hóa đơn, không cộng riêng tiền cọc
SELECT CONVERT(date,NgayLap) AS Ngay,COUNT(*) AS SoHoaDon,SUM(TongTien) AS DoanhThu
FROM dbo.HoaDon GROUP BY CONVERT(date,NgayLap) ORDER BY Ngay DESC;
-- Mỗi đơn tối đa một hóa đơn, không giao đôi
SELECT MaDon,COUNT(*) AS SoHoaDon FROM dbo.HoaDon WHERE MaDon IS NOT NULL GROUP BY MaDon HAVING COUNT(*)>1;
-- Tổng tiền hóa đơn và các dòng, giúp kiểm tra rollback/FEFO
SELECT h.MaHoaDon,h.TongTien,COALESCE(SUM(ct.SoLuong*ct.DonGia),0) AS TongChiTiet
FROM dbo.HoaDon h LEFT JOIN dbo.ChiTietHoaDon ct ON ct.MaHoaDon=h.MaHoaDon GROUP BY h.MaHoaDon,h.TongTien
HAVING h.TongTien<>COALESCE(SUM(ct.SoLuong*ct.DonGia),0);
-- Tồn có thể bán: tính ngày hết hạn hợp lệ đến hết ngày ghi trên cột DATE
SELECT p.MaSanPham,p.TenSanPham,COALESCE(SUM(CONVERT(bigint,l.SoLuongCon)),0) AS TonCoTheBan
FROM dbo.SanPham p LEFT JOIN dbo.LoBanh l ON l.MaSanPham=p.MaSanPham AND l.SoLuongCon>0 AND l.TinhTrang=N'DangBan' AND l.HanSuDung>=CONVERT(date,SYSDATETIME())
WHERE p.DangKinhDoanh=1 GROUP BY p.MaSanPham,p.TenSanPham;
GO
