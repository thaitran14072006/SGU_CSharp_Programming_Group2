# Bàn giao Mộc Bakery ngày 15 tháng 11 năm 2026

Bản hiện hành trên main tích hợp Accounts/Customers của Thái, Catalog/Inventory của Như,
Sales của Mai và Orders/Reports của Nam. Phần còn thiếu được Như tiếp nhận hoàn thiện:
hồ sơ Nhân viên, liên kết Tài khoản, ma trận quyền, mật khẩu tạm và kiểm thử SQL chung.
Tài liệu này là điểm bắt đầu cho bản chốt; các báo cáo ngày trước được giữ làm lịch sử.

## Bản chạy và cài mới

Bộ bàn giao tại `artifacts/MocBakery-Final-2026-11-15/` và tệp ZIP cùng tên chứa:

- `app/`: executable Windows x64, kèm .NET Runtime; không cần Visual Studio hay .NET SDK để mở.
- `database/`: schema, migration, seed mẫu, truy vấn đối chiếu.
- `scripts/Initialize-Demo.ps1`: tạo một CSDL demo mới và cấu hình kết nối.
- `docs/`: hướng dẫn, báo cáo chương 5–6, kịch bản demo và kết quả có bằng chứng.

Máy nhận cần Windows x64, SQL Server/LocalDB đang chạy và sqlcmd để chạy script khởi tạo.
Giải nén ZIP trước khi sử dụng. Mở PowerShell tại thư mục giải nén và chạy:

```powershell
.\scripts\Initialize-Demo.ps1
# Hoặc với SQL Express dùng Windows Authentication:
.\scripts\Initialize-Demo.ps1 -Server '.\SQLEXPRESS'
.\app\MocBakery.GUI.exe
```

Script luôn tạo tên `MocBakery_Demo_<mã ngẫu nhiên>` mới, không xóa/ghi đè CSDL cũ.
Nếu app đã có cấu hình local, script giữ một bản `.previous-<thời điểm>` trước khi cập nhật.
Không chạy script nhiều lần khi chỉ cần mở lại ứng dụng: mở executable là đủ.
Chọn **Thiết lập lần đầu** để tạo quản lý của bạn, sau đó đăng nhập. Không có tài khoản/mật khẩu mặc định.
Seed là số dư mẫu để demo; không được coi là phiếu nhập hoặc giao dịch kinh doanh thật.

Để dùng CSDL có sẵn, tự cấu hình `app/appsettings.local.json` theo mẫu
`appsettings.example.json`. Biến `MOCBAKERY_CONNECTION_STRING`, nếu được đặt, ưu tiên hơn tệp cấu hình.
Không đưa tệp local hoặc chuỗi kết nối có mật khẩu lên Git.

## Nâng cấp dữ liệu cũ

Sao lưu CSDL đang dùng và kiểm tra có thể phục hồi trước khi nâng cấp. Chọn đúng database trong SSMS:

- Bản 9 bảng: chạy `02_Upgrade_ExcelDesign.sql`, rồi `03_Upgrade_StaffAccounts.sql`.
- Bản 15 bảng theo Excel: chạy `03_Upgrade_StaffAccounts.sql`.
- Bản mới hoàn toàn: chạy `01_CreateDatabase.sql`, đã có đủ 16 bảng; không chạy 01 trên dữ liệu cũ.

Migration 03 thêm bảng `NhanVien` và cờ `TaiKhoan.BatBuocDoiMatKhau`.
Tài khoản cũ tự có hồ sơ liên kết, giữ nguyên hash, vai trò, trạng thái khóa và lịch sử.
Điện thoại hồ sơ cũ chưa biết được để trống; cần điền đúng trước khi lưu hồ sơ.
Khóa tài khoản không đồng nghĩa với nghỉ việc, vì vậy migration giữ hồ sơ cũ ở trạng thái đang làm.
Migration 02 và 03 có thể chạy lại. Bộ kiểm tra đã xác nhận nâng cấp 9 → 15 và 15 → 16 bảng.

## Các thao tác của phần Thái

**Nhân viên:** tìm mã NV/tên/điện thoại; thêm/sửa họ tên, điện thoại 10 chữ số,
chức vụ, ngày vào làm, trạng thái. Ngày vào làm không vượt hôm nay, điện thoại không trùng.
Chuyển nghỉ việc phải xác nhận và khóa tài khoản liên quan trong cùng giao dịch.
Quay lại làm việc không tự mở khóa tài khoản; quản lý mở khóa riêng ở màn hình Tài khoản.
Không tự nghỉ việc/hạ quyền tài khoản đang sử dụng; luôn giữ quản lý hoạt động cuối cùng.

