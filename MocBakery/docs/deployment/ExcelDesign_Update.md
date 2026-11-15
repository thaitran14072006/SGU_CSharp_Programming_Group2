# Hướng dẫn bản Catalog và Inventory theo thiết kế Excel

**Bản chốt:** đã tích hợp main cùng các module nhóm, CSDL 16 bảng. Bản 15 bảng cần migration 03.
Xem `CHOT_DO_AN.md` để triển khai hiện hành; mô tả chưa gộp nhóm bên dưới là lịch sử bản nhánh.

Chuẩn đối chiếu là `Thiet_ke_giao_dien_quan_ly_tiem_banh.xlsx`, bản 1.0 ngày 25/10/2026 do người dùng yêu cầu áp dụng.
Phạm vi phần Như là các sheet 05, 06, 07, 12 và 13; bổ sung Nhà cung cấp và danh sách nguyên liệu để lập phiếu nhập hoạt động.
Bản này thay thế cách nhập lô đơn giản và các trang giao diện của bản trước. Không gộp Sales/Orders từ nhánh nhóm trong thay đổi này.

## Mở bản thử trên máy hiện tại

Bản mới ở `MocBakery/artifacts/publish/MocBakery.GUI.exe`, cấu hình trỏ đến CSDL LocalDB demo mới, tách khỏi các CSDL cũ.
Tại Đăng nhập chọn **Thiết lập lần đầu** để tự tạo chủ tiệm, sau đó đăng nhập. Không có mật khẩu mặc định.
Dữ liệu Croissant, Tiramisu, bánh kem, cookie, muffin và nguyên liệu là số dư đầu kỳ minh họa, không phải chứng từ thật.
Binary và cấu hình local không được push lên Git; người nhận nhánh cần build/publish theo hướng dẫn dưới đây.

## Cài mới hoặc nâng cấp CSDL

Với CSDL mới, chạy `database/schema/01_CreateDatabase.sql`. Script đã bao gồm cấu trúc mở rộng theo Excel.
Tùy chọn chạy `database/seeds/04_ExcelDesign_Demo.sql` trên CSDL thử để có dữ liệu theo mẫu; chạy lại không reset tồn cũ.

Với CSDL đang có 9 bảng của bản trước, sao lưu rồi chọn đúng CSDL đó và chạy **chỉ** `database/schema/02_Upgrade_ExcelDesign.sql`.
Không chạy lại 01 trên CSDL đang dùng. Migration có thể chạy lại, không xóa bảng hoặc dữ liệu nghiệp vụ.
Sau nâng cấp có 15 bảng: giữ 9 bảng cũ, thêm NhaCungCap, NguyenLieu, LoNguyenLieu, PhieuNhap, ChiTietPhieuNhap và BienDongKho.
Mã sản phẩm/lô/khách hàng/hóa đơn cũ và các khóa liên quan được giữ nguyên.

Danh mục được thêm trạng thái đang dùng; sản phẩm thêm hạn dùng mặc định, ảnh và ngưỡng tồn.
Không tự cắt tên sản phẩm cũ hoặc đổi giá cũ. Khi sửa thông tin bằng bản mới, cần đáp ứng tên tối đa 100 ký tự và giá lớn hơn 0.
Ràng buộc lượng còn không vượt lượng nhập được bỏ để nghiệp vụ kiểm kê có thể ghi nhận tồn thực tế tăng;
số lượng nhập gốc vẫn giữ nguyên, thay đổi lượng còn được ghi nhật ký riêng.

## Sử dụng các màn hình

**Quản lý bánh:** tìm theo mã B001 hoặc tên; lọc danh mục và trạng thái. Bảng có giá và tồn có thể bán.
Thêm/Sửa mở trang Chi tiết bánh hai cột. Ngừng/Bán lại cần xác nhận và không xóa lịch sử.

**Chi tiết bánh:** mã tự sinh, tên 1–100 ký tự, danh mục đang dùng, giá > 0, đơn vị Cái/Hộp/Ổ,
hạn dùng mặc định 1–3650 ngày, trạng thái và ảnh JPG/PNG tối đa 5 MB.
Hạn dùng mặc định gợi ý ngày hết hạn khi thêm dòng nhập; từng lô vẫn có ngày thực tế riêng.
Ngưỡng sắp hết có thể cấu hình. Bỏ thay đổi chưa lưu phải xác nhận.

**Danh mục bánh:** thêm/sửa tên và mô tả, tên không trùng, hiển thị số loại bánh và trạng thái.
Ngừng sử dụng không xóa sản phẩm; danh mục ngừng không xuất hiện khi tạo bánh mới.
Bánh cũ vẫn giữ liên kết danh mục; có thể sửa bánh mà không buộc đổi danh mục lịch sử của nó.

**Tồn kho và hạn dùng:** chọn Bánh thành phẩm hoặc Nguyên liệu; tìm theo tên hàng/mã lô.
Bộ lọc có Sắp hết, Sắp hết hạn, Đã hết hạn và Ngừng dùng.
Sắp hết là tổng tồn hợp lệ của mặt hàng thấp hơn ngưỡng đã đặt; sắp hết hạn là còn lượng và còn tối đa 3 ngày, gồm ngày hôm nay.
Hạn dùng có hiệu lực hết ngày ghi trên lô. Bánh hết hạn, lô ngừng bán hoặc sản phẩm ngừng kinh doanh không được tính vào tồn bán.

