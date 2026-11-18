# Bàn giao Web Catalog của Như

Ngày 18/11/2026. ASP.NET Core MVC theo lựa chọn của nhóm. Nhánh
`codex/nhu-public-catalog` dựa trên `feature/customer-web-20261118` của Thái tại
`8c7e7fc`, đã chứa main `2f6e06e`; dùng cùng project Web, Models, BLL, DAL và SQL desktop.

## Kết quả

Đã có trang chủ, danh mục, thực đơn tìm mã/tên, lọc danh mục/tình trạng, sắp tên/giá,
phân trang, chi tiết, ảnh, layout/menu/CSS chung và API đọc công khai.
Route `/` nay là trang chủ; route, cookie, biểu mẫu và CSS tài khoản của Thái giữ nguyên.
Không thêm migration hoặc thay quyền ghi desktop. Có [thiết kế](Nhu_18_11_2026_PhanTichVaThietKe.md),
[kết quả kiểm thử](Nhu_KetQuaKiemThu_2026_11_18.md), ảnh và phần viết chương 5, 6.

## Chạy với CSDL nhóm

Cần .NET 10 SDK, SQL Server và CSDL theo hướng dẫn chung. Schema 01 hiện tại đã có
`LoaiBanh.DangSuDung`, `SanPham.AnhBanh`. Chỉ chạy schema 01 trên CSDL mới; CSDL cũ dùng
script nâng cấp phù hợp. Migration 04 của Thái cần cho tài khoản khách, không cho Catalog.
Đặt cùng chuỗi kết nối desktop trong cửa sổ chạy Web, từ gốc repo:

```powershell
# Thay tên máy SQL thực tế, không đưa mật khẩu vào Git.
$env:MOCBAKERY_CONNECTION_STRING = 'Server=TEN_MAY_SQL;Database=QuanLyTiemBanh;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project .\MocBakery\src\MocBakery.Web\MocBakery.Web.csproj
```

Mở `http://localhost:5080`. Web đọc biến môi trường hoặc `ConnectionStrings:MocBakery`,
không tự đọc file cấu hình riêng desktop. Bánh kinh doanh và danh mục đang dùng mới hiện.
Không có lô hợp lệ thì hiện “Tạm hết bánh”. Thiếu kết nối trả thông báo 503 không lộ SQL.

## Kiểm tra riêng

```powershell
dotnet build .\MocBakery\MocBakery.sln -c Release
dotnet run --project .\MocBakery\tests\PublicCatalog\MocBakery.PublicCatalogChecks.csproj -c Release --no-build
```

Cần LocalDB Windows. Mỗi lượt tạo CSDL ngẫu nhiên `MocBakery_PublicTest_`, dọn đúng CSDL
đó khi kết thúc bình thường. Không dùng CSDL nhóm. Thêm `-- --serve` giữ Web fixture ở
`http://127.0.0.1:5088`; nếu buộc dừng, kiểm tra dọn CSDL thử còn sót.
Khi phiên này mở, môi trường có Playwright/Edge chạy
`node MocBakery/tests/PublicCatalog/browser-check.cjs` để kiểm tra responsive.
Đổi URL bằng `MOCBAKERY_PREVIEW_URL`. Ảnh và log ở `docs/web/evidence`.
Mai có thể nối các lệnh vào CI Windows có LocalDB; workflow chung chưa bị sửa.

## Hợp đồng tích hợp

Inject `PublicCatalogService` đã đăng ký DI. Gọi `CheckSelection(productId, quantity)`:
null nếu bánh mất/ẩn; `CurrentUnitPrice` là giá mới từ SQL; `CanFulfill` cho biết đủ lượng
tại thời điểm đọc, chưa giữ/trừ kho. Lượng 1 đến 1000; sai nhận `BusinessException`,
field `Quantity`. Mai đọc lại khi đổi lượng/xác nhận; Nam kiểm tra trong transaction ghi
đơn. Không tin giá browser gửi, không gán khách thành nhân viên.

| GET | Đầu ra |
| --- | --- |
| `/api/danh-muc` | id, name, productCount |
| `/api/banh` | items, total, page, pageSize, pageCount |
| `/api/banh/{id}` | PublicProduct hoặc 404 |
| `/banh/{id}/anh` | PNG/JPEG hoặc 404 |

Tham số: `q` tối đa 100 ký tự, `categoryId` dương,
`availability=all|in-stock|out-of-stock`, `sort=name|price-asc|price-desc`,
`page` từ 1, `pageSize` 1 đến 24 (mặc định 12). HTML 12 bánh/trang, home tối đa 6.
Sai input 400; vượt trang rỗng; lỗi dịch vụ 503. Danh mục thiếu trên HTML trả 404,
lọc API theo cùng ID trả rỗng.

PublicProduct: id, categoryId, code, name, category, unit, price, description,
hasImage, inStock, imageUrl. Không có số tồn, lô, giá vốn hay tài khoản.
Không trả `CatalogProductSnapshot` nội bộ ra HTTP. Kiểm tra lượng giỏ dùng BLL server.
Nối menu giỏ/đơn ở `Views/Shared/_Layout.cshtml` khi có route thật; CSS chung tại
`wwwroot/css/site.css`. Chưa hiện nút mua giả hoặc liên kết giỏ chưa có.

## Demo và giới hạn

Trên CSDL thử: tìm/lọc → chi tiết → chủ tiệm sửa giá desktop → tải lại Web → thêm/xóa
ảnh → ngừng bánh → URL cũ 404 → bật lại bánh và ngừng lô → Web ghi tạm hết.
Thử màn hình nhỏ và quyền nhân viên (không được ghi).

Lượt này kiểm chứng qua chính BLL/DAL desktop với SQL/HTTP thật, không bấm chuột
WinForms. Chưa nghiệm thu toàn luồng giỏ/đơn vì phần Mai/Nam chưa có trên nhánh nền.
Sau khi ghép, nhóm cần kiểm tra chọn bánh → giỏ → tạo đơn → xử lý desktop và nhờ
thành viên khác rà phần Như trước tổng hợp.
