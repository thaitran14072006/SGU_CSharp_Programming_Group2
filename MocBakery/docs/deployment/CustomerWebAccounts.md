# Tài khoản khách hàng Web

Đã có Models, BLL, DAL, migration và Web host ASP.NET Core MVC cho tài khoản khách.
Web có đăng ký, đăng nhập, đăng xuất, xem và cập nhật hồ sơ tài khoản hiện tại.
Đổi mật khẩu mới có BLL, chưa có trang Web tương ứng.
Kiểm thử HTTP dùng repository bộ nhớ; chưa thay thế kiểm thử SQL thật và desktop–Web.

## Dữ liệu và quyền

- Tài khoản mới nằm trong `TaiKhoanKhachHang`, liên kết duy nhất với `KhachHang`.
  Không tạo tài khoản nhân viên, không thay đổi `Roles` hoặc dịch vụ desktop.
- Hồ sơ Web chỉ có họ tên, số điện thoại, địa chỉ; không có ghi chú nội bộ.
- Đăng ký tạo tài khoản và khách hàng trong một transaction. Không tự nhận hồ sơ cũ
  chỉ dựa vào số điện thoại. Luồng xác minh và liên kết hồ sơ cũ chưa triển khai.
- Đổi mật khẩu thay `SecurityStamp`, làm danh tính cũ hết hiệu lực. Web host phải lưu
  danh tính trong cookie đã được bảo vệ, xác minh ở mỗi request qua `CustomerCookie.Validate`.
- Chưa có xác minh số điện thoại, khôi phục mật khẩu hoặc chức năng khóa khách trên UI.

## Migration dùng chung

Sau khi khởi tạo schema chung, chạy `database/schema/04_CustomerWebAccounts.sql`
với database thử được chọn rõ bằng `sqlcmd -d <ten_database>` hoặc trong SSMS.
Script không có `USE`, không sửa bảng nhân viên và không seed mật khẩu mặc định.
Không tự chạy migration trên máy chủ dùng chung. Khi áp dụng sẽ thêm một bảng, từ
16 thành 17 bảng nếu database nền có đầy đủ schema hiện hành.

Ví dụ SQL Server có Windows Authentication, thay tên database thử trước khi chạy:

```powershell
sqlcmd -S '.\SQLEXPRESS' -E -C -b -d '<ten_database_thu>' -i '.\MocBakery\database\schema\04_CustomerWebAccounts.sql'
```

Máy dùng Docker hoặc SQL Authentication cần dùng cấu hình xác thực tương ứng;
không chép mật khẩu vào repo. Script khởi tạo hiện có chưa tự chạy migration 04.

## Kiểm tra nghiệp vụ không cần SQL

Từ gốc repo:

```powershell
dotnet run --project MocBakery/tests/CustomerAccounts/MocBakery.CustomerAccountChecks.csproj -c Release
```

Project này độc lập với các test desktop. Đã đưa project Web và hai bộ kiểm tra
tài khoản khách vào solution; workflow chạy kiểm tra nghiệp vụ và HTTP sau khi build.
Chưa xác nhận kết quả chạy trên GitHub Actions trước khi push.

Kết quả ở commit nền tảng ngày 18/11/2026: bộ CustomerAccountChecks đạt 36/36;
bộ UnitChecks trên main hiện tại đạt 45/45. BLL và DAL build Release không cảnh báo,
không lỗi; DAL được biên dịch với `RestoreSqlClient=false` để kiểm tra mã độc lập
driver. Tại commit nền tảng chưa chạy migration, kiểm thử SQL, HTTP hoặc xác nhận trên GitHub Actions.
Các kết quả này không thay thế minh chứng tích hợp với CSDL thật.

## Chạy Web tại máy phát triển

1. Cấu hình cùng SQL Server và database mà desktop sử dụng, ưu tiên database thử.
2. Chạy migration 04 trên database đó trước khi đăng ký khách. Web không tự chạy SQL migration.
3. Từ gốc repo, cấu hình kết nối trong phiên PowerShell và chạy:

```powershell
$env:ConnectionStrings__MocBakery = 'Server=.\SQLEXPRESS;Database=<ten_database_thu>;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project MocBakery/src/MocBakery.Web/MocBakery.Web.csproj
```

Mở `http://localhost:5080/tai-khoan/dang-ky`. Nếu đã có tài khoản, mở
`http://localhost:5080/tai-khoan/dang-nhap`. Đăng nhập thành công chuyển tới
`/tai-khoan`, chỉ đọc hồ sơ của danh tính từ cookie; đăng xuất là POST có antiforgery.

