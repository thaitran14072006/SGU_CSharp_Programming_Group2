# Kiểm thử bản theo thiết kế Excel

Chuẩn màn hình: sheet 05, 06, 07, 12, 13 và phần Nhà cung cấp phụ trợ của workbook giao diện.
Chạy bộ nghiệp vụ mới bằng `MocBakery.CatalogInventoryChecks.dll --excel` trên LocalDB; mỗi lần tạo CSDL mới và tự dọn.
Các bộ 43/43/24 cũ vẫn chạy hồi quy. Ca giá 0 đã đổi sang từ chối theo ràng buộc giá > 0 của sheet Chi tiết bánh;
ca vai trò không hợp lệ cũ đổi chuỗi kiểm tra vì Kho hiện là vai trò hợp lệ theo Excel.

| Nhóm | Kịch bản cần kiểm chứng | Minh chứng |
| --- | --- | --- |
| Catalog | Tìm bằng mã/tên, danh mục ngừng không dùng cho bánh mới, giữ liên kết cũ, giá > 0, tên ≤ 100, đơn vị, shelf life, ảnh và ngưỡng tồn | excel-rules.txt; unit.txt |
| Quyền | Kho được ghi Inventory nhưng không sửa Catalog; Thu ngân/NhanVien cũ không được ghi kho; quyền bị kiểm tra lại trong transaction | excel-rules.txt; accounts-regression.txt |
| Nhà cung cấp | Điện thoại/email, ngừng hợp tác, chặn phiếu mới hoặc ghi sổ khi đã ngừng, giữ lịch sử | excel-rules.txt |
| Nháp | Phiếu ít nhất một dòng, lượng/ngày/đơn vị/giá hợp lệ, tổng tiền, không tăng tồn, phát hiện phiên bản cũ khi sửa đè | excel-rules.txt |
| Ghi sổ | Ghi toàn bộ dòng một lần; chặn ghi lại/sửa phiếu đã ghi; hai phiên chỉ một thành công | excel-rules.txt |
| Rollback | Dòng đầu tạo được nhưng dòng sau trùng lô: không còn lô/nhật ký dở, trạng thái vẫn Nhap | excel-rules.txt |
| Xuất | FEFO nhiều lô, số lẻ nguyên liệu, chặn vượt tồn, không dùng lô hết hạn cho sản xuất, hủy lấy lô hết hạn trước | excel-rules.txt |
| Kiểm kê | Lượng thực tế tăng hoặc bằng 0, giữ lượng nhập gốc, lưu số cũ/mới, người làm, thời điểm và lý do | excel-rules.txt |
| Cảnh báo | Tồn thấp theo tổng hợp lệ của mặt hàng; lô còn lượng và hạn trong 3 ngày; lô hết hạn | excel-rules.txt |
| Migration | CSDL 9 bảng gốc có dữ liệu → 15 bảng, giữ giá cũ/tồn; migration chạy lại không thay số dư | excel-rules.txt |
| Form SQL | Form bánh chặn tên rỗng; lưu các trường; form phiếu nhận bánh và nguyên liệu; nháp 120.000đ không tăng tồn; ghi sổ khóa sửa; trang kho đọc lại 3 hộp/2,5 Kg; Thu ngân không có nút ghi | form-sql.txt; form-sql/*.png |
| Bố cục | 5 màn hình chính, nhà cung cấp/nguyên liệu, danh sách phiếu và biến thể vai trò; rà ảnh với mẫu | layout/*.png |

Ảnh layout dùng dữ liệu minh họa. Bộ form-SQL phát sự kiện vào control trong message loop thật, dùng service/repository SQL thật;
thay phản hồi hộp xác nhận bằng callback để chạy tự động. Không mô tả chúng như thao tác chuột/bàn phím Windows của người dùng.
Chạy máy thứ hai, DPI hệ điều hành khác và tích hợp Sales/Orders vẫn cần nhóm kiểm tra sau khi gộp.
