# Bàn giao Catalog và Inventory

Như hiện thực Model, Repository, Service, giao diện và kiểm thử cho `LoaiBanh`, `SanPham`, `LoBanh`.
Không bổ sung bảng, không thay đổi khóa và không sửa nghiệp vụ Accounts/Customers.
Schema bổ sung các thiết lập SET để chạy nhất quán qua SqlClient và sqlcmd, gồm QUOTED_IDENTIFIER cho filtered index.

## Điểm tích hợp

`AppServices.Catalog` và `AppServices.Inventory` dùng cùng `AuthService` của Thái.
Mọi thao tác ghi yêu cầu chủ tiệm; tra cứu yêu cầu phiên đăng nhập còn hợp lệ.
BLL đọc lại quyền hiện tại, không tin vai trò do GUI truyền vào. GUI dùng ErrorProvider và `Ui.Run` dùng chung.
Các menu Loại bánh, Sản phẩm, Kho bánh đã nối vào MainForm.

`CatalogService` cung cấp `Categories`, `Products`, `SaveCategory`, `DeleteCategory`, `SaveProduct`.
Sản phẩm luôn chuyển trạng thái thay vì xóa; lô không cho cập nhật trực tiếp lượng còn từ giao diện.

## Hợp đồng với Sales của Mai

`InventoryService.SellableLots(session, productId)` lấy ngày hiện tại của máy ứng dụng và kiểm tra phiên.
`LoBanhRepository.GetSellableLots(productId, businessDate)` trả về lô hợp lệ theo `HanSuDung, MaLo` tăng dần.
Đối với luồng bán/giao đơn, dùng overload:

```csharp
repository.GetSellableLots(connection, transaction, productId, businessDate);
```

Overload dùng đúng connection/transaction của Sales hoặc Orders, không tự commit, không trừ tồn và không giữ chỗ.
Mai phải kiểm tra lại trạng thái sản phẩm, ngày hạn, trạng thái lô và lượng còn khi ghi;
dùng UPDATE có điều kiện và kiểm tra số dòng ảnh hưởng. Việc đọc lô trước đó không bảo đảm tồn vẫn còn khi thanh toán.
Các dòng hóa đơn cần gắn đúng MaLo; lấy nhiều lô thì tạo nhiều dòng tương ứng.
Không triển khai lặp một bộ quy tắc ngày khác trong Sales. Hạn dùng bao gồm toàn bộ ngày ghi trên lô.
Chốt một múi giờ/ngày nghiệp vụ cho máy ứng dụng và SQL Server; hiện dùng ngày local, chưa có cấu hình múi giờ tập trung.

## Chạy kiểm tra và đóng gói

Từ gốc repo:

```powershell
dotnet build .\MocBakery\MocBakery.sln
dotnet .\MocBakery\tests\Unit\bin\Debug\net10.0\MocBakery.UnitChecks.dll
dotnet .\MocBakery\tests\Unit\CatalogInventory\bin\Debug\net10.0\MocBakery.CatalogInventoryChecks.dll
dotnet .\MocBakery\tests\Unit\CatalogInventory\bin\Debug\net10.0\MocBakery.CatalogInventoryChecks.dll --integration
dotnet publish .\MocBakery\src\MocBakery.GUI\MocBakery.GUI.csproj -c Release --self-contained false -o .\MocBakery\artifacts\publish
```

Đây là các console kiểm tra, không chạy bằng `dotnet test`. Bộ tích hợp mặc định cần SQL LocalDB,
có thể chọn SQL Server thử bằng biến `MOCBAKERY_TEST_CONNECTION`. Bộ kiểm tra luôn tạo CSDL mới,
không tái sử dụng hay xóa CSDL nghiệp vụ. Mặc định dọn các CSDL thử do chính lần chạy đó tạo;
thêm `--keep-database` để giữ lại. Backup ở thư mục `backup-check` cạnh DLL test.
Không cam kết môi trường LocalDB tương đương mọi cấu hình SQL Server Express 2022.

Bản publish hiện tại phụ thuộc .NET Desktop Runtime 10. Giao toàn bộ thư mục publish, không chỉ executable.
Tạo `appsettings.local.json` theo máy đích. Không yêu cầu cài Visual Studio trên máy chạy.

## Việc phối hợp còn lại

- Thái kiểm thử chéo module Như bằng tài khoản thật và xác nhận quy ước ngày nghiệp vụ.
- Mai nối FEFO với transaction bán hàng và cho Như kiểm thử chéo KT06–KT09; chưa có Sales trên nhánh nền nên chưa chạy được.
- Nam nối giao đơn/Reports; kiểm tra doanh thu và tồn sau giao.
- Như chạy luồng đầy đủ trên máy thứ hai, thử mất kết nối, khởi động lại và kiểm tra sao lưu/khôi phục theo hướng dẫn chung của Nam.

Kết quả tự động hiện tại không thay thế các ca toàn hệ thống trên máy thứ hai.
