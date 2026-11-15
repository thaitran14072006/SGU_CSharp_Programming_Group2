# Nhật ký Sales - Nguyễn Thị Trúc Mai

## 2026-11-15
- Làm: viết Entities HoaDon, ChiTietHoaDon, LoBanh (Models/Entities); 
- Kiểm tra: chưa build được, báo lỗi MSB1003 vì repo chưa có Solution/.csproj
- Commit: f2af9c7881de041a5c121bbbce5cf32ad31f4090
## 2026-11-15 — triển khai Sales
- Dùng Solution và schema chung sau khi ghép Accounts/Customers.
- Thêm giỏ hàng, thanh toán FEFO, ghi hóa đơn và tồn trong một transaction, tra cứu hóa đơn.
- Build thành công, 45 kiểm thử đơn vị đạt. Đã kiểm tra KT06–KT09 ở mức Repository trên SQL Server Express.
- Chi tiết: [kết quả kiểm thử Sales](../testing/results/2026-11-15-sales.md).
