# Nhật ký Sales - Nguyễn Thị Trúc Mai

## 2026-11-19 — giỏ hàng Web
- Thêm giỏ session, sửa/xóa số lượng, tổng tiền server và trang xác nhận dùng tài khoản khách.
- Dùng dữ liệu PublicCatalog của Như, kiểm tra giá/tình trạng lại trước gửi; giữ điểm kết nối dịch vụ tạo đơn của Nam.
- 18 kiểm tra nghiệp vụ/HTTP đạt với dữ liệu giả lập, chưa kiểm tra SQL chung hoặc dịch vụ tạo đơn thật.
- Thêm test vào Solution và CI. Chi tiết sử dụng, phân tích và bàn giao: [giỏ hàng Web](../web/Mai_GioHang.md).

## 2026-11-15
- Làm: viết Entities HoaDon, ChiTietHoaDon, LoBanh (Models/Entities); 
- Kiểm tra: chưa build được, báo lỗi MSB1003 vì repo chưa có Solution/.csproj
- Commit: f2af9c7881de041a5c121bbbce5cf32ad31f4090
## 2026-11-15 — triển khai Sales
- Dùng Solution và schema chung sau khi ghép Accounts/Customers.
- Thêm giỏ hàng, thanh toán FEFO, ghi hóa đơn và tồn trong một transaction, tra cứu hóa đơn.
- Build thành công, 45 kiểm thử đơn vị đạt. Đã kiểm tra KT06–KT09 ở mức Repository trên SQL Server Express.
- Chi tiết: [kết quả kiểm thử Sales](../testing/results/2026-11-15-sales.md).
