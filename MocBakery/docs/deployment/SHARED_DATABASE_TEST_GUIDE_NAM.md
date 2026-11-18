# Mộc Bakery — Hướng dẫn tạo database thử nghiệm và kết nối Desktop/Web

**Phụ trách đề xuất/triển khai:** Trần Thanh Nam (Deployment / Docker / phối hợp tích hợp).  
**CSDL nguồn:** `database/schema/01_CreateDatabase.sql` do cả nhóm quản lý; **không tạo schema cạnh tranh**.  
**Áp dụng:** Windows PowerShell, Docker Desktop, SQL Server 2022 Express, .NET 10.

## 1. Chọn một trong hai mô hình

**Khuyên dùng khi phát triển:** mỗi thành viên chạy một SQL Server Docker riêng, cùng một schema + seed. Dễ tái tạo, test không ảnh hưởng người khác. Dữ liệu không tự đồng bộ giữa các máy.

**Khi cần xem cùng một dữ liệu:** chỉ **một máy chủ/VM** chạy SQL Server và chứa `QuanLyTiemBanh`. Desktop của các thành viên và backend Web cùng truy cập tới máy chủ đó. Đòi hỏi kết nối mạng riêng an toàn, quản lý backup, tài khoản và quyền truy cập. **Không nên mở SQL Server ra Internet công khai.**

## 2. Giữ nguyên những thứ nhóm đã làm

Repo đã có `MocBakery/compose.yaml` chạy `mcr.microsoft.com/mssql/server:2022-latest`, volume `sqlserver_data`, bind `127.0.0.1:14333:1433`. `.gitignore` đã loại `.env` và `*.local.json`. `AppServices.cs` đã hỗ trợ biến môi trường `MOCBAKERY_CONNECTION_STRING`.

Repo hiện tại có script tạo **9 bảng cốt lõi và các phần mở rộng do các module bổ sung**, nên số bảng thực tế có thể **lớn hơn 9**. Không xóa bớt các bảng mở rộng. Script gốc có điều kiện chặn tạo lại khi bảng nghiệp vụ đã tồn tại.

## 3. Bắt đầu từ thư mục `MocBakery/`

```powershell
# Chỉ tạo .env lần đầu; không ghi đè mật khẩu đang dùng
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
notepad .env  # thay mật khẩu mẫu bằng mật khẩu đủ mạnh

docker compose config --quiet
# Chỉ khi bạn CHƯA có database QuanLyTiemBanh (hoặc kiểm tra database đã có):
./scripts/Initialize-TestDatabase.ps1 -SeedDemo
```

`Initialize-TestDatabase.ps1` tự khởi động container, chờ SQL phản hồi, kiểm tra `QuanLyTiemBanh`, **chỉ tạo schema khi database chưa tồn tại**, rồi xác nhận 9 bảng cốt lõi và tùy chọn seed. Không xóa volume. Nếu database tồn tại nhưng thiếu bảng, script báo lỗi và **không tạo lại/ghi đè**.

Seed mẫu mới tại `database/seeds/03_DemoCatalogInventory.sql` có 2 loại bánh, 2 sản phẩm, 4 lô (gồm 2 lô FEFO 4+5, 1 lô hết hạn và 1 lô bánh mì). Seed kiểm tra sự tồn tại trước khi thêm; chạy lại không tự khôi phục số tồn đã bán. Có thể chạy thêm `database/seeds/02_Customers.sql` để có 2 khách mẫu. **Không tạo tài khoản mật khẩu mẫu bằng SQL**: dùng **Thiết lập lần đầu** trên Desktop tạo chủ tiệm với mật khẩu băm. Ngày hết hạn demo tính từ ngày seed; nếu để lâu sẽ phải tạo lô mới qua ứng dụng.

> Lệnh này dành cho CSDL thử nghiệm, KHÔNG chạy trên dữ liệu bán hàng thật. Không chạy script schema thủ công lần nữa trên database đã có bảng.

## 4. Kết nối Desktop cùng máy Docker

```powershell
./scripts/Start-Desktop-Test.ps1
```

Nhập mật khẩu `sa` khi được hỏi; script đặt biến kết nối **chỉ trong tiến trình chạy ứng dụng** rồi khôi phục giá trị cũ khi đóng. Trên máy khác, sau khi đã có kết nối mạng riêng an toàn đến máy chủ:

```powershell
./scripts/Start-Desktop-Test.ps1 -DbHost 192.168.1.20 -DbPort 14333
```

Thay IP bằng IP LAN thực tế. Không commit mật khẩu. Desktop trực tiếp SQL phù hợp bài tập trên LAN; triển khai sản phẩm thực tế nên đi qua API, tài khoản DB quyền tối thiểu và TLS hợp lệ (script hiện dùng `TrustServerCertificate=True` chỉ cho test).

