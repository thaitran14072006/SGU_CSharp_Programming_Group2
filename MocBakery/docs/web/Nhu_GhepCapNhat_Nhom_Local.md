# Giải thích phần Như và cập nhật nhóm trên máy

## Nhóm vừa có gì

Thái thêm trang sửa hồ sơ khách hàng ở commit `1cc547f` trên nhánh
`feature/customer-web-20261118`. Có sửa họ tên, số điện thoại, địa chỉ, kiểm tra dữ liệu,
giữ đúng danh tính đăng nhập và thông báo lưu thành công. Sau đó nhóm thêm đổi mật khẩu
và kiểm thử SQL tài khoản. Phần xem bánh của Như đã ghép vào main tại `2f57cb5`.

## Phần Như làm tiếp

Ghép code Thái vào bản làm việc Như trên máy. Trang chủ được thêm liên kết từng danh mục
kèm số loại bánh, giúp khách bấm trực tiếp để lọc. CSS chung ẩn vùng tổng kết lỗi khi
biểu mẫu hợp lệ; trước đó trang hồ sơ hiện chấm đỏ rỗng do style ẩn mặc định bị chính
sách Web chặn. Không đổi nghiệp vụ hoặc trường dữ liệu hồ sơ của Thái.

## Hiểu phần của mình

Như phụ trách quản lý danh mục và kho trên desktop, đồng thời phụ trách trang chủ,
danh mục, tìm/lọc, chi tiết bánh và giao diện chung trên Web. Thái phụ trách tài khoản;
Mai/Nam phụ trách giỏ và đơn. Khi khách bấm tìm bánh:

1. **View** là trang khách nhìn thấy, có ô tìm, bộ lọc, giá và ảnh.
2. **Controller** nhận từ khóa/danh mục rồi gọi service.
3. **BLL** kiểm tra dữ liệu, chỉ cho xem bánh hợp lệ, chuẩn bị dữ liệu công khai.
4. **DAL** gửi truy vấn có tham số tới SQL; kết quả đi ngược lại để hiện trên View.

Các tệp chính: `Views/Catalog/Browse.cshtml`, `Controllers/CatalogController.cs`,
`Services/Catalog/PublicCatalogService.cs` trong BLL, `PublicCatalogRepository.cs` trong
DAL. `Views/Shared/_Layout.cshtml` là menu/khung chung; `wwwroot/css/site.css` chỉnh hình thức.

Hai ứng dụng dùng cùng SQL nên chủ tiệm sửa giá/ảnh/trạng thái ở desktop, tải lại Web
sẽ đọc dữ liệu mới. Web khách không hiện số tồn nội bộ, chỉ hiện còn/tạm hết bánh.
Code kiểm tra lượng để Mai gọi lại lúc xác nhận không có nghĩa đã giữ kho hoặc tạo đơn.

## Kiểm tra đợt ghép

- Solution Release build thành công, 0 lỗi và 0 cảnh báo.
- 39 ca PublicCatalog đạt, có SQL thật cho đăng ký/đăng nhập, chuyển sang thực đơn,
  sửa hồ sơ rồi mở chi tiết vẫn giữ phiên và thấy giá đúng.
- Bản ghép ban đầu đạt 35 ca WebAccounts. Khi ghép main có thêm đổi mật khẩu,
  bộ WebAccounts đạt 50 ca; cùng 39 ca PublicCatalog là 89 ca ở hai bộ.
- Edge kiểm tra 36 lượt trang trên bốn độ rộng 320, 375, 768, 1440 px, gồm trang tài
  khoản và sửa hồ sơ; không cuộn ngang hoặc lỗi JavaScript. Thao tác thật chọn danh mục,
  đăng nhập, sửa hồ sơ và về thực đơn đạt. Kiểm tra ẩn thông báo lỗi rỗng cũng đạt.
- Log SQL/HTTP mới: `evidence/public-catalog-integration-local.txt`;
  kết quả trình duyệt: `evidence/browser-result.json`; ảnh hồ sơ:
  `evidence/profile-375.png`, `evidence/profile-1440.png`.

Chỉ dùng LocalDB riêng, không sửa CSDL nhóm. Phần giao diện đã có trên main;
kiểm thử bổ sung cùng tài liệu và ảnh được bàn giao qua hai commit tiếp theo.
Toàn luồng giỏ/đơn vẫn cần phần Mai/Nam trước khi nhóm nghiệm thu chung.
