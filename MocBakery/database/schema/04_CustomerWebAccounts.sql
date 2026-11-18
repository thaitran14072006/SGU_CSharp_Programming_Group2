-- Run against the selected shared TEST database after schema 01 (or upgrades 02 and 03).
-- No USE statement: the caller chooses the database explicitly. No customer accounts are seeded.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.KhachHang', N'U') IS NULL
    THROW 51000, N'Missing dbo.KhachHang. Initialize the shared schema first.', 1;

IF OBJECT_ID(N'dbo.TaiKhoanKhachHang', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaiKhoanKhachHang (
        MaTaiKhoanKhach INT IDENTITY PRIMARY KEY,
        MaKhachHang INT NOT NULL,
        TenDangNhap NVARCHAR(50) COLLATE Latin1_General_100_CI_AS NOT NULL,
        MatKhauHash NVARCHAR(255) NOT NULL,
        TrangThai BIT NOT NULL CONSTRAINT DF_TaiKhoanKhach_TrangThai DEFAULT 1,
        SecurityStamp UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_TaiKhoanKhach_Stamp DEFAULT NEWID(),
        NgayTao DATETIME2(0) NOT NULL CONSTRAINT DF_TaiKhoanKhach_NgayTao DEFAULT SYSDATETIME(),
        CONSTRAINT FK_TaiKhoanKhach_KhachHang FOREIGN KEY(MaKhachHang) REFERENCES dbo.KhachHang(MaKhachHang),
        CONSTRAINT UQ_TaiKhoanKhach_KhachHang UNIQUE(MaKhachHang),
        CONSTRAINT UQ_TaiKhoanKhach_Username UNIQUE(TenDangNhap)
    );
END;
COMMIT;
