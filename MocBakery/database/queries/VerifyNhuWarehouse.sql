-- Chỉ đọc dữ liệu. Chọn đúng database nhóm hoặc database thử trong SSMS trước khi chạy.
-- Để NULL để xem mọi phiếu; điền mã phiếu cụ thể để đối chiếu một lần nhập.
SET NOCOUNT ON;
DECLARE @MaPhieu INT = NULL;

-- Phiếu Nhap là nháp, DaGhiSo là đã tăng tồn qua nghiệp vụ ghi sổ.
SELECT p.MaPhieu,n.TenNCC,p.NgayNhap,p.TrangThai,
       COUNT(d.MaDong) AS SoDong,COALESCE(SUM(d.ThanhTien),0) AS TongTien
FROM dbo.PhieuNhap p
JOIN dbo.NhaCungCap n ON n.MaNCC=p.MaNCC
LEFT JOIN dbo.ChiTietPhieuNhap d ON d.MaPhieu=p.MaPhieu
WHERE @MaPhieu IS NULL OR p.MaPhieu=@MaPhieu
GROUP BY p.MaPhieu,n.TenNCC,p.NgayNhap,p.TrangThai
ORDER BY p.MaPhieu DESC;

SELECT d.MaPhieu,d.LoaiHang,COALESCE(b.TenSanPham,n.TenNguyenLieu) AS MatHang,
       d.SoLo,d.NgaySanXuat,d.HanSuDung,d.SoLuong,d.DonViTinh,d.DonGia,d.ThanhTien
FROM dbo.ChiTietPhieuNhap d
LEFT JOIN dbo.SanPham b ON b.MaSanPham=d.MaSanPham
LEFT JOIN dbo.NguyenLieu n ON n.MaNguyenLieu=d.MaNguyenLieu
WHERE @MaPhieu IS NULL OR d.MaPhieu=@MaPhieu
ORDER BY d.MaPhieu DESC,d.MaDong;

-- Lượng nhập gốc được giữ nguyên khi kiểm kê. Lượng còn thay đổi theo nghiệp vụ.
SELECT N'Bánh' AS LoaiHang,l.MaLo,l.SoLo,b.TenSanPham AS MatHang,
       l.SoLuongNhap,l.SoLuongCon,l.HanSuDung,l.TinhTrang
FROM dbo.LoBanh l JOIN dbo.SanPham b ON b.MaSanPham=l.MaSanPham
WHERE @MaPhieu IS NULL OR EXISTS(SELECT 1 FROM dbo.BienDongKho v WHERE v.MaPhieu=@MaPhieu AND v.MaLoBanh=l.MaLo)
UNION ALL
SELECT N'Nguyên liệu',l.MaLo,l.SoLo,n.TenNguyenLieu,
       l.SoLuongNhap,l.SoLuongCon,l.HanSuDung,l.TinhTrang
FROM dbo.LoNguyenLieu l JOIN dbo.NguyenLieu n ON n.MaNguyenLieu=l.MaNguyenLieu
WHERE @MaPhieu IS NULL OR EXISTS(SELECT 1 FROM dbo.BienDongKho v WHERE v.MaPhieu=@MaPhieu AND v.MaLoNguyenLieu=l.MaLo);

-- Nhật ký có số cũ/số mới, tài khoản thực hiện và lý do.
-- Kiểm kê/xuất độc lập có MaPhieu NULL, xem toàn bộ khi @MaPhieu=NULL.
SELECT v.MaBienDong,v.ThoiDiem,v.MaPhieu,v.MaLoBanh,v.MaLoNguyenLieu,
       v.LoaiThaoTac,v.SoCu,v.SoMoi,t.HoTen AS NguoiThucHien,v.LyDo
FROM dbo.BienDongKho v JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=v.MaNguoiLam
WHERE @MaPhieu IS NULL OR v.MaPhieu=@MaPhieu
ORDER BY v.MaBienDong DESC;
