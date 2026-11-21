-- Select the intended database explicitly; adds no sample data.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.TaiKhoanKhachHang',N'U') IS NULL
    THROW 51000,N'Run migration 04_CustomerWebAccounts first.',1;
IF OBJECT_ID(N'dbo.GioHangKhachHang',N'U') IS NULL
CREATE TABLE dbo.GioHangKhachHang(
 MaKhachHang INT NOT NULL PRIMARY KEY REFERENCES dbo.TaiKhoanKhachHang(MaKhachHang),
 PhienBan BIGINT NOT NULL DEFAULT 0,
 CapNhatLuc DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
IF OBJECT_ID(N'dbo.ChiTietGioHang',N'U') IS NULL
CREATE TABLE dbo.ChiTietGioHang(
 MaKhachHang INT NOT NULL REFERENCES dbo.GioHangKhachHang(MaKhachHang),
 MaSanPham INT NOT NULL REFERENCES dbo.SanPham(MaSanPham),
 SoLuong INT NOT NULL CHECK(SoLuong BETWEEN 1 AND 1000),
 PRIMARY KEY(MaKhachHang,MaSanPham)
);
COMMIT;
