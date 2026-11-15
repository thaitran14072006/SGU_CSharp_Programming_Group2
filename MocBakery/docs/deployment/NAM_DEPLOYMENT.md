# Hướng dẫn triển khai phần Nam

1. Cài .NET 10 SDK + Visual Studio hỗ trợ WinForms trên máy phát triển; SQL Server 2022 Express và SSMS. Máy sử dụng cần Windows, SQL Server truy cập được, .NET Desktop Runtime 10 nếu không publish self-contained.
2. Tại repo `SGU_CSharp_Programming_Group2`, triển khai `MocBakery/database/schema/01_CreateDatabase.sql` **trên CSDL thử mới**, không ghi đè dữ liệu hiện hữu. Nhóm thống nhất seed cho Accounts, Customers, Catalog, Inventory trước khi chạy Orders.
3. Đặt `MOCBAKERY_CONNECTION_STRING` hoặc cấu hình `appsettings.local.json` theo `src/MocBakery.GUI/appsettings.example.json`. Không commit mật khẩu hay chuỗi kết nối bí mật.
4. Ghép file Nam vào `MocBakery/`; tích hợp `AppServices.cs`, `MainForm.cs` bằng diff nếu hai file đã được Thái chỉnh sửa.
5. Tại gốc repo: `dotnet restore MocBakery/MocBakery.sln`; `dotnet build MocBakery/MocBakery.sln -c Release`. Chạy WinForms trên Windows; đăng nhập rồi thử Đặt bánh/Báo cáo.
6. Kiểm tra KT10–KT13; chạy `database/queries/VerifyOrderReports.sql` trên CSDL thử, so sánh tồn/hóa đơn/doanh thu. Xuất ảnh và ghi trạng thái thực tế.
7. Publish ví dụ: `dotnet publish MocBakery/src/MocBakery.GUI/MocBakery.GUI.csproj -c Release -r win-x64 --self-contained true -o publish/MocBakery`. Trên máy thứ hai kiểm tra kết nối, đăng nhập, bán, giao đơn, báo cáo và khôi phục DB thử riêng.
8. Sao lưu/khôi phục bằng SSMS cho **CSDL thử**, xác nhận tên nguồn/đích trước Restore.

**Cảnh báo:** ZIP này là nguồn bổ sung, không chứa DLL/EXE Release; chưa có kết quả chạy thực tế.
