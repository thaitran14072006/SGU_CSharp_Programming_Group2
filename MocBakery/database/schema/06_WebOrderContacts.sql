-- Select the shared TEST database explicitly, after migration 05.
-- Old orders remain unchanged. New Web submissions require contact data in BLL.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.DonDatBanh','WebRequestKey') IS NULL
    THROW 51000, N'Apply migration 05 before adding Web order contacts.', 1;
IF COL_LENGTH('dbo.DonDatBanh','NguoiNhan') IS NULL
    ALTER TABLE dbo.DonDatBanh ADD NguoiNhan NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.DonDatBanh','DienThoaiNhan') IS NULL
    ALTER TABLE dbo.DonDatBanh ADD DienThoaiNhan VARCHAR(10) NULL;
IF COL_LENGTH('dbo.DonDatBanh','EmailNhan') IS NULL
    ALTER TABLE dbo.DonDatBanh ADD EmailNhan NVARCHAR(254) NULL;
COMMIT;
