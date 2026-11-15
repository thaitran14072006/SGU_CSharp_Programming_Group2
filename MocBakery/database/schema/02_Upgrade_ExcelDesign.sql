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
