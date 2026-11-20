# Khu quản trị Web Mộc Bakery

Ngày kiểm tra: 20/11/2026.

## Truy cập

Chạy project `src/MocBakery.Web` theo cấu hình kết nối trong hướng dẫn Web hiện có.
Trang mua hàng ở `/`; đăng nhập nhân viên ở `/admin/dang-nhap`; dashboard ở `/admin`.
Chân trang mua hàng có liên kết “Dành cho nhân viên”. Admin có liên kết mở cửa hàng.

Admin đọc cùng tài khoản nhân viên và dữ liệu nghiệp vụ với desktop. Không tạo tài khoản
chủ tiệm công khai hoặc mật khẩu mặc định. Khi cài mới, thiết lập chủ tiệm qua desktop.
CSDL hiện có cần schema nền hiện hành và các nâng cấp 04, 05, 06 cho tài khoản khách,
đơn Web và thông tin người nhận. Việc tách admin không cần thêm bảng hay migration mới.
Không chạy script tạo mới đè lên dữ liệu đang dùng.

## Chức năng và quyền

| Khu vực | Chủ tiệm | Thu ngân / Nhân viên | Kho |
| --- | --- | --- | --- |
| Dashboard | Doanh thu, hóa đơn, đơn, tồn | Đơn và tồn | Tồn |
| Bánh, danh mục | Thêm, sửa, ngừng dùng; xóa danh mục chưa dùng | Tra cứu | Tra cứu |
| Kho bánh | Nhập lô, ngừng/mở bán lô, tra tồn | Tra cứu | Tra cứu |
| Đơn và khách hàng | Xem, cập nhật, tiếp nhận/giao/hủy | Có | Không |
| Hóa đơn | Tra cứu chi tiết | Có | Không |
| Nhân viên và tài khoản | Thêm, sửa, khóa, cấp mật khẩu tạm | Không | Không |
| Báo cáo | Doanh thu theo ngày và bánh bán chạy theo khoảng chọn | Không | Không |

Kho chỉ đọc theo quyền BLL hiện hành. Không mở rộng quyền ghi khi đưa lên Web.
Các chỉ số dashboard lấy từ SQL: doanh thu/hóa đơn hôm nay, đơn mới, tồn còn hạn đang
bán, doanh thu 7 ngày, đơn sắp nhận, bánh bán chạy 30 ngày và cảnh báo tồn dưới 5.

## Luồng đơn hàng

Khách xác nhận họ tên, điện thoại, email và lịch nhận trước khi gửi. Nhân viên xem thông tin
người nhận tại thời điểm đặt, chi tiết bánh và ghi chú. Tiếp nhận ghi đúng nhân viên đang
xử lý, chuyển sang chuẩn bị. Giao đơn lập một hóa đơn và trừ kho FEFO qua BLL hiện có.
Gửi lại lệnh giao không lập hóa đơn hoặc trừ kho lần hai. Hủy bắt buộc lý do, không trừ
kho. Khách chỉ xem được đơn của mình và trạng thái mới.

## Phiên đăng nhập

Cookie khách và nhân viên dùng hai cơ chế xác thực riêng. Cookie nhân viên HttpOnly,
SameSite Strict, giới hạn đường dẫn `/admin`, hết hạn sau 30 phút. Vai trò và tình trạng
tài khoản được đọc lại mỗi yêu cầu; đổi mật khẩu, khóa hoặc đổi quyền có hiệu lực lên
phiên đang mở. Nhân viên có mật khẩu tạm buộc đổi trước khi dùng chức năng.
Đăng xuất dùng POST và thu hồi phiên phía máy chủ. Mọi lệnh ghi kiểm tra antiforgery.
Registry phiên nằm trong bộ nhớ một Web host; khởi động lại cần đăng nhập lại. Nếu triển
khai nhiều máy chủ cần kho phiên dùng chung. Môi trường production dùng HTTPS.

## Kiểm thử

Từ gốc repository:

```powershell
dotnet run --project MocBakery/tests/PublicCatalog/MocBakery.PublicCatalogChecks.csproj -c Release
dotnet run --project MocBakery/tests/WebAccounts/MocBakery.WebAccountChecks.csproj -c Release
dotnet run --project MocBakery/tests/WebCart/MocBakery.WebCartChecks.csproj -c Release
```

Kết quả: 90 ca Catalog/đơn/admin đạt với SQL LocalDB riêng; 52 ca tài khoản HTTP và
19 ca giỏ hàng đạt với repository thử. Có kiểm tra từ chối khách truy cập admin, quyền
nhân viên/kho, thao tác CRUD, số tiền lẻ trên Windows tiếng Việt, buộc đổi mật khẩu,
thu hồi phiên, tự khóa quản lý, nhận/giao/hủy và không trừ kho hai lần.

Trình duyệt Edge: 64 lượt trang và biểu mẫu admin, 52 lượt trang khách, tại các chiều
rộng 320, 375, 768, 1440 px; không tràn ngang hoặc lỗi JavaScript. Bộ kiểm tra giao diện
ở `tests/PublicCatalog/admin-browser-check.cjs` và `browser-check.cjs` (cần Playwright).

Kiểm tra lại điều hướng ngày 20/11: `tests/PublicCatalog/navigation-check.cjs` đạt 12
luồng trình duyệt thật. Có đăng nhập từ giỏ hàng rồi tiếp tục xác nhận, giữ đích đến khi
chuyển đăng ký/đăng nhập, quay về giỏ/cập nhật/xóa, quay lại sau khi đã đặt không tạo đơn
trùng, hủy sửa hồ sơ/mật khẩu, quay về tổng quan từ chín mục admin trên mobile/desktop,
quay lại/hủy sáu biểu mẫu quản trị, nhận và giao đơn rồi đối chiếu trạng thái phía khách,
và Back sau đăng xuất không mở lại nội dung cần đăng nhập. Trang đổi mật khẩu bắt buộc
có nút đăng xuất thay cho liên kết quay lại gây vòng lặp. Kết quả ở
`docs/web/evidence/navigation-result.json`. Đây là các luồng thực sự đã chạy, không phải
khẳng định đã kiểm tra mọi tổ hợp thao tác hoặc mọi môi trường trình duyệt.

Thêm `-- --serve` vào lệnh PublicCatalog để mở bản thử tại `http://127.0.0.1:5088`.
Tài khoản thử: `owner` / `PublicTest123!`; khách: `catalog.customer` / `CatalogTest123!`.
Đây là tài khoản chỉ sinh trong CSDL thử ngẫu nhiên, không dùng cho dữ liệu nhóm.
Nhấn Enter trong cửa sổ chạy để dừng và dọn đúng CSDL thử vừa tạo.
