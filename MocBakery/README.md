# Mộc Bakery

Ứng dụng quản lý tiệm bánh trên Desktop và Web, dùng chung cơ sở dữ liệu SQL Server. Thông tin sản phẩm, khách hàng và đơn hàng được cập nhật giữa hai ứng dụng khi tải lại dữ liệu.

## Chức năng

| Nhóm chức năng | Nội dung |
| --- | --- |
| Tài khoản và nhân viên | Đăng nhập, phân quyền, quản lý nhân viên, khóa tài khoản, cấp mật khẩu tạm và đổi mật khẩu |
| Khách hàng | Quản lý hồ sơ, tìm kiếm và xem lịch sử mua hàng |
| Sản phẩm | Quản lý danh mục, bánh, giá bán, ảnh và trạng thái kinh doanh |
| Kho | Nhà cung cấp, nguyên liệu, lô bánh, hạn dùng, phiếu nhập, xuất kho, kiểm kê và nhật ký |
| Bán hàng | Bán tại quầy, lập hóa đơn và cập nhật tồn theo FEFO: ưu tiên lô hết hạn trước |
| Đơn đặt | Tiếp nhận, xử lý, giao và hủy đơn; liên kết hóa đơn khi giao hàng |
| Báo cáo | Doanh thu và bánh bán chạy theo khoảng thời gian |
| Website khách hàng | Danh mục, tìm kiếm, chi tiết bánh, tài khoản, giỏ hàng, đặt bánh và theo dõi đơn |

Chủ tiệm quản lý toàn bộ nghiệp vụ. Thu ngân/nhân viên xử lý bán hàng, khách hàng và đơn đặt theo quyền được cấp. Nhân viên kho thực hiện nghiệp vụ kho. Tài khoản khách hàng Web tách biệt với tài khoản nhân viên.

## Công nghệ

- C# và .NET 10.
- Windows Forms cho Desktop; ASP.NET Core MVC cho Web.
- SQL Server 2022, ADO.NET và Microsoft.Data.SqlClient.
- GitHub Actions để build và chạy các bộ kiểm thử.

Giao diện gọi lớp nghiệp vụ `BLL`; lớp `DAL` truy cập SQL Server. `Models` chứa thực thể và kiểu dữ liệu dùng chung.

## Cấu trúc thư mục

| Đường dẫn | Nội dung |
| --- | --- |
| `src/MocBakery.GUI` | Ứng dụng Windows Forms |
| `src/MocBakery.Web` | Website khách hàng và quản trị |
| `src/MocBakery.BLL` | Xử lý nghiệp vụ và phân quyền |
| `src/MocBakery.DAL` | Kết nối và truy vấn cơ sở dữ liệu |
| `src/MocBakery.Models` | Thực thể và dữ liệu trao đổi |
| `database/schema` | Script tạo và nâng cấp cơ sở dữ liệu |
| `database/seeds` | Dữ liệu mẫu |
| `database/queries` | Truy vấn đối chiếu dữ liệu |
| `tests` | Các chương trình kiểm thử |
| `scripts` | Lệnh chạy, kiểm tra và đóng gói |
| `docs` | Tài liệu triển khai, kiểm thử và báo cáo |

## Cài đặt

Cần Windows để chạy Desktop, .NET 10 SDK và SQL Server có thể truy cập từ máy chạy ứng dụng. Dùng SSMS hoặc sqlcmd để thực thi script SQL.

Các lệnh bên dưới chạy từ thư mục `MocBakery`:

```powershell
cd MocBakery
dotnet restore .\MocBakery.sln
dotnet build .\MocBakery.sln -c Release
```

### Cơ sở dữ liệu

Với cơ sở dữ liệu mới, chạy lần lượt các script trong `database/schema`:

1. `01_CreateDatabase.sql`
2. `02_Upgrade_ExcelDesign.sql`
3. `03_Upgrade_StaffAccounts.sql`
4. `04_CustomerWebAccounts.sql`
5. `05_WebOrders.sql`
6. `06_WebOrderContacts.sql`

Database mặc định là `QuanLyTiemBanh`. Sau bước tạo mới, chọn đúng database trước khi chạy các bản nâng cấp. Với database đang sử dụng, sao lưu và chỉ chạy những bản nâng cấp còn thiếu; không chạy lại script tạo mới lên dữ liệu hiện có. Dữ liệu trong `database/seeds` dùng cho môi trường thử.

### Kết nối dùng chung

Đặt biến môi trường trong mỗi cửa sổ PowerShell dùng để chạy ứng dụng. Ví dụ với SQL Server Express trên máy cá nhân:

```powershell
$env:MOCBAKERY_CONNECTION_STRING = 'Server=.\SQLEXPRESS;Database=QuanLyTiemBanh;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10'
```

Nếu dùng máy chủ chung, thay bằng chuỗi kết nối được cấp cho máy chủ đó. Desktop và Web phải trỏ đến cùng server và database. `TrustServerCertificate=True` dùng trong môi trường phát triển với chứng chỉ tự ký; triển khai thực tế cần cấu hình chứng chỉ phù hợp.

