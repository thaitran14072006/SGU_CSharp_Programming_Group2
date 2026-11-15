# Kiểm tra tích hợp phần Như vào main

Ngày 15/11/2026. Gộp `codex/nhu-catalog-inventory` tại `ccbd80e` vào main tại
`79b064a`, theo yêu cầu của Như. Bao gồm mã nguồn, migration, seed, kiểm thử,
báo cáo chương 5–6 và bằng chứng của phần được phân công.

Giữ Bán hàng, Hóa đơn, Đơn đặt, Tổng quan, Báo cáo và khung cửa sổ của main.
Nối thêm Phiếu nhập kho/Nhà cung cấp theo quyền, Danh mục bánh và xác nhận khi
rời trang có thay đổi chưa lưu. Loại bỏ các lớp MainForm/UiSmoke trùng tên;
giữ kiểm tra thay đổi số lượng giỏ hàng của nhóm trong bộ kiểm tra giao diện.

Kết quả trên bản gộp, Release, .NET SDK 10.0.401, SQL LocalDB:

| Bộ kiểm tra | Đạt | Không đạt |
| --- | ---: | ---: |
| Accounts/Customers và Sales FEFO | 45 | 0 |
| Catalog/Inventory | 43 | 0 |
| SQL tích hợp, Sales bán sản phẩm từ Catalog, backup/restore | 25 | 0 |
| Excel, migration, giao dịch kho và đồng thời | 25 | 0 |
| Sự kiện form với SQL thật, chạy ngoài màn hình | 7 | 0 |
| Tổng | 145 | 0 |

Build solution thành công: 0 lỗi, 0 cảnh báo. Xuất 13 ảnh bố cục và kiểm tra
số lượng/thành tiền giỏ hàng thành công. Ảnh bản gộp tại
`../evidence/main-merge-2026-11-15/`. Đây là kiểm tra tự động và ảnh preview;
chưa thay thế kiểm thử thủ công toàn bộ các quy trình của nhóm.

Các báo cáo và ảnh `nhu-excel-2026-11-15` ghi nhận bản nhánh trước khi tích hợp;
khung cửa sổ trong ảnh cũ khác khung main được giữ lại trong lần gộp này.
CSDL thử tích hợp được tạo riêng và tự dọn, không thay đổi CSDL nghiệp vụ.
