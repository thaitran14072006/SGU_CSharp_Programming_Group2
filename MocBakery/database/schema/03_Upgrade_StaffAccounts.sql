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
