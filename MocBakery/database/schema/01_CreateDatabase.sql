-- Source: BaoCao_QuanLyTiemBanh_Updated(3).docx, section 4.4.7.
-- Run on a NEW database in SSMS. Existing business tables are never dropped.
IF DB_ID(N'QuanLyTiemBanh') IS NULL
    CREATE DATABASE QuanLyTiemBanh COLLATE Vietnamese_CI_AS;
GO
USE QuanLyTiemBanh;
GO
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
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

-- Excel design extensions for new installations
-- Run in the existing bakery database after backing it up. Safe to rerun; no data is deleted.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.LoaiBanh','DangSuDung') IS NULL
    ALTER TABLE dbo.LoaiBanh ADD DangSuDung BIT NOT NULL CONSTRAINT DF_LoaiBanh_Active DEFAULT 1;
IF COL_LENGTH('dbo.SanPham','HanDungNgay') IS NULL
    ALTER TABLE dbo.SanPham ADD HanDungNgay INT NOT NULL CONSTRAINT DF_SP_Shelf DEFAULT 1 CHECK(HanDungNgay>0),
        AnhBanh VARBINARY(MAX) NULL, NguongCanhBao DECIMAL(18,3) NOT NULL CONSTRAINT DF_SP_Low DEFAULT 5 CHECK(NguongCanhBao>=0);
IF COL_LENGTH('dbo.LoBanh','SoLo') IS NULL
    ALTER TABLE dbo.LoBanh ADD SoLo NVARCHAR(50) NULL;
DECLARE @balanceConstraint SYSNAME;
SELECT @balanceConstraint=name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.LoBanh')
    AND definition LIKE '%SoLuongCon%' AND definition LIKE '%SoLuongNhap%';
DECLARE @dropSql NVARCHAR(500);
IF @balanceConstraint IS NOT NULL BEGIN SET @dropSql=N'ALTER TABLE dbo.LoBanh DROP CONSTRAINT '+QUOTENAME(@balanceConstraint);EXEC sp_executesql @dropSql;END;
DECLARE @constraint SYSNAME;
SELECT @constraint=name FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.TaiKhoan') AND definition LIKE '%VaiTro%';
IF @constraint IS NOT NULL BEGIN SET @dropSql=N'ALTER TABLE dbo.TaiKhoan DROP CONSTRAINT '+QUOTENAME(@constraint);EXEC sp_executesql @dropSql;END;
ALTER TABLE dbo.TaiKhoan ADD CONSTRAINT CK_TaiKhoan_Role CHECK(VaiTro IN(N'ChuTiem',N'ThuNgan',N'Kho',N'NhanVien'));
IF OBJECT_ID('dbo.NhaCungCap') IS NULL
CREATE TABLE dbo.NhaCungCap(
 MaNCC INT IDENTITY PRIMARY KEY, TenNCC NVARCHAR(150) NOT NULL UNIQUE,
 DienThoai VARCHAR(11) NOT NULL, DiaChi NVARCHAR(255) NULL, Email NVARCHAR(255) NULL, DangHopTac BIT NOT NULL DEFAULT 1
);
IF OBJECT_ID('dbo.NguyenLieu') IS NULL
CREATE TABLE dbo.NguyenLieu(
 MaNguyenLieu INT IDENTITY PRIMARY KEY, TenNguyenLieu NVARCHAR(150) NOT NULL UNIQUE,
 DonViTinh NVARCHAR(20) NOT NULL, DangSuDung BIT NOT NULL DEFAULT 1,
 NguongCanhBao DECIMAL(18,3) NOT NULL DEFAULT 5 CHECK(NguongCanhBao>=0)
);
IF OBJECT_ID('dbo.LoNguyenLieu') IS NULL
CREATE TABLE dbo.LoNguyenLieu(
 MaLo INT IDENTITY PRIMARY KEY, MaNguyenLieu INT NOT NULL REFERENCES dbo.NguyenLieu,
 SoLo NVARCHAR(50) NOT NULL, NgayNhap DATETIME2(0) NOT NULL, NgaySanXuat DATE NOT NULL, HanSuDung DATE NOT NULL,
 SoLuongNhap DECIMAL(18,3) NOT NULL CHECK(SoLuongNhap>0), SoLuongCon DECIMAL(18,3) NOT NULL CHECK(SoLuongCon>=0),
 TinhTrang NVARCHAR(20) NOT NULL DEFAULT N'DangBan' CHECK(TinhTrang IN(N'DangBan',N'NgungBan')),
 UNIQUE(MaNguyenLieu,SoLo),CHECK(HanSuDung>=NgaySanXuat)
);
IF OBJECT_ID('dbo.PhieuNhap') IS NULL
CREATE TABLE dbo.PhieuNhap(
 MaPhieu INT IDENTITY PRIMARY KEY, MaNCC INT NOT NULL REFERENCES dbo.NhaCungCap,
 NgayNhap DATETIME2(0) NOT NULL, MaNguoiLap INT NOT NULL REFERENCES dbo.TaiKhoan,
 TrangThai NVARCHAR(20) NOT NULL DEFAULT N'Nhap' CHECK(TrangThai IN(N'Nhap',N'DaGhiSo')),
 MaNguoiGhi INT NULL REFERENCES dbo.TaiKhoan, NgayGhi DATETIME2(0) NULL, PhienBan ROWVERSION
);
IF OBJECT_ID('dbo.ChiTietPhieuNhap') IS NULL
CREATE TABLE dbo.ChiTietPhieuNhap(
 MaDong INT IDENTITY PRIMARY KEY, MaPhieu INT NOT NULL REFERENCES dbo.PhieuNhap,
 LoaiHang NVARCHAR(20) NOT NULL CHECK(LoaiHang IN(N'Banh',N'NguyenLieu')),
 MaSanPham INT NULL REFERENCES dbo.SanPham, MaNguyenLieu INT NULL REFERENCES dbo.NguyenLieu,
 SoLo NVARCHAR(50) NOT NULL, NgaySanXuat DATE NOT NULL, HanSuDung DATE NOT NULL,
 SoLuong DECIMAL(18,3) NOT NULL CHECK(SoLuong>0), DonViTinh NVARCHAR(20) NOT NULL,
 DonGia DECIMAL(18,2) NOT NULL CHECK(DonGia>=0),
 ThanhTien AS CAST(ROUND(SoLuong*DonGia,2) AS DECIMAL(18,2)) PERSISTED,
 CHECK((LoaiHang=N'Banh' AND MaSanPham IS NOT NULL AND MaNguyenLieu IS NULL AND SoLuong=FLOOR(SoLuong)) OR
       (LoaiHang=N'NguyenLieu' AND MaSanPham IS NULL AND MaNguyenLieu IS NOT NULL)),
 CHECK(HanSuDung>=NgaySanXuat)
);
IF OBJECT_ID('dbo.BienDongKho') IS NULL
CREATE TABLE dbo.BienDongKho(
 MaBienDong INT IDENTITY PRIMARY KEY, MaLoBanh INT NULL REFERENCES dbo.LoBanh,
 MaLoNguyenLieu INT NULL REFERENCES dbo.LoNguyenLieu, MaPhieu INT NULL REFERENCES dbo.PhieuNhap,
 LoaiThaoTac NVARCHAR(20) NOT NULL CHECK(LoaiThaoTac IN(N'Nhap',N'Xuat',N'KiemKe')),
 SoCu DECIMAL(18,3) NOT NULL CHECK(SoCu>=0), SoMoi DECIMAL(18,3) NOT NULL CHECK(SoMoi>=0),
 MaNguoiLam INT NOT NULL REFERENCES dbo.TaiKhoan, LyDo NVARCHAR(500) NOT NULL,
 ThoiDiem DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
 CHECK((MaLoBanh IS NULL AND MaLoNguyenLieu IS NOT NULL) OR(MaLoBanh IS NOT NULL AND MaLoNguyenLieu IS NULL))
);
COMMIT;
GO

