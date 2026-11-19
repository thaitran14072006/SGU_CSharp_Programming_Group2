# Ảnh sản phẩm và nền giao diện

Ngày cập nhật 19/11/2026. Bốn ảnh do Như cung cấp được đặt trong
`src/MocBakery.Web/wwwroot/images/bakery`:

| Bánh | Tệp |
| --- | --- |
| Cookie hoặc bánh quy | cookie.jpg |
| Muffin | muffin.jpg |
| Croissant hoặc bánh sừng bò | croissant.jpg |
| Tiramisu | tiramisu.jpg |

`BakeryDisplay.Photo` chọn ảnh tải từ desktop trước; khi bánh chưa có ảnh trong SQL,
Web dùng ảnh được cung cấp theo tên loại bánh. Bánh khác chưa được ghép ảnh vẫn có
thông báo thay thế. Áp dụng ở thực đơn và chi tiết. Các ảnh dự phòng nằm ở Web,
không ghi vào bảng SanPham và không thay đổi hợp đồng HasImage/ImageUrl của API.

Trang dùng nền kem ấm, phần giới thiệu nâu cacao và ảnh croissant làm điểm nhấn.
CSS điều chỉnh vị trí ảnh theo khuôn hiển thị; các tệp JPG nguồn được giữ nguyên.
Nút và luồng giỏ hàng của Mai vẫn được giữ.

Đã bổ sung viền champagne, bóng nhẹ, hiệu ứng ảnh/nút và khung nhập đồng bộ.
Trạng thái nhập sai vẫn giữ viền đỏ; người chọn giảm chuyển động không bị hiệu ứng.

Build Release đạt; bộ PublicCatalog kèm đơn Web đạt 58 ca, WebCart đạt 19 ca.
Trình duyệt đã kiểm tra 52 lượt trang tại 320, 375, 768, 1440 px, không cuộn ngang
hoặc lỗi JavaScript. Kiểm tra hình thức riêng đã đối chiếu 24 lượt trang cho hiệu ứng,
32 lượt trang và 88 khung nhập cho viền, focus, báo lỗi và chế độ giảm chuyển động.
Ảnh minh chứng trong `docs/web/evidence` được chụp khi giảm chuyển động để tránh
ảnh mờ giữa hiệu ứng; hiệu ứng hover đã được kiểm tra riêng trên trình duyệt.
