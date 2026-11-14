-- KT06: thu ban 6 banh bang hai lo mau trong bang tam.
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @NgayBan date = '20261115';
DECLARE @MaSanPham int = 1;
DECLARE @SoLuongBan int = 6;

DROP TABLE IF EXISTS #KT06_PhanBo;
DROP TABLE IF EXISTS #KT06_LoBanh;

CREATE TABLE #KT06_LoBanh (
    MaLo int PRIMARY KEY,
    MaSanPham int NOT NULL,
    HanSuDung date NOT NULL,
    SoLuongTon int NOT NULL CHECK (SoLuongTon >= 0),
    DuocPhepBan bit NOT NULL
);

INSERT INTO #KT06_LoBanh (MaLo, MaSanPham, HanSuDung, SoLuongTon, DuocPhepBan)
VALUES (101, @MaSanPham, DATEADD(day, 1, @NgayBan), 4, 1),
       (102, @MaSanPham, DATEADD(day, 3, @NgayBan), 5, 1);

SELECT MaLo, HanSuDung, SoLuongTon AS TonTruoc
FROM #KT06_LoBanh
ORDER BY HanSuDung, MaLo;

-- Quy uoc rieng cho demo: lo van ban duoc trong ngay het han.
WITH LoHopLe AS (
    SELECT MaLo, SoLuongTon,
           SUM(SoLuongTon) OVER (ORDER BY HanSuDung, MaLo ROWS UNBOUNDED PRECEDING)
               - SoLuongTon AS TonCacLoTruoc
    FROM #KT06_LoBanh
    WHERE MaSanPham = @MaSanPham AND DuocPhepBan = 1
      AND HanSuDung >= @NgayBan AND SoLuongTon > 0
)
SELECT MaLo,
       CASE WHEN @SoLuongBan - TonCacLoTruoc >= SoLuongTon THEN SoLuongTon
            ELSE @SoLuongBan - TonCacLoTruoc END AS SoLuongLay
INTO #KT06_PhanBo
FROM LoHopLe
WHERE TonCacLoTruoc < @SoLuongBan;

IF COALESCE((SELECT SUM(SoLuongLay) FROM #KT06_PhanBo), 0) <> @SoLuongBan
    THROW 50001, 'Khong du ton hop le.', 1;

UPDATE lo
SET SoLuongTon = lo.SoLuongTon - pb.SoLuongLay
FROM #KT06_LoBanh AS lo
JOIN #KT06_PhanBo AS pb ON pb.MaLo = lo.MaLo;

SELECT lo.MaLo, lo.HanSuDung, pb.SoLuongLay, lo.SoLuongTon AS TonSau
FROM #KT06_LoBanh AS lo
JOIN #KT06_PhanBo AS pb ON pb.MaLo = lo.MaLo
ORDER BY lo.HanSuDung, lo.MaLo;

IF NOT EXISTS (SELECT 1 FROM #KT06_PhanBo WHERE MaLo = 101 AND SoLuongLay = 4)
   OR NOT EXISTS (SELECT 1 FROM #KT06_PhanBo WHERE MaLo = 102 AND SoLuongLay = 2)
   OR NOT EXISTS (SELECT 1 FROM #KT06_LoBanh WHERE MaLo = 101 AND SoLuongTon = 0)
   OR NOT EXISTS (SELECT 1 FROM #KT06_LoBanh WHERE MaLo = 102 AND SoLuongTon = 3)
    THROW 50002, 'KT06 demo: sai phan bo hoac ton sau ban.', 1;

SELECT 'KT06 demo: PASS' AS KetQua;

DROP TABLE #KT06_PhanBo;
DROP TABLE #KT06_LoBanh;