**Xuất/Điều chỉnh:** chọn một lô/mặt hàng. Xuất hàng tự phân bổ theo FEFO cho mặt hàng, không vượt tồn.
Lý do Sản xuất chỉ dùng lô còn hạn đang sử dụng; Hủy có thể dùng lô hết hạn và ưu tiên hạn sớm.
Kiểm kê áp dụng riêng lô đã chọn, nhập lượng thực tế (có thể bằng 0), chọn Kiểm kê và ghi diễn giải.
Lịch sử Chi tiết lô có số cũ, số mới, người làm, thời điểm và lý do. Không tự trừ nguyên liệu khi bán bánh vì chưa có định mức.

**Phiếu nhập kho:** lập phiếu, chọn nhà cung cấp còn hợp tác, ngày nhập không sau hiện tại.
Thêm nhiều dòng bánh/nguyên liệu, số lô, NSX, HSD, số lượng và đơn giá.
Điều kiện NSX ≤ ngày nhập ≤ HSD; bánh dùng lượng nguyên, nguyên liệu tối đa 3 chữ số lẻ; đơn giá không âm và tối đa 2 số lẻ.
Đơn vị lấy từ mặt hàng để tránh cộng sai đơn vị. Mỗi dòng tính tiền và làm tròn 2 số lẻ, tổng phiếu bằng tổng các dòng.
**Lưu nháp** chưa cộng tồn. **Ghi sổ phiếu** cần xác nhận, tạo lô/tăng tồn đúng một lần và khóa sửa dòng.
Nếu lỗi ở bất kỳ dòng nào, toàn bộ lần ghi sổ rollback; phiếu vẫn là nháp. Phiếu được hai người sửa sẽ kiểm tra phiên bản để tránh ghi đè dữ liệu mới.

**Nhà cung cấp/Nguyên liệu:** quản lý thông tin và trạng thái dùng cho phiếu mới; giữ dữ liệu đã liên kết lịch sử.
Nhà cung cấp ngừng hợp tác không chọn cho phiếu mới và bị kiểm tra lại khi ghi sổ.

## Phân quyền và phối hợp nhóm

Ba vai trò hiển thị là Quản lý (giá trị cũ ChuTiem), Thu ngân và Nhân viên kho.
Quản lý sửa Catalog; Quản lý/Kho thay đổi Inventory. Thu ngân tra cứu, không được ghi kho hoặc sửa danh mục/sản phẩm.
Vai trò NhanVien cũ vẫn đọc được để tương thích dữ liệu, chỉ xuất hiện khi sửa tài khoản cũ; tài khoản mới dùng ba lựa chọn trên.
Menu tài khoản cá nhân ở góc dưới trái chứa Đổi mật khẩu, Đăng xuất và mục quản lý tài khoản cho quản lý.

Khi ghép Sales của Mai, giữ truy vấn lô hợp lệ `LoBanhRepository.GetSellableLots(connection, transaction, ...)`;
cột MaLo/MaSanPham và quan hệ hóa đơn không đổi. Bổ sung ThuNgan vào quyền bán hàng của module Sales khi tích hợp.
Không dùng GUI nhập lô cũ để thay cho phiếu nháp/ghi sổ. API Receive cũ còn được giữ cho tương thích và kiểm thử hồi quy, không có đường điều hướng từ GUI mới.
Các mục Tổng quan, Sales/Hóa đơn, Orders, Nhân viên độc lập và Reports vẫn do thành viên tương ứng tích hợp.

## Build và kiểm tra

```powershell
dotnet build .\MocBakery\MocBakery.sln -c Release
dotnet .\MocBakery\tests\Unit\bin\Release\net10.0\MocBakery.UnitChecks.dll
dotnet .\MocBakery\tests\Unit\CatalogInventory\bin\Release\net10.0\MocBakery.CatalogInventoryChecks.dll
dotnet .\MocBakery\tests\Unit\CatalogInventory\bin\Release\net10.0\MocBakery.CatalogInventoryChecks.dll --integration
dotnet .\MocBakery\tests\Unit\CatalogInventory\bin\Release\net10.0\MocBakery.CatalogInventoryChecks.dll --excel
.\MocBakery\tests\System\PrepareGuiFixture.ps1 -NewFixture
dotnet .\MocBakery\tests\System\bin\Release\net10.0-windows\MocBakery.GuiChecks.dll --verify-design --owner
dotnet publish .\MocBakery\src\MocBakery.GUI\MocBakery.GUI.csproj -c Release --self-contained false -o .\MocBakery\artifacts\publish
```

Ca SQL dùng database tạm và tự dọn. Test form dùng fixture có tiền tố NhuGuiTest; không chạy trên dữ liệu thật.
Test form phát sự kiện trên WinForms với message loop và SQL thật, thay câu trả lời xác nhận bằng callback của bộ kiểm tra;
đây không phải một lần nhập bằng chuột/bàn phím Windows của người dùng.
Máy chạy publish cần .NET Desktop Runtime 10 và cấu hình `appsettings.local.json` riêng theo mẫu.