Nếu sử dụng Visual Studio, có thể khai báo `MOCBAKERY_CONNECTION_STRING` trong môi trường chạy Debug, hoặc `appsettings.local.json` **cạnh executable**, không phải tự động ở thư mục source.

## 5. Kiểm chứng dữ liệu

Mở SSMS kết nối `localhost,14333`, SQL Authentication, `sa`, mật khẩu trong `.env`, bật Trust server certificate **trong môi trường test**. Chạy truy vấn trong `database/queries/VerifySharedDatabase.sql`.

1. Xác nhận database `QuanLyTiemBanh` tồn tại; số bảng cốt lõi phải là 9 (tổng số bảng có thể >9).
2. Đăng nhập Desktop (tạo chủ tiệm nếu chưa có). Tạo khách, đặt đơn; đối chiếu `dbo.KhachHang`, `dbo.DonDatBanh`.
3. Bán thử 6 bánh `DEMO - Banh kem dau` **khi hai lô còn hạn và chưa bị bán**; mong đợi 4+2 theo FEFO, còn 0+3. Nếu lô đã hết hạn, seed lại dữ liệu demo thông qua ứng dụng.
4. Kiểm tra hóa đơn, tồn và doanh thu; đơn chưa giao không được trừ tồn, hóa đơn giao đơn không tính cọc hai lần.
5. `docker compose stop` → `docker compose start`, kiểm tra dữ liệu vẫn còn.
6. Web backend đọc/ghi **cùng database**, tải lại Desktop để đối chiếu. Không nên cho frontend/browser kết nối trực tiếp SQL Server.

## 6. Cách cho cả nhóm cùng truy cập một SQL Server

Mặc định `127.0.0.1:14333:1433` **chỉ truy cập được từ máy chạy Docker**. Muốn cho máy khác kết nối, cả nhóm cần thống nhất máy chủ và mạng LAN/VPN riêng (ví dụ VPN nội bộ), phân quyền truy cập và sao lưu.

Trên máy chủ, **chỉ sau khi thống nhất bảo mật**, sửa đúng **một dòng** trong `compose.yaml`:

```yaml
# Chỉ ví dụ LAN riêng: thay bằng IP của máy chủ nếu bind được
ports:
  - "192.168.1.20:14333:1433"
```

Sau đó chạy `docker compose up -d` để áp dụng cấu hình. Tùy Docker Desktop, bind IP cụ thể có thể không khả dụng; khi đó cần cấu hình mạng host/firewall thích hợp. **Không tự động đổi thành `0.0.0.0` hoặc mở port 14333 trên router/Internet.** Giới hạn Windows Firewall chỉ cho các máy thành viên/VPN và tạo SQL login riêng quyền thấp; không phát tán mật khẩu `sa` của máy chủ cho cả nhóm.

Máy khác thử `Test-NetConnection 192.168.1.20 -Port 14333`. Trong Docker network của backend Web (nếu backend chạy container cùng Compose), kết nối `sqlserver,1433` thay vì `localhost,14333`.

## 7. Xử lý lỗi thông dụng

- **Login failed for user 'sa':** kiểm tra mật khẩu; đổi `.env` sau khi volume đã được tạo **không tự đổi mật khẩu sa trong SQL**.
- **Connection refused / timeout:** chạy `docker compose ps`, `docker compose logs sqlserver`, kiểm tra bind IP/cổng/firewall.
- **Invalid object name:** đã kết nối SQL nhưng chưa chạy script schema hoặc đang chỉ sai database.
- **Script schema đã tồn tại:** script tạo ban đầu không chạy lại; công việc thay đổi cấu trúc về sau phải dùng migration đã được nhóm duyệt.
- **Xóa dữ liệu:** tránh `docker compose down -v` vì xóa volume; giữ bản backup riêng trước thử nghiệm rủi ro.

## 8. Checklist nghiệm thu (chưa chạy trên máy người nhận)

| Mã | Nội dung | Trạng thái ban đầu |
|---|---|---|
| DB01 | Docker SQL Server sẵn sàng | Chưa chạy trong môi trường này |
| DB02 | Tạo schema từ script repo | Chưa chạy |
| DB03 | 9 bảng cốt lõi và seed demo | Chưa chạy |
| DB04 | Desktop đăng nhập & ghi vào Docker | Chưa chạy |
| DB05 | Web backend & Desktop đọc chung | Chưa chạy |
| DB06 | Dữ liệu còn sau restart | Chưa chạy |

Ghi commit, ngày chạy, người chạy, thông báo thực tế, ảnh Docker/SSMS và đối chiếu dữ liệu vào `docs/testing/results/` sau khi chạy thật.