Tại trang tài khoản, chọn **Cập nhật hồ sơ** hoặc mở `/tai-khoan/ho-so` sau khi đăng nhập.
Chỉ sửa họ tên, số điện thoại và địa chỉ của danh tính trong cookie; không nhận mã khách,
quyền nhân viên hoặc ghi chú nội bộ từ biểu mẫu. Lưu thành công quay về trang tài khoản
và hiện thông báo một lần; tải lại trang không gửi lại thao tác lưu. Bấm **Hủy và quay lại**
không ghi dữ liệu. Lỗi dữ liệu hoặc lỗi ghi CSDL giữ lại bản nháp để sửa hoặc thử lại.
Nếu phiên không còn hợp lệ lúc lưu, Web yêu cầu đăng nhập lại và không lưu thay đổi.

Biến `MOCBAKERY_CONNECTION_STRING` nếu đã có sẽ được ưu tiên hơn `ConnectionStrings__MocBakery`.
Dùng đúng server của nhóm, không mặc định rằng `.\SQLEXPRESS` tồn tại. Không đưa
connection string thật vào repo. Web sử dụng Microsoft.Data.SqlClient từ DAL;
chạy bình thường cần restore driver, không đặt `RestoreSqlClient=false` khi dùng SQL thật.

## Kiểm thử HTTP

```powershell
dotnet run --project MocBakery/tests/WebAccounts/MocBakery.WebAccountChecks.csproj -c Release
```

Bộ kiểm tra khởi động Kestrel trên loopback ở cổng tạm, dùng cookie và antiforgery thật,
thay repository bằng fixture chỉ trong test host. Production host luôn dùng repository SQL;
không có cờ bật tài khoản mẫu hoặc dữ liệu bộ nhớ. Test host dừng khi chạy xong.

Ngày 18/11/2026, sau khi thêm cập nhật hồ sơ: 35/35 kiểm tra Web đạt (34 ca HTTP và 1 ca cấu hình production); project Web build Release không cảnh báo,
không lỗi với `RestoreSqlClient=false`. Đây không phải kết quả SQL hoặc kiểm tra giao diện
trực quan trên điện thoại. Hai bộ kiểm tra có 36 ca BLL và 35 ca Web, không cộng vào
các kết quả lịch sử ngày 15/11.

Hồi quy cùng lượt: UnitChecks 45/45, CatalogInventoryChecks 43/43, CustomerAccountChecks
36/36 đều đạt. Build toàn solution thành công với output tách riêng qua
`--artifacts-path` và `RestoreSqlClient=false`, không lỗi/cảnh báo. Build vào thư mục
desktop mặc định ban đầu bị khóa DLL do ứng dụng đang mở; không đóng hay dừng ứng dụng.

## Cấu hình phiên và giới hạn

- Cookie khách có HttpOnly, SameSite=Lax, hết hạn sau 30 phút và không lưu bền qua phiên.
  Cookie bắt buộc Secure ngoài Development; triển khai thật cần HTTPS.
- Mọi POST MVC kiểm tra antiforgery. Trang tài khoản không được cache và không cho nhúng iframe.
- Đăng nhập và đăng ký giới hạn 10 POST mỗi phút theo IP, mỗi chính sách riêng;
  không bật tin cậy X-Forwarded-For từ mọi nguồn. Khi đặt sau proxy cần cấu hình nguồn tin cậy.
- Khóa tài khoản hoặc thay mật khẩu làm cookie cũ bị từ chối ở request tiếp theo.
  Đăng xuất xóa cookie của trình duyệt hiện tại, không tự đăng xuất mọi thiết bị khác.
- Triển khai nhiều instance hoặc container cần cấu hình lưu và chia sẻ Data Protection keys;
  Docker, trang chủ bán bánh và các module khác chưa được tích hợp ở đợt xác thực này.

## Nguồn kỹ thuật

- Microsoft Learn, Tag Helpers in forms in ASP.NET Core:
  https://learn.microsoft.com/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-10.0
- Microsoft Learn, Prevent Cross-Site Request Forgery:
  https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
- Microsoft Learn, Rate limiting middleware:
  https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0

Mã triển khai trong repo áp dụng MVC Form Tag Helper, AutoValidateAntiforgeryToken
và fixed window limiter từ nền tảng ASP.NET Core; không chép ví dụ thành chức năng
ngoài phạm vi tài khoản khách hàng.

## Bước tiếp theo

Làm trang đổi mật khẩu sau commit cập nhật hồ sơ. Kiểm thử SQL
thật trước khi ghi nhận chức năng hoạt động qua hai ứng dụng. Phần Catalog, Sales,
Orders, Reports và Docker không nằm trong đợt tài khoản này.
