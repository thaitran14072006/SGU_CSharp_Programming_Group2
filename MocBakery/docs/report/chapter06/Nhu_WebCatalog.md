# Phần Như bổ sung chương 6 kiểm thử Web Catalog

Ngày 18/11/2026, chương trình console tạo CSDL LocalDB riêng, khởi động Kestrel, gửi HTTP
và đối chiếu BLL/DAL desktop. Không sửa CSDL nhóm. CSDL thử được dọn khi kết thúc bình thường.

36 ca mới đạt, bao phủ công khai, tìm/lọc, phân trang, input sai, ảnh, tồn, quyền và
hợp đồng giỏ. Fixture có lô hết hạn/ngừng bán/sản xuất tương lai; chỉ lô hợp lệ tính tồn,
bao gồm ngày hết hạn. Chủ tiệm sửa giá 45.000 thành 49.000, thêm/xóa ảnh và ngừng bánh/
danh mục/lô được HTTP phản ánh sau tải lại. Nhân viên không sửa giá qua BLL; API không
trả tồn hoặc tài khoản nội bộ.

Hồi quy đạt 45 ca Unit, 43 CatalogInventory, 36 CustomerAccounts và 23 WebAccounts.
Tổng năm bộ console 183 ca đạt. Solution Release 0 lỗi, 0 cảnh báo.
Edge qua Playwright kiểm tra sáu trang ở 320, 375, 768, 1440 px: 24 lượt không cuộn
ngang hoặc lỗi JavaScript. Tìm/sắp/mở chi tiết trên điện thoại, mã hóa script và liên
kết bỏ qua menu bằng bàn phím đạt. Bốn ảnh giao diện đã lưu và xem lại.

Log, ảnh và hướng dẫn chạy tại `docs/web`. Lượt này kiểm chứng đồng bộ qua BLL/DAL SQL
thật, không bấm GUI WinForms hoặc thử tải lớn/Internet. Sau khi ghép giỏ/đơn Mai/Nam,
nhóm cần kiểm tra toàn luồng chọn bánh đến xử lý đơn desktop và kiểm tra chéo. Kết quả
GUI ngày 15/11 được giữ là lịch sử riêng, không dùng thay ca Web mới.