GO
-- Run on the selected existing database after migration 02. Preserves all existing data.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.TaiKhoan','BatBuocDoiMatKhau') IS NULL
    ALTER TABLE dbo.TaiKhoan ADD BatBuocDoiMatKhau BIT NOT NULL CONSTRAINT DF_TaiKhoan_DoiMatKhau DEFAULT 0;
IF OBJECT_ID('dbo.NhanVien','U') IS NULL
BEGIN
    CREATE TABLE dbo.NhanVien (
        MaNhanVien INT IDENTITY PRIMARY KEY,
        HoTen NVARCHAR(100) NOT NULL,
        SoDienThoai VARCHAR(10) NULL,
        ChucVu NVARCHAR(20) NOT NULL CHECK (ChucVu IN(N'ChuTiem',N'ThuNgan',N'Kho',N'NhanVien')),
        NgayVaoLam DATE NOT NULL,
        DangLam BIT NOT NULL DEFAULT 1,
        MaTaiKhoan INT NULL REFERENCES dbo.TaiKhoan(MaTaiKhoan),
        CONSTRAINT CK_NhanVien_Phone CHECK (SoDienThoai IS NULL OR (LEN(SoDienThoai)=10 AND SoDienThoai NOT LIKE '%[^0-9]%' COLLATE Latin1_General_100_BIN2))
    );
    CREATE UNIQUE INDEX UX_NhanVien_Phone ON dbo.NhanVien(SoDienThoai) WHERE SoDienThoai IS NOT NULL;
    CREATE UNIQUE INDEX UX_NhanVien_Account ON dbo.NhanVien(MaTaiKhoan) WHERE MaTaiKhoan IS NOT NULL;
END;
-- Legacy account records receive a linked profile; phone remains unknown until supplied.
INSERT dbo.NhanVien(HoTen,ChucVu,NgayVaoLam,DangLam,MaTaiKhoan)
SELECT a.HoTen,a.VaiTro,CONVERT(date,a.NgayTao),1,a.MaTaiKhoan FROM dbo.TaiKhoan a
WHERE NOT EXISTS(SELECT 1 FROM dbo.NhanVien n WHERE n.MaTaiKhoan=a.MaTaiKhoan);
COMMIT;
