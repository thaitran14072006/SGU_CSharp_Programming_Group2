-- Chọn đúng database trước khi chạy. Chỉ thêm thực đơn, KHÔNG tạo tồn hoặc phiếu nhập.
-- Giá là giá gợi ý cho đồ án; các sản phẩm đã có được giữ nguyên.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @category INT;
    SELECT @category=MaLoai FROM dbo.LoaiBanh WITH(UPDLOCK,HOLDLOCK) WHERE TenLoai=N'Bánh Pháp';
    IF @category IS NULL
    BEGIN
        INSERT dbo.LoaiBanh(TenLoai,MoTa,DangSuDung)
        VALUES(N'Bánh Pháp',N'Những chiếc bánh nhỏ tinh tế, hương bơ, hạnh nhân và chocolate.',1);
        SET @category=SCOPE_IDENTITY();
    END;
    IF NOT EXISTS(SELECT 1 FROM dbo.LoaiBanh WHERE MaLoai=@category AND DangSuDung=1)
        THROW 51000,N'Danh mục Bánh Pháp đang ngừng sử dụng. Không tự thay đổi trạng thái danh mục.',1;
    DECLARE @items TABLE(Ten NVARCHAR(100),DonVi NVARCHAR(20),Gia DECIMAL(18,2),MoTa NVARCHAR(500));
    INSERT @items VALUES
    (N'Canelé vani',N'Cái',45000,N'Lớp vỏ caramel nâu óng, ruột mềm ẩm và hương vani dịu. Một chiếc bánh nhỏ để thưởng thức cùng cà phê.'),
    (N'Madeleine chanh vàng',N'Cái',30000,N'Bánh bơ hình vỏ sò, cốt mềm nhẹ và hương chanh vàng thanh. Phù hợp dùng cùng trà chiều.'),
    (N'Financier hạnh nhân',N'Cái',38000,N'Bánh hạnh nhân dáng chữ nhật, viền vàng nhẹ và vị bơ thơm. Điểm thêm hạnh nhân lát trên mặt.'),
    (N'Macaron hộp 6 chiếc',N'Hộp',180000,N'Hộp sáu chiếc macaron với vỏ hạnh nhân mỏng và nhân kem. Phối các vị vani, chocolate, hạt dẻ cười và hoa hồng.'),
    (N'Éclair chocolate',N'Cái',55000,N'Bánh su dài với nhân kem chocolate, phủ lớp chocolate bóng mịn. Vị cacao hài hòa trong phần bánh mềm nhẹ.');
    DECLARE @added TABLE(Id INT,Name NVARCHAR(100));
    INSERT dbo.SanPham(MaLoai,TenSanPham,DonViTinh,GiaBan,DangKinhDoanh,MoTa,HanDungNgay,NguongCanhBao)
    OUTPUT INSERTED.MaSanPham,INSERTED.TenSanPham INTO @added
    SELECT @category,i.Ten,i.DonVi,i.Gia,1,i.MoTa,1,5 FROM @items i
    WHERE NOT EXISTS(SELECT 1 FROM dbo.SanPham p WITH(UPDLOCK,HOLDLOCK)
                     WHERE p.TenSanPham COLLATE Latin1_General_100_CI_AI=i.Ten COLLATE Latin1_General_100_CI_AI);
    COMMIT;
    SELECT Id,Name FROM @added ORDER BY Id;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
