-- Run in the same TEST database shown in application configuration.
DECLARE @date DATE=CAST(GETDATE() AS DATE);
SELECT p.MaSanPham,p.TenSanPham,p.DangKinhDoanh,
    COALESCE(SUM(CAST(l.SoLuongCon AS BIGINT)),0) AS TongTon,
    COALESCE(SUM(CASE WHEN p.DangKinhDoanh=1 AND l.TinhTrang=N'DangBan'
        AND l.SoLuongCon>0 AND l.NgaySanXuat<=@date AND l.HanSuDung>=@date
        THEN CAST(l.SoLuongCon AS BIGINT) ELSE 0 END),0) AS TonCoTheBan
FROM dbo.SanPham p LEFT JOIN dbo.LoBanh l ON l.MaSanPham=p.MaSanPham
GROUP BY p.MaSanPham,p.TenSanPham,p.DangKinhDoanh ORDER BY p.MaSanPham;
SELECT l.*,p.TenSanPham FROM dbo.LoBanh l JOIN dbo.SanPham p ON p.MaSanPham=l.MaSanPham
ORDER BY l.MaSanPham,l.HanSuDung,l.MaLo;