Desktop cũng đọc `appsettings.local.json` cạnh file thực thi, theo cấu trúc của `src/MocBakery.GUI/appsettings.example.json`. Biến `MOCBAKERY_CONNECTION_STRING` được ưu tiên. Không đưa mật khẩu hoặc cấu hình kết nối thật lên Git.

## Chạy ứng dụng

### Desktop

```powershell
dotnet run --project .\src\MocBakery.GUI\MocBakery.GUI.csproj
```

Khi bảng tài khoản còn trống, dùng **Thiết lập lần đầu** ở màn hình đăng nhập để tạo Chủ tiệm. Khi đã có dữ liệu, đăng nhập bằng tài khoản nhân viên được cấp.

Có thể dùng script hỏi mật khẩu SQL và khởi chạy Desktop:

```powershell
.\scripts\Start-Desktop-Test.ps1 -DbHost <dia-chi-may-chu> -DbPort 14333 -Database QuanLyTiemBanh -DbUser <tai-khoan-sql>
```

### Web

Trong cửa sổ PowerShell đã cấu hình kết nối:

```powershell
dotnet run --project .\src\MocBakery.Web\MocBakery.Web.csproj
```

| Trang | Địa chỉ |
| --- | --- |
| Cửa hàng | http://localhost:5080/ |
| Đăng ký khách hàng | http://localhost:5080/tai-khoan/dang-ky |
| Đăng nhập khách hàng | http://localhost:5080/tai-khoan/dang-nhap |
| Hồ sơ khách hàng | http://localhost:5080/tai-khoan |
| Đăng nhập nhân viên | http://localhost:5080/admin/dang-nhap |
| Quản trị | http://localhost:5080/admin |

Web quản trị dùng tài khoản nhân viên của Desktop. Tài khoản SQL chỉ dùng kết nối cơ sở dữ liệu, không dùng đăng nhập ứng dụng.

Ảnh tải lên từ màn hình sửa bánh được lưu trong cơ sở dữ liệu để cả Web và Desktop sử dụng. Ảnh dự phòng của Web nằm ở `src/MocBakery.Web/wwwroot/images/bakery`.

### SQL Server bằng Docker

`compose.yaml` chạy SQL Server, không chạy Desktop hoặc Web. Tạo file `.env` trong thư mục này với biến `MSSQL_SA_PASSWORD`, rồi chạy:

```powershell
docker compose up -d
```

Cấu hình Compose hiện gắn cổng `14333` vào địa chỉ mạng của máy chủ nhóm. Nếu chạy trên máy khác, sửa địa chỉ bind trong `compose.yaml` cho phù hợp. SQL Server dùng volume `sqlserver_data` để lưu dữ liệu; schema vẫn cần được khởi tạo theo hướng dẫn trên.

## Kiểm thử

Các project kiểm thử chạy bằng chương trình console, không dùng `dotnet test`.

Chạy nghiệp vụ và HTTP tài khoản khách:

```powershell
.\scripts\Validate-CustomerAccounts.ps1
```

Chạy thêm tích hợp SQL sau khi đặt `MOCBAKERY_SQL_TEST_CONNECTION` cho server thử có quyền tạo cơ sở dữ liệu:

```powershell
.\scripts\Validate-CustomerAccounts.ps1 -IncludeSql
```

Bộ SQL tài khoản khách tạo database tạm riêng và dọn database vừa tạo khi kết thúc. Bộ HTTP mặc định dùng repository trong bộ nhớ. Log được lưu trong `artifacts/customer-accounts-*`.

Các bộ kiểm thử Web khác:

```powershell
dotnet run --project .\tests\PublicCatalog\MocBakery.PublicCatalogChecks.csproj -c Release
dotnet run --project .\tests\WebCart\MocBakery.WebCartChecks.csproj -c Release
```

Kiểm tra tổng hợp, bao gồm các bước tích hợp yêu cầu SQL LocalDB:

```powershell
.\scripts\Validate-Project.ps1
```

Thêm `-IncludeGui` để chạy kiểm thử form với fixture riêng. Hướng dẫn tại [kiểm thử hệ thống](tests/System/README.md). Workflow của repository nằm ở `../.github/workflows/mocbakery.yml`.

## Đóng gói Desktop

```powershell
.\scripts\Package-Release.ps1
```

Bản Windows x64 kèm runtime, tài liệu và script được tạo trong `artifacts`. Cấu hình kết nối được thiết lập riêng trên máy triển khai.

## Tài liệu

- [Hướng dẫn triển khai](docs/deployment/CHOT_DO_AN.md)
- [Khu quản trị Web](docs/web/Admin_Web.md)
- [Tài khoản khách hàng Web](docs/deployment/CustomerWebAccounts.md)
- [Luồng đơn đặt từ Web](docs/web/TiepNhanDon_Web.md)

