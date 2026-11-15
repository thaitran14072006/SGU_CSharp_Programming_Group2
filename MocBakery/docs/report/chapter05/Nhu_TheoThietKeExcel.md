# Chương 5 phần Như theo thiết kế Excel

## Triển khai

Catalog và Inventory được cập nhật theo các màn hình 05, 06, 07, 12 và 13 của thiết kế Excel.
Giao diện sử dụng sidebar trắng, nền kem, điểm nhấn nâu/cam, tiêu đề và breadcrumb,
bảng sáng với trạng thái màu và vùng thao tác dưới theo mẫu.
Trang Chi tiết bánh là form hai cột trong vùng nội dung; Phiếu nhập có chi tiết nhiều dòng, tổng tiền và trạng thái nháp/đã ghi sổ.

Catalog hỗ trợ mã/tên, bộ lọc danh mục/trạng thái, giá và tồn bán, ảnh bánh, hạn dùng mặc định,
đơn vị tính, ngừng bán sản phẩm và ngừng dùng danh mục. Dữ liệu lịch sử được giữ.
Inventory mở rộng sang nguyên liệu, cảnh báo tồn thấp/hạn gần, xuất FEFO và kiểm kê có nhật ký.
Phiếu nháp không tăng tồn; ghi sổ chạy trong transaction chung cho tất cả dòng và trạng thái phiếu.
Khóa và phiên bản phiếu kiểm soát ghi sổ trùng/sửa đè; lỗi giữa các dòng rollback toàn bộ.

CSDL mở rộng từ 9 lên 15 bảng, thêm nhà cung cấp, nguyên liệu, lô nguyên liệu, phiếu/chi tiết nhập và biến động kho.
Migration không xóa dữ liệu cũ, giữ các khóa sản phẩm/lô/hóa đơn để phối hợp Sales.
Vai trò Quản lý, Thu ngân và Kho dùng kiểm tra quyền ở BLL; tài khoản NhanVien cũ vẫn được hỗ trợ để đọc dữ liệu.

## Kiểm thử và đánh giá

Build Release thành công, không cảnh báo/lỗi. Môi trường kiểm tra là Windows, .NET SDK 10.0.401 và SQL LocalDB.

| Bộ kiểm tra | Đạt | Không đạt |
| --- | ---: | ---: |
| Accounts/Customers hồi quy | 43 | 0 |
| Catalog/Inventory nghiệp vụ | 43 | 0 |
| SQL hồi quy, seed và backup/restore | 24 | 0 |
| Quy tắc Excel, ghi sổ/rollback/concurrency và nâng cấp CSDL cũ | 25 | 0 |
| Luồng form WinForms với SQL thật | 7 | 0 |
| Tổng | 142 | 0 |

Tỷ lệ trên các ca đã chạy là 142/(142+0) × 100% = 100%.
Không tính 11 ảnh bố cục thành ca nghiệp vụ và không suy rộng tỷ lệ này sang toàn hệ thống của nhóm.
Ảnh bố cục dùng dữ liệu minh họa; ảnh form-sql dùng các record được lưu thật trên fixture SQL.
Ca form được phát sự kiện bằng bộ kiểm tra, có message loop, và callback trả lời xác nhận; chưa thay thế kiểm tra thao tác tay hay máy thứ hai.

Các ca tiêu biểu chứng minh phiếu 552.000đ giữ tồn khi nháp, chỉ ghi một lần khi hai phiên cùng ghi,
lỗi ở dòng thứ hai không để lại lô/nhật ký của dòng đầu, xuất nguyên liệu lấy lô gần hạn trước,
và kiểm kê 18 → 25,125 Kg giữ lượng nhập gốc cùng nhật ký người/lý do.
Ca nâng cấp tạo đúng CSDL 9 bảng gốc với bánh có giá 0 và tồn 2, chạy migration rồi xác nhận nguyên trạng dữ liệu đó.

Mã ca, log, môi trường và giới hạn đầy đủ tại `docs/testing/results/2026-11-15-excel-design.md`.
Chưa ghép Sales/Orders mới từ main theo chỉ đạo ưu tiên phần Như; chưa có kiểm thử toàn hệ thống trên máy thứ hai hoặc kết luận hiệu năng dữ liệu lớn.
