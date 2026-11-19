-- Select the shared TEST database explicitly. Back up before upgrading existing data.
-- Existing desktop orders retain their employee and all history.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.DonDatBanh', N'U') IS NULL OR OBJECT_ID(N'dbo.TaiKhoanKhachHang', N'U') IS NULL
    THROW 51000, N'Initialize schema 01 and customer accounts 04 first.', 1;
IF COL_LENGTH('dbo.DonDatBanh', 'WebRequestKey') IS NULL
    ALTER TABLE dbo.DonDatBanh ADD WebRequestKey UNIQUEIDENTIFIER NULL;
IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.DonDatBanh') AND name=N'MaTaiKhoan' AND is_nullable=0)
    ALTER TABLE dbo.DonDatBanh ALTER COLUMN MaTaiKhoan INT NULL;
-- Dynamic SQL allows the new column to be compiled after it is added, including reruns.
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.DonDatBanh') AND name=N'UX_DonDatBanh_WebRequest')
    EXEC(N'CREATE UNIQUE INDEX UX_DonDatBanh_WebRequest ON dbo.DonDatBanh(MaKhachHang,WebRequestKey) WHERE WebRequestKey IS NOT NULL');
IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.DonDatBanh') AND name=N'CK_DonDatBanh_Creator')
    EXEC(N'ALTER TABLE dbo.DonDatBanh ADD CONSTRAINT CK_DonDatBanh_Creator CHECK(MaTaiKhoan IS NOT NULL OR WebRequestKey IS NOT NULL)');
COMMIT;
