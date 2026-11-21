# Mai — Giỏ hàng và xác nhận yêu cầu mua Web

## Phạm vi và luồng

Khách xem chi tiết bánh → thêm số lượng vào giỏ → sửa/xóa → đăng nhập → xem lại giá → xác nhận yêu cầu.
Giỏ trống không đi tiếp; số lượng từ 1 đến 1000, tối đa 50 loại bánh. Thêm bánh trùng cộng số lượng.
Giá, thành tiền và khả năng đáp ứng đọc lại tại server. Nếu giá hoặc tình trạng thay đổi sau bước xem lại,
khách phải kiểm tra và xác nhận lại. Không nhận mã khách hoặc giá từ trình duyệt.

Giao diện dùng layout/CSS chung của Như. Trang giỏ chia danh sách và tổng yêu cầu thành hai cột;
màn hình hẹp chuyển một cột. Mọi thao tác ghi dùng POST và antiforgery.

## Các lớp

- View: `src/MocBakery.Web/Views/Cart/Index.cshtml`, `Review.cshtml`; nút thêm tại `Views/Catalog/Detail.cshtml`.
- Controller: `src/MocBakery.Web/Controllers/CartController.cs`.
- BLL: `src/MocBakery.BLL/Services/Sales/CartService.cs` gọi `PublicCatalogService.CheckSelection` của Như.
- DAL: `PublicCatalogRepository` đọc giá/tồn; `CustomerCartRepository` lưu giỏ khách trong SQL chung.
- Models: `src/MocBakery.Models/WebCart.cs`; view models tại `src/MocBakery.Web/Models/CartViewModels.cs`.

Khách chưa đăng nhập dùng giỏ session trong trình duyệt, hết hạn sau 30 phút không hoạt động.
Sau đăng nhập, giỏ được lưu trong `GioHangKhachHang` và `ChiTietGioHang` theo mã khách lấy từ cookie
đã xác thực. Giỏ vãng lai được gộp vào giỏ tài khoản khi đăng nhập thành công.
Đăng xuất xóa session xác nhận nhưng giữ giỏ SQL. Cùng tài khoản trên trình duyệt hoặc máy khác
thấy cùng giỏ sau khi tải lại trang; giỏ không tự mất khi Web khởi động lại.
Các máy phải chạy code mới và cùng kết nối một SQL Server/database. Giỏ chỉ lưu mã bánh và số lượng,
không giữ giá cũ, không giữ hoặc trừ kho. Người khác đăng nhập không thấy giỏ của tài khoản trước.

Chạy migration `database/schema/07_CustomerCart.sql` một lần trên database chung; có thể chạy lại.
Không cần chạy lại nếu đã tạo đúng hai bảng. Đọc giỏ trống chưa tạo dòng dữ liệu;
khách đăng nhập thêm bánh thì SQL mới có bản ghi.

Mỗi lần sửa tăng phiên bản giỏ. Nếu giỏ bị sửa ở trình duyệt khác sau màn xác nhận,
Web yêu cầu xem lại. Tạo đơn và xóa giỏ SQL thực hiện trong cùng transaction của WebOrderRepository;
lỗi ghi đơn giữ nguyên giỏ. Hai trình duyệt gửi cùng phiên bản chỉ một yêu cầu mới được tạo đơn.

## Ghép với Nam

Luồng hiện tại đã đăng ký WebOrderService và WebOrderRepository cho chức năng tạo đơn.
Phần dưới mô tả hợp đồng giữa giỏ và đơn; không viết thêm luồng ghi đơn riêng.

Nam triển khai `IWebOrderSubmission.Submit(WebPurchaseRequest)` và đăng ký DI trong Web:

```csharp
builder.Services.AddScoped<IWebOrderSubmission, TenDichVuCuaNam>();
```

Tên lớp trên là chỗ thay bằng implementation thật, không phải lớp đã có.
Đầu vào gồm CustomerId từ cookie đã xác thực, RequestKey do server tạo, mã bánh/số lượng,
và ConfirmedTotal đã kiểm tra ở server. Không nhận nhân viên tạo đơn từ trình duyệt.
Nam cần kiểm tra lại giá/tồn và danh tính hợp lệ khi ghi; không tin ConfirmedTotal như giá nguồn.
Đơn và chi tiết ghi trong một transaction. Nếu giá/tồn đổi, trả BusinessException để khách xem lại.
Khóa duy nhất trong SQL theo khách/RequestKey đảm bảo gửi đồng thời hoặc retry trả cùng mã đơn.
Giữ RequestKey khi refresh hoặc lỗi gửi; thành công mới xóa giỏ và phiên xác nhận.
Chặn gửi lặp tuần tự ở Controller đã có; tính đúng khi gửi đồng thời phải được Nam bảo đảm bằng SQL.
Gửi yêu cầu chưa tạo hóa đơn, chưa ghi doanh thu và chưa trừ tồn.

## Chạy và sử dụng

Tại thư mục MocBakery, cấu hình biến `MOCBAKERY_CONNECTION_STRING` tới CSDL đã nâng cấp của nhóm,
rồi chạy `dotnet run --project src/MocBakery.Web/MocBakery.Web.csproj`.
Mở http://localhost:5080 → Thực đơn → Chi tiết bánh → Thêm vào giỏ → Cập nhật/Xóa → Tiếp tục xác nhận.
Nếu chưa đăng nhập, đăng nhập bằng tài khoản khách rồi chọn Giỏ hàng → Tiếp tục xác nhận lần nữa.
Đăng nhập Web dùng tài khoản khách của Thái; không dùng tài khoản nhân viên Desktop.
Chưa kiểm tra ZeroTier hoặc ghi đơn thật trên SQL chung trong đợt này.

## Kiểm thử ngày 19/11/2026

Build toàn bộ Solution Release thành công, 0 cảnh báo và 0 lỗi. Bộ UnitChecks có 60 ca đạt;
WebAccounts có 50 ca HTTP đạt với repository bộ nhớ. Bộ WebCart có 18 kiểm tra nghiệp vụ và HTTP đạt:
giỏ trống, cộng dồn, số lượng âm/vượt giới hạn, thiếu tồn, xóa, bánh ngừng bán, giá mới,
form thêm bánh, antiforgery, giả giá, yêu cầu đăng nhập, mã xác nhận, giá đổi sau xem lại,
lỗi gửi giữ giỏ, đúng danh tính/tổng tiền, gửi lặp tuần tự, thành công xóa giỏ.

Chạy lại: `dotnet run --project tests/WebCart/MocBakery.WebCartChecks.csproj`.
Test HTTP dùng catalog, danh tính và dịch vụ đơn giả lập; không kết nối SQL, không xác nhận luồng Nam thật.
Project test đã thêm Solution và workflow CI chung. CI có build Release và các bộ test sẵn có;
chưa chạy GitHub Actions cho thay đổi local này.

Khi ghép Nam cần chạy thêm: tạo đơn thật nhận mã → Desktop thấy đơn → xử lý đúng trạng thái →
khách xem trạng thái mới; thử gửi đồng thời, rollback chi tiết, đổi giá ngay lúc ghi,
đồng thời đối chiếu chưa có hóa đơn/biến động tồn khi chỉ gửi yêu cầu.
