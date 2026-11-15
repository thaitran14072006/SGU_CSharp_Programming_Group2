# Chương 6 phần Như theo thiết kế Excel

## Kết luận

Phần Như đã chuyển sang các màn hình theo Excel, có danh mục/sản phẩm, ảnh/hạn dùng mặc định,
tồn bánh và nguyên liệu, phiếu nhập nhiều dòng với nháp/ghi sổ và điều chỉnh tồn có lý do.
Quyền thay đổi kho được kiểm tra tại BLL và trong transaction; dữ liệu chứng từ và lượng nhập gốc được giữ lại.
Mã nguồn, migration, dữ liệu demo, hướng dẫn sử dụng, log kiểm thử và ảnh giao diện được bàn giao cùng nhánh riêng.

## Hạn chế

- Chưa gộp/chạy Sales, Orders và Reports mới của nhóm trong bản thay đổi này.
- Chưa chạy trên máy thứ hai, SQL Server Express 2022 riêng hoặc mọi mức DPI.
- Ảnh/layout và kiểm tra form-SQL là kiểm tra tự động trong tiến trình; cần nhóm rà thao tác bằng chuột/bàn phím trên máy sử dụng thực tế.
- Kho và danh mục hiện tải danh sách để lọc/tổng hợp trong bộ nhớ; chưa đo hiệu năng dữ liệu lớn.
- Cảnh báo sắp hết hạn hiện dùng mốc 3 ngày; chưa có màn hình cấu hình mốc tập trung hoặc tác vụ thông báo chủ động.
- Đơn vị được lấy từ mặt hàng, không có quy đổi Kg/Gram/Lít. Chưa có định mức nên không tự trừ nguyên liệu khi bán bánh.
- Kiểm kê và xuất có nhật ký; chưa có quy trình duyệt điều chỉnh nhiều cấp hoặc hủy chứng từ đã ghi sổ.

## Hướng phát triển

Ưu tiên tích hợp Sales/Orders với quy tắc ngày, vai trò ThuNgan và các transaction dùng chung;
kiểm thử đối chiếu tồn/nguyên liệu và hóa đơn sau các luồng đó. Hoàn thành chạy máy thứ hai và rà DPI/thao tác tay.
Tiếp theo bổ sung phân trang/lọc tại SQL và đo trên bộ dữ liệu xác định trước khi tối ưu.
Nếu cần tự động trừ nguyên liệu, nhóm phải bổ sung định mức, quy đổi đơn vị và nghiệp vụ sản xuất rõ ràng thay vì suy ra từ bán bánh.
