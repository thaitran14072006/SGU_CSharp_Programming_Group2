# Phần Như theo phân công Desktop và Web ngày 18 tháng 11 năm 2026

Nguồn phân công: `Phan_cong_MocBakery_Desktop_Web_4_thanh_vien.docx`, mục 5, 8 và 9.
Nhóm chọn ASP.NET Core MVC; dùng khung `MocBakery.Web` đã có trên nhánh của Thái,
dùng chung Models/BLL/DAL và SQL Server với desktop. Nhánh thực hiện: `codex/nhu-public-catalog`.

## Phạm vi và use case

| Mã | Người sử dụng | Luồng và kết quả |
| --- | --- | --- |
| WEB-NHU-01 | Khách vãng lai hoặc đã đăng nhập | Mở trang chủ, xem nhóm bánh và các bánh đang công khai, sang danh sách hoặc chi tiết |
| WEB-NHU-02 | Mọi khách | Xem danh mục đang sử dụng, chọn danh mục để lọc bánh |
| WEB-NHU-03 | Mọi khách | Tìm mã/tên, lọc danh mục, còn/hết hàng, sắp tên/giá, chuyển trang; trường hợp rỗng có lời giải thích và nút bỏ bộ lọc |
| WEB-NHU-04 | Mọi khách | Mở bánh theo mã ID; xem tên, loại, mô tả, đơn vị, giá, ảnh và khả năng đáp ứng; bánh không tồn tại/ngừng bán trả 404 |
| WEB-NHU-05 | BLL giỏ hàng của Mai | Đọc bánh hợp lệ và kiểm tra lại giá/khả năng đáp ứng theo lượng yêu cầu từ CSDL; không giữ/trừ kho hoặc ghi đơn |

## Quy tắc công khai

- Chỉ công khai bánh đang kinh doanh thuộc danh mục đang sử dụng. Danh mục ngừng dùng
  và bánh ngừng bán không xuất hiện trong danh sách, chi tiết, ảnh hoặc API công khai.
- Bánh đang kinh doanh nhưng không còn lô đủ điều kiện vẫn có trang riêng, hiển thị **Tạm hết bánh**.
  Không hiện nút gửi yêu cầu mua khi chưa có luồng giỏ hàng của Mai.
- Tồn đủ điều kiện dùng cùng quy tắc desktop: lô đang bán, còn lượng, đã sản xuất,
  hạn dùng chưa qua ngày nghiệp vụ SQL Server. Ngày hết hạn vẫn bán được trong ngày đó.
- DTO công khai chỉ có mã, tên, danh mục, đơn vị, giá, mô tả sản phẩm, URL ảnh và cờ còn hàng.
  Không có lượng tồn chính xác, mã lô, giá vốn, nguồn nhập, ngưỡng cảnh báo, NCC,
  ghi chú khách/nhân viên hoặc dữ liệu tài khoản.
- Không cache HTML/API/ảnh theo cách làm giá hoặc trạng thái cũ còn hiện sau khi tải lại.
  Ảnh chỉ phục vụ JPEG/PNG hợp lệ trong giới hạn 5 MB; thiếu ảnh có trạng thái thay thế trung thực.
- Mọi truy vấn SQL dùng tham số. Sort dùng danh sách cố định, ký tự LIKE trong từ khóa được escape.
  Đầu vào sai trả 400, không có bánh trả 404, SQL chưa sẵn sàng trả thông báo 503 không lộ chi tiết kết nối.
- Truy vấn công khai không mở rộng quyền của CatalogService/Inventory/Warehouse hiện có.
  Khách Web không trở thành nhân viên và không có endpoint ghi Catalog.

## Bố cục trước triển khai

Layout chung: liên kết bỏ qua điều hướng; logo Mộc; menu Trang chủ, Bánh, Danh mục;
liên kết tài khoản/đăng nhập của Thái; vùng nội dung; footer. Menu và nút có trạng thái focus.
Trên điện thoại, menu xuống dòng, ô tìm kiếm và nút tìm chiếm cả hàng;
các bộ lọc còn lại chia hai cột, không gây cuộn ngang toàn trang.

Trang chủ có tiêu đề, mô tả ngắn, liên kết xem bánh; danh mục dạng liên kết;
lưới bánh công khai và lối sang danh sách. Không bịa địa chỉ, giờ mở cửa hay chính sách giao hàng.
Danh sách có ô tìm, danh mục, trạng thái còn/hết, thứ tự sắp, tổng kết quả, lưới và phân trang.
Chi tiết có ảnh/tình trạng thiếu ảnh, breadcrumb, tên, mô tả, đơn vị, giá và trạng thái.

## Hợp đồng giữa các module

`PublicCatalogService` là lối đọc dùng chung. Controller chỉ trả `PublicProduct` và `PublicCategory`.
`CheckSelection(id, quantity)` dành cho BLL giỏ hàng: đọc lại bánh và trả giá hiện tại cùng
cờ có thể đáp ứng lượng yêu cầu. Mai cần gọi lại khi xác nhận và Nam vẫn phải kiểm tra
trong transaction ghi/xử lý đơn. Kết quả đọc không phải giữ chỗ hoặc cam kết tồn về sau.

Giỏ hàng, xác nhận mua và ghi đơn thuộc Mai/Nam; không tạo endpoint ghi đơn thay họ.
Nhân diện khách dùng cookie và quy ước của Thái; phần Catalog không cần đăng nhập để xem.
Layout cung cấp vị trí điều hướng để các thành viên tự nối trang của mình sau khi có route.

## Dữ liệu và bàn giao

Không cần thay đổi CSDL cho Catalog công khai, đọc các bảng hiện hành `SanPham`, `LoaiBanh`, `LoBanh`.
Không tự chạy migration trên server dùng chung. Test SQL tạo database riêng tại LocalDB.
Schema 04 thuộc Thái và chỉ cần cho tài khoản khách, không phải điều kiện đọc Catalog.

Tiêu chí nghiệm thu: có test mới cho BLL, HTTP, SQL và responsive; có minh chứng cùng dữ liệu
desktop thay giá/ảnh/trạng thái thì Web đổi khi tải lại. Kết quả cũ ngày 15/11 chỉ là lịch sử,
không dùng thay kết quả đợt Web này. Kiểm tra chéo toàn luồng giỏ của Mai chỉ xác nhận
khi module đó thực sự có mặt; bàn giao hợp đồng và fixture để phát triển độc lập trước.
