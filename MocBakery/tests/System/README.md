# Kiểm thử GUI Catalog và Inventory với SQL thật

Project GuiChecks là bộ chạy thử độc lập, không phải executable giao cho người dùng cuối.
Nó dùng đúng MainForm, page, dialog và AppServices của ứng dụng, kết nối SQL LocalDB thật.
Chỉ chấp nhận database có tiền tố `MocBakery_NhuGuiTest_`; không mở CSDL nghiệp vụ.
Account và session là fixture được khởi tạo qua repository/AuthService, không tự động hóa hộp thoại đăng nhập của người dùng.
Không dùng UiSmoke.Active, không dùng repository trong bộ nhớ và không thay đường đi GUI → BLL → DAL.

Từ thư mục gốc repo, chuẩn bị fixture chủ tiệm:

```powershell
.\MocBakery\tests\System\PrepareGuiFixture.ps1 -NewFixture
```

Mở executable theo đường dẫn script in ra, kiểm tra qua giao diện, đóng ứng dụng rồi chuyển vai trò:

```powershell
.\MocBakery\tests\System\PrepareGuiFixture.ps1 -Staff
```

Cần LocalDB đã cài, .NET SDK 10 và sqlcmd. CSDL và cấu hình fixture được giữ theo máy,
không đưa config/binary lên Git. Script không xóa dữ liệu, không cài phần mềm và không đổi cấu hình máy.
Không gọi script NewFixture liên tục để tránh chiếm dung lượng.

Kịch bản và bằng chứng lần chạy thật được ghi ở `docs/testing/results/2026-11-15-nhu-gui.md`.
Sau kịch bản đó, chạy `VerifyGuiFixture.sql` trên đúng CSDL thử để xác nhận giá 40.000 và tồn 6.
Kết quả fixture session không thay thế kiểm thử đăng nhập, máy thứ hai hoặc transaction của Sales/Orders.

## Bản giao diện theo Excel

`PrepareGuiFixture.ps1 -NewFixture` tạo schema 15 bảng hiện hành. Fixture cũ cần migration 02 trước khi mở GUI mới.
Để tự kiểm tra các form qua sự kiện WinForms và SQL thật:

```powershell
dotnet .\MocBakery\tests\System\bin\Release\net10.0-windows\MocBakery.GuiChecks.dll --verify-design --owner
```

Bộ kiểm tra chạy trong message loop WinForms, lưu dữ liệu thật vào fixture và trả lời xác nhận qua callback nội bộ,
không điều khiển chuột/bàn phím hoặc chụp màn hình desktop. `MOCBAKERY_UI_OUTPUT` chọn nơi lưu ảnh render.
Nó kiểm tra phiếu nhiều dòng, nháp/ghi sổ và quyền Thu ngân. Hướng dẫn hiện hành ở `docs/deployment/ExcelDesign_Update.md`.