**Tài khoản:** tạo hồ sơ Nhân viên trước, rồi chọn nhân viên chưa có tài khoản để cấp tài khoản.
Tên đăng nhập 3–50 ký tự, duy nhất và không đổi sau khi tạo. Mỗi tài khoản liên kết một nhân viên.
Mật khẩu tạo mới qua màn hình này và mật khẩu đặt lại là mật khẩu tạm, phải đổi trước khi dùng nghiệp vụ.
Đổi mật khẩu thành công yêu cầu đăng nhập lại; các phiên cũ mất hiệu lực.
Ma trận quyền hiển thị quyền nghiệp vụ theo vai trò. Kiểm tra quyền thực sự được thực hiện tại BLL.
Họ tên/chức vụ của hồ sơ và tài khoản được đồng bộ khi lưu; không xóa lịch sử đăng nhập liên quan hóa đơn.

**Khách hàng:** tìm tên/SĐT, thêm/sửa hồ sơ, chặn trùng SĐT, giữ số 0 đầu,
xem các hóa đơn đã phát sinh. Khách lẻ không bắt buộc tạo hồ sơ khi bán hàng.
Các trang có dữ liệu chưa lưu hỏi trước khi chuyển trang/đóng cửa sổ.

## Quyền trong bản chốt

| Nghiệp vụ | Quản lý | Thu ngân | Kho |
| --- | --- | --- | --- |
| Tra cứu bánh, tồn kho | Có | Có | Có |
| Thêm/sửa bánh, danh mục | Có | Không | Không |
| Nhập, xuất, kiểm kê, nhà cung cấp/nguyên liệu | Có | Không | Có |
| Bán hàng, hóa đơn, đơn đặt, khách hàng | Có | Có | Không |
| Nhân viên, tài khoản, báo cáo | Có | Không | Không |
| Đổi mật khẩu của mình | Có | Có | Có |

Vai trò `NhanVien` cũ tương thích Thu ngân trong Sales/Orders/Customers, chỉ đọc Catalog/Inventory.
Thu hồi vai trò/khóa/reset mật khẩu có hiệu lực ở lần gọi nghiệp vụ tiếp theo, dù menu phiên cũ còn hiện.

## Build và kiểm tra lại

Cần .NET SDK 10, SQL LocalDB và sqlcmd trên máy phát triển. Từ gốc repository:

```powershell
.\MocBakery\scripts\Validate-Project.ps1 -IncludeGui
dotnet publish .\MocBakery\src\MocBakery.GUI\MocBakery.GUI.csproj -c Release -r win-x64 --self-contained true -o .\MocBakery\artifacts\publish
```

Bộ console chạy qua DLL, không dùng `dotnet test`. Script kiểm tra dừng khi có ca lỗi,
lưu log trong `artifacts/validation-<thời điểm>`. Test SQL tạo CSDL riêng và tự dọn.
Test GUI dùng fixture có tiền tố bắt buộc `MocBakery_NhuGuiTest_`, giữ theo máy để xem ảnh/dữ liệu.
Các sự kiện form được chạy ngoài màn hình với SQL thật; không điều khiển chuột/bàn phím desktop.

## Sao lưu và khôi phục

Trong SSMS chọn đúng CSDL → Tasks → Back Up → Full, lưu `.bak` ở thư mục mà dịch vụ SQL có quyền ghi.
Để xác nhận bản sao lưu, Restore vào **tên database mới**, chọn đường dẫn data/log mới và không bật ghi đè CSDL đang dùng.
Đối chiếu số khách, sản phẩm, tồn lô, phiếu nhập, hóa đơn và doanh thu trước khi đổi cấu hình ứng dụng.
Bộ tích hợp đã backup/restore bản CSDL thử độc lập và đối chiếu dữ liệu bánh/lô thành công.
Không có nút backup/restore tự động trong ứng dụng.

## Hồ sơ và giới hạn nghiệm thu

Kết quả hiện hành: `docs/testing/results/2026-11-15-final.md`.
Báo cáo để ghép bản chính: `docs/report/CHUONG_5_6_TONG_HOP.md`.
Kịch bản thuyết trình/demo: `docs/deployment/DEMO_VA_VAN_DAP.md`.

190 ca tự động đã đạt trên máy hiện tại; build không lỗi/cảnh báo. Đây không phải bằng chứng
đã chạy trên máy thứ hai, nhiều DPI hoặc thử in ra máy in vật lý. Nhóm cần thử mở bộ ZIP trên máy trình bày
và ghép chương 5–6 vào báo cáo chính trước khi nộp. File báo cáo gốc/phân công Word cũ không còn ở đường dẫn
Downloads lúc tổng hợp nên không tự sửa hoặc thay thế các chương 1–4 và thông tin bìa chưa được cung cấp.
