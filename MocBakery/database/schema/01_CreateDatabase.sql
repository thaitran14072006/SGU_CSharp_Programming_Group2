-- Source: BaoCao_QuanLyTiemBanh_Updated(3).docx, section 4.4.7.
-- Run on a NEW database in SSMS. Existing business tables are never dropped.
IF DB_ID(N'QuanLyTiemBanh') IS NULL
    CREATE DATABASE QuanLyTiemBanh COLLATE Vietnamese_CI_AS;
GO
USE QuanLyTiemBanh;
GO
SET XACT_ABORT ON;
IF EXISTS (SELECT 1 FROM sys.tables WHERE name IN
    (N'TaiKhoan',N'KhachHang',N'LoaiBanh',N'SanPham',N'LoBanh',N'DonDatBanh',N'ChiTietDonDat',N'HoaDon',N'ChiTietHoaDon'))
    THROW 50001, 'Business tables already exist. Use a new database; do not overwrite existing data.', 1;
BEGIN TRANSACTION;
CREATE TABLE dbo.TaiKhoan (
 MaTaiKhoan INT IDENTITY PRIMARY KEY,
 TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
 MatKhauHash NVARCHAR(255) NOT NULL,
 HoTen NVARCHAR(100) NOT NULL,
 VaiTro NVARCHAR(20) NOT NULL CHECK (VaiTro IN (N'ChuTiem',N'NhanVien')),
 TrangThai BIT NOT NULL DEFAULT 1,
 NgayTao DATETIME2(0) NOT NULL DEFAULT SYSDATETIME()
);
CREATE TABLE dbo.LoaiBanh (
 MaLoai INT IDENTITY PRIMARY KEY,
 TenLoai NVARCHAR(100) NOT NULL UNIQUE,
 MoTa NVARCHAR(255) NULL
);
CREATE TABLE dbo.SanPham (
 MaSanPham INT IDENTITY PRIMARY KEY,
 MaLoai INT NOT NULL REFERENCES dbo.LoaiBanh(MaLoai),
 TenSanPham NVARCHAR(150) NOT NULL UNIQUE,
 DonViTinh NVARCHAR(20) NOT NULL,
 GiaBan DECIMAL(18,2) NOT NULL CHECK (GiaBan >= 0),
 DangKinhDoanh BIT NOT NULL DEFAULT 1,
 MoTa NVARCHAR(500) NULL
);
CREATE TABLE dbo.LoBanh (
 MaLo INT IDENTITY PRIMARY KEY,
 MaSanPham INT NOT NULL REFERENCES dbo.SanPham(MaSanPham),
 NgayNhap DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
 NguonNhap NVARCHAR(100) NULL,
 NgaySanXuat DATE NOT NULL,
 HanSuDung DATE NOT NULL,
 SoLuongNhap INT NOT NULL CHECK (SoLuongNhap > 0),
 SoLuongCon INT NOT NULL CHECK (SoLuongCon >= 0),
 TinhTrang NVARCHAR(20) NOT NULL DEFAULT N'DangBan' CHECK (TinhTrang IN (N'DangBan',N'NgungBan')),
 CHECK (SoLuongCon <= SoLuongNhap),
 CHECK (HanSuDung >= NgaySanXuat)
);
CREATE TABLE dbo.KhachHang (
 MaKhachHang INT IDENTITY PRIMARY KEY,
 HoTen NVARCHAR(100) NOT NULL,
 SoDienThoai VARCHAR(15) NOT NULL UNIQUE,
 DiaChi NVARCHAR(255) NULL,
 GhiChu NVARCHAR(255) NULL
);
CREATE TABLE dbo.DonDatBanh (
 MaDon INT IDENTITY PRIMARY KEY,
 MaKhachHang INT NOT NULL REFERENCES dbo.KhachHang(MaKhachHang),
 MaTaiKhoan INT NOT NULL REFERENCES dbo.TaiKhoan(MaTaiKhoan),
 NgayDat DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
 NgayNhan DATETIME2(0) NOT NULL,
 YeuCau NVARCHAR(500) NULL,
 TongTien DECIMAL(18,2) NOT NULL CHECK (TongTien >= 0),
 TienCoc DECIMAL(18,2) NOT NULL DEFAULT 0,
 TrangThai NVARCHAR(20) NOT NULL DEFAULT N'MoiTiepNhan'
   CHECK (TrangThai IN (N'MoiTiepNhan',N'DangChuanBi',N'DaGiao',N'DaHuy')),
 LyDoHuy NVARCHAR(255) NULL,
 CHECK (TienCoc >= 0 AND TienCoc <= TongTien),
 CHECK (NgayNhan > NgayDat)
);
CREATE TABLE dbo.ChiTietDonDat (
 MaDon INT NOT NULL REFERENCES dbo.DonDatBanh(MaDon),
 MaSanPham INT NOT NULL REFERENCES dbo.SanPham(MaSanPham),
 SoLuong INT NOT NULL CHECK (SoLuong > 0),
 DonGia DECIMAL(18,2) NOT NULL CHECK (DonGia >= 0),
 ThanhTien AS (SoLuong * DonGia) PERSISTED,
 GhiChu NVARCHAR(255) NULL,
 PRIMARY KEY (MaDon,MaSanPham)
);
CREATE TABLE dbo.HoaDon (
 MaHoaDon INT IDENTITY PRIMARY KEY,
 NgayLap DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
 MaTaiKhoan INT NOT NULL REFERENCES dbo.TaiKhoan(MaTaiKhoan),
 MaKhachHang INT NULL REFERENCES dbo.KhachHang(MaKhachHang),
 MaDon INT NULL REFERENCES dbo.DonDatBanh(MaDon),
 TongTien DECIMAL(18,2) NOT NULL CHECK (TongTien >= 0),
 TienCocTru DECIMAL(18,2) NOT NULL DEFAULT 0,
 SoTienThu AS (TongTien - TienCocTru) PERSISTED,
 GhiChu NVARCHAR(255) NULL,
 CHECK (TienCocTru >= 0 AND TienCocTru <= TongTien)
);
CREATE TABLE dbo.ChiTietHoaDon (
 MaHoaDon INT NOT NULL REFERENCES dbo.HoaDon(MaHoaDon),
 MaLo INT NOT NULL REFERENCES dbo.LoBanh(MaLo),
 SoLuong INT NOT NULL CHECK (SoLuong > 0),
 DonGia DECIMAL(18,2) NOT NULL CHECK (DonGia >= 0),
 ThanhTien AS (SoLuong * DonGia) PERSISTED,
 PRIMARY KEY (MaHoaDon,MaLo)
);
CREATE INDEX IX_SanPham_MaLoai ON dbo.SanPham(MaLoai);
CREATE INDEX IX_LoBanh_SanPham_HanSuDung ON dbo.LoBanh(MaSanPham,HanSuDung) INCLUDE(SoLuongCon,TinhTrang);
CREATE INDEX IX_HoaDon_NgayLap ON dbo.HoaDon(NgayLap);
CREATE INDEX IX_HoaDon_MaKhachHang ON dbo.HoaDon(MaKhachHang);
CREATE UNIQUE INDEX UX_HoaDon_MaDon ON dbo.HoaDon(MaDon) WHERE MaDon IS NOT NULL;
CREATE INDEX IX_DonDatBanh_TrangThai_NgayNhan ON dbo.DonDatBanh(TrangThai,NgayNhan);
CREATE INDEX IX_DonDatBanh_MaKhachHang ON dbo.DonDatBanh(MaKhachHang);
CREATE INDEX IX_ChiTietHoaDon_MaLo ON dbo.ChiTietHoaDon(MaLo);
COMMIT;
GO
