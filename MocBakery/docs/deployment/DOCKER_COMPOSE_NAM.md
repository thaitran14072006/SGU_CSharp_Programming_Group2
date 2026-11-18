# Trần Thanh Nam — Docker Compose cho Mộc Bakery

## Phạm vi

Docker Compose chỉ chạy SQL Server 2022 Express; ứng dụng .NET 10 Windows Forms chạy trên máy Windows.
Sử dụng CSDL thử riêng, không ghi đè dữ liệu trên SQL Server đang sử dụng.

## Chuẩn bị

- Docker Desktop đang chạy ở Linux containers mode, có đủ RAM và dung lượng đĩa.
- SQL Server Management Studio (SSMS) trên Windows để thực thi script có `GO`.
- Thư mục này nằm tại `MocBakery/` cùng `database/schema/01_CreateDatabase.sql`.

## Chạy lần đầu (PowerShell tại MocBakery/)

```powershell
Copy-Item .env.example .env
notepad .env
# Sửa MSSQL_SA_PASSWORD thành mật khẩu mạnh riêng của bạn.
docker compose config
docker compose up -d
docker compose ps
docker compose logs --tail=80 sqlserver
```

Đợi SQL Server sẵn sàng. Kiểm tra Docker container:

```powershell
docker exec mocbakery-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<mat-khau-trong-env>' -C -Q 'SELECT @@VERSION'
```

Lưu ý: tránh ghi mật khẩu thật vào lịch sử shell trên máy dùng chung; có thể dùng SSMS thay cho `-P`.

## Tạo CSDL 9 bảng

Trong SSMS, kết nối:
- Server: `localhost,14333`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: giá trị trong `.env`
- Nếu xuất hiện cảnh báo chứng chỉ ở môi trường phát triển, bật Trust server certificate.

Mở và chạy `database/schema/01_CreateDatabase.sql`. Script này chỉ dùng trên database mới và không cho ghi đè các bảng đã tồn tại. Sau đó tùy chọn chạy `database/seeds/02_Customers.sql` và các seed phù hợp của nhóm.

## Kết nối WinForms

Đặt biến môi trường trong terminal chạy ứng dụng (thay mật khẩu):

```powershell
$env:MOCBAKERY_CONNECTION_STRING='Server=localhost,14333;Database=QuanLyTiemBanh;User Id=sa;Password=<mat-khau>;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10'
```

Hoặc tạo `src/MocBakery.GUI/appsettings.local.json` ngay cạnh chương trình xuất bản theo cấu trúc:

```json
{"ConnectionString":"Server=localhost,14333;Database=QuanLyTiemBanh;User Id=sa;Password=<mat-khau>;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10"}
```

Không commit tệp `.env` hoặc `appsettings.local.json` có mật khẩu. Tùy môi trường, file cấu hình cục bộ cần đặt tại thư mục executable (theo `AppContext.BaseDirectory`), không nhất thiết là thư mục source.

## Vận hành

```powershell
docker compose ps
docker compose logs -f sqlserver
docker compose stop
docker compose start
docker compose down
```

`docker compose down` không xóa named volume. **Không dùng `docker compose down -v`** trừ khi có chủ đích xóa toàn bộ dữ liệu thử và đã sao lưu.

## Kiểm tra trước bàn giao

- Container chạy được và ứng dụng đăng nhập sau khi tạo chủ tiệm lần đầu.
- Thực hiện thử bán hàng và giao đơn, đối chiếu SQL/FEFO và báo cáo theo KT10–KT15.
- Khởi động lại container rồi kiểm tra dữ liệu còn nguyên.
- Kiểm tra máy thứ hai, kiểm tra mất kết nối và thử sao lưu/khôi phục trên DB thử. Chỉ ghi Đạt sau khi chạy thực tế.
- Bảo mật: tài khoản `sa` chỉ dùng cho môi trường phát triển; cần login SQL ít quyền hơn khi triển khai thực tế, không mở cổng ra mạng ngoài.
