# Ca kiểm thử Catalog và Inventory

Người phụ trách: Phạm Quỳnh Như. Người thực thi tự động: Codex theo yêu cầu của Như.
Dữ liệu nghiệp vụ unit được cố định tại 14/11/2026; ca SQL dùng ngày local khi chạy và CSDL mới có tiền tố MocBakery_NhuTest_.
Mỗi ca tự động in PASS/FAIL riêng; kết quả thực tế nằm trong các log ở `../evidence/` và bảng tổng hợp `../results/2026-11-14-nhu.md`.

## Ca đã tự động hóa

| Mã | Mức | Tiền điều kiện và bước chạy | Mong đợi | Minh chứng |
| --- | --- | --- | --- | --- |
| NH-C01 | Unit + SQL | Chủ tiệm thêm/sửa loại, tên có khoảng trắng và Unicode | Chuẩn hóa tên, lưu và đọc lại đúng | nhu-unit.txt; nhu-integration.txt |
| NH-C02 | Unit + SQL | Thêm tên loại rỗng/trùng; gửi trùng trực tiếp xuống DAL | BLL chặn rỗng; unique constraint chặn trùng cả tại DAL | Hai log trên |
| NH-C03 | Unit + SQL | Xóa loại đang được sản phẩm dùng, rồi xóa một loại trống | Chặn loại được dùng; xóa loại trống thành công | Hai log trên |
| NH-C04 | Unit + SQL | Thêm/sửa sản phẩm; giá 0, âm, vượt độ dài hoặc quá 2 số lẻ | Giá 0 hợp lệ; dữ liệu sai bị chặn; dữ liệu SQL cũ không đổi | Hai log trên; KT04 |
| NH-C05 | Unit + SQL | Tên sản phẩm trùng, loại không tồn tại, đơn vị rỗng | Thông báo trường sai; không lưu dữ liệu sai | Hai log trên |
| NH-C06 | Unit + SQL | Đổi sản phẩm có lô sang ngừng kinh doanh | Giữ tất cả lô; tồn có thể bán bằng 0 | Hai log trên |
| NH-I01 | Unit + SQL | Nhập lô đúng; số lượng 0/âm; hạn trước ngày sản xuất; ngày SX tương lai | Lô đúng có lượng còn bằng lượng nhập; trường sai bị chặn | Hai log trên; KT04 |
| NH-I02 | Unit + SQL | Nhập hai lô 4+5 bánh còn hạn, lô 3 bánh hết hạn và 2 bánh ngừng bán | Tồn vật lý 14, có thể bán 9; loại lô hết hạn/ngừng bán | nhu-integration.txt dòng EVIDENCE; KT05 |
| NH-I03 | Unit + SQL | Lô có hạn đúng hôm nay; chuyển ngày nghiệp vụ sang hôm sau | Còn bán hôm nay, không bán hôm sau | Hai log trên |
| NH-I04 | Unit + SQL | Tạo lô xa hạn trước, lô gần hạn sau; lấy danh sách cho Sales | Gần hạn trước, mã lô làm tiêu chí phụ khi trùng hạn | Hai log trên |
| NH-I05 | Unit + SQL | Ngừng/cho bán lại lô, gồm lô đã hết hạn | Lô còn hạn đổi tồn đúng; lô hết hạn không được bán lại | Hai log trên |
| NH-I06 | Unit + SQL | Gọi nhập lô cho sản phẩm ngừng kinh doanh, cả qua DAL | Bị từ chối, không có lô mới | Hai log trên |
| NH-I07 | Unit | Hai lô có int.MaxValue bánh | Tổng dùng 64-bit, không tràn Int32 | nhu-unit.txt |
| NH-P01 | Unit + SQL | Nhân viên gọi trực tiếp các hàm ghi; giả vai trò; thu hồi phiên/quyền | BLL từ chối; tra cứu hợp lệ vẫn được phép | Hai log trên |
| NH-S01 | SQL | Lấy lô bằng overload transaction rồi chạy tiếp truy vấn và rollback | Hàm không commit transaction của caller | nhu-integration.txt |
| NH-S02 | SQL | Chạy seed, đổi lượng còn, chạy seed lần hai | Không sinh thêm lô hoặc reset tồn | nhu-integration.txt |
| NH-S03 | SQL | Backup CSDL thử, restore sang tên mới, so sản phẩm/lô | Dữ liệu khôi phục khớp từng record | nhu-integration.txt |
| NH-R01 | Regression | Chạy bộ Accounts/Customers của Thái | 43 ca cũ không bị ảnh hưởng | nhu-regression-accounts-customers.txt |

## Ca còn cần thao tác và tích hợp nhóm

Trạng thái ban đầu là Chưa chạy. Ngày 15/11 đã bổ sung các kịch bản GUI với SQL thật;
phạm vi đạt và giới hạn nằm trong `../results/2026-11-15-nhu-gui.md`. Không tính các nhóm này vào tỷ lệ 110 ca tự động.

| Mã | Bước kiểm tra | Mong đợi |
| --- | --- | --- |
| NH-M01 | Đăng nhập bằng tài khoản thật, thêm loại → sản phẩm → nhập lô qua các hộp thoại | Lưu được, tải lại đúng SQL; ảnh giao diện và truy vấn cùng bản dữ liệu |
| NH-M02 | Nhập trường sai qua hộp thoại và bấm Lưu | ErrorProvider đúng trường, dữ liệu không bị ghi |
| NH-M03 | Mở trên màn hình nhỏ/DPI 125–150%, dùng bàn phím | Có thể đọc và thao tác, không mất nút bắt buộc |
| NH-M04 | Rút kết nối SQL khi đang tra cứu/ghi, nối lại và tải lại | Thông báo dễ hiểu, không ghi dở, có thể tiếp tục |
| NH-M05 | Như kiểm thử chéo Sales của Mai: KT06–KT09 | FEFO 4+2, chặn vượt tồn, rollback và hai phiên không bán âm |
| NH-M06 | Chạy bản publish trên máy thứ hai: KT14–KT15, bán và giao đơn rồi khởi động lại | Cấu hình, quyền, tồn và doanh thu nhất quán; khôi phục được |

Ảnh ở `nhu-ui-preview/` dùng dữ liệu bố cục minh họa, không phải bằng chứng đã chạy NH-M01 hay NH-M05.

| Nhóm | Cập nhật ngày 15/11/2026 |
| --- | --- |
| NH-M01 | Đã chạy thêm loại → sản phẩm → nhập lô với GUI/SQL thật; sửa giá và kiểm chứng sau restart. Chưa kiểm tra hộp thoại đăng nhập người dùng và mọi thao tác sửa/xóa |
| NH-M02 | Đã xác nhận trường tên loại rỗng bị chặn đúng ô; các biến thể GUI khác chưa chạy đầy đủ |
| NH-M03 | Chưa kiểm tra thay đổi DPI hệ điều hành |
| NH-M04 | Đạt luồng đọc: ngắt riêng CSDL thử, tải lại có cảnh báo, nối lại tải được và tồn không đổi; lỗi giữa luồng ghi chưa chạy |
| NH-M05 | Chưa chạy do Sales chưa có trên remote |
| NH-M06 | Chưa có máy thứ hai; restart cùng máy đã kiểm tra, không thay thế ca này |

Ảnh thật nằm ở `nhu-gui-2026-11-15/`, tách riêng ảnh preview.
