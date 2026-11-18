# Tài khoản khách hàng Web

Đợt đầu xây dựng Models, BLL, DAL và migration tài khoản khách hàng. Chưa có giao diện
Web, cookie, đăng xuất hoặc trang hồ sơ HTTP. Không coi bộ test bộ nhớ là kiểm thử SQL hay Web.

## Dữ liệu và quyền

- Tài khoản mới nằm trong `TaiKhoanKhachHang`, liên kết duy nhất với `KhachHang`.
  Không tạo tài khoản nhân viên, không thay đổi `Roles` hoặc dịch vụ desktop.
- Hồ sơ Web chỉ có họ tên, số điện thoại, địa chỉ; không có ghi chú nội bộ.
- Đăng ký tạo tài khoản và khách hàng trong một transaction. Không tự nhận hồ sơ cũ
  chỉ dựa vào số điện thoại. Luồng xác minh và liên kết hồ sơ cũ chưa triển khai.
- Đổi mật khẩu thay `SecurityStamp`, làm danh tính cũ hết hiệu lực. Web host phải lưu
  danh tính trong cookie đã được bảo vệ, xác minh ở mỗi request; chưa có host ở đợt này.
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

Project này độc lập với các test desktop. Chưa thêm vào solution hoặc workflow chung;
sẽ tích hợp cùng project Web sau khi commit phần nền tảng.

Kết quả chạy cục bộ ngày 18/11/2026: bộ CustomerAccountChecks đạt 36/36;
bộ UnitChecks trên main hiện tại đạt 45/45. BLL và DAL build Release không cảnh báo,
không lỗi; DAL được biên dịch với `RestoreSqlClient=false` để kiểm tra mã độc lập
driver. Chưa chạy migration, kiểm thử SQL, HTTP hoặc xác nhận trên GitHub Actions.
Các kết quả này không thay thế minh chứng tích hợp với CSDL thật.

## Bước tiếp theo

Tạo Web host và trang đăng ký, đăng nhập; bổ sung cookie, antiforgery, giới hạn thử
đăng nhập, trang hồ sơ, đổi mật khẩu và đăng xuất. Kiểm thử HTTP và SQL thật trước
khi ghi nhận chức năng hoạt động qua hai ứng dụng. Phần Catalog, Sales, Orders,
Reports và Docker không nằm trong đợt tài khoản này.
