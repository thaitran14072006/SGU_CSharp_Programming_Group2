# Kết quả bản Catalog và Inventory theo thiết kế Excel

Ngày thực hiện 15/11/2026, múi giờ Asia/Saigon. Người thực thi: Codex theo yêu cầu của Như.
Mã triển khai và kiểm thử: `b94077dd1c2d38934203b6173e32be79d23465f5` trên `codex/nhu-catalog-inventory`.
Commit hồ sơ tiếp theo chỉ bổ sung kết quả/log/ảnh, không sửa mã chức năng.

Chuẩn đối chiếu: `Thiet_ke_giao_dien_quan_ly_tiem_banh.xlsx`, bản 1.0 ngày 25/10/2026;
SHA256 `3A44AFC868AAD38AFE2B5C3B07E181D1CD19783FA5470BF1C9905081E6985922`.
Đã đọc cả bảng thành phần, quy tắc và ảnh nhúng ở các sheet 05, 06, 07, 12, 13; dùng sheet 14 để bổ trợ nhà cung cấp.

## Đối chiếu yêu cầu với phần Như

| Sheet | Đã hiện thực trong bản mới |
| --- | --- |
| 05 Quản lý bánh | Bố cục theo mẫu, tìm mã/tên, danh mục/trạng thái, bảng giá/tồn, thêm/sửa/chi tiết và xác nhận ngừng/bán lại |
| 06 Chi tiết bánh | Trang hai cột, mã tự sinh chỉ đọc, tên tối đa 100, giá > 0, đơn vị Cái/Hộp/Ổ, hạn dùng mặc định, ảnh JPG/PNG, trạng thái, lưu/hủy có kiểm tra thay đổi |
| 07 Danh mục bánh | Tên/mô tả, chống trùng, số loại bánh, trạng thái, ngừng/dùng lại và loại khỏi lựa chọn tạo bánh mới; giữ liên kết lịch sử |
| 12 Tồn kho/hạn dùng | Bánh/nguyên liệu, tìm tên/mã lô, cảnh báo thấp/sắp hết hạn/hết hạn, xuất FEFO, chặn vượt tồn và kiểm kê có số cũ/mới/người/lý do |
| 13 Phiếu nhập | Nhà cung cấp, ngày nhập, nhiều dòng hàng/lô/NSX/HSD/lượng/đơn vị/giá, tổng tiền, nháp chưa tăng tồn, ghi sổ một lần và khóa sửa |
| Phụ trợ | Nhà cung cấp, nguyên liệu, danh sách phiếu, vai trò Kho/ThuNgan, migration từ dữ liệu cũ |

Giao diện là WinForms thật, giữ thanh tiêu đề cửa sổ của Windows. Ảnh layout dùng dữ liệu minh họa và ngày hiện tại,
không giả định mọi pixel/giá trị ngày giống ảnh mô phỏng trong Excel. Có thêm các thao tác cần cho ứng dụng chạy:
danh sách phiếu, chọn ảnh, ngưỡng tồn, nút điều chỉnh và quản lý nguyên liệu.

## Kết quả kiểm tra

Windows, .NET SDK 10.0.401, Release, SQL LocalDB. CSDL thử và CSDL demo được tạo riêng, không sửa CSDL nghiệp vụ có sẵn.

| Bộ kiểm tra | Đạt | Không đạt | Log |
| --- | ---: | ---: | --- |
| Nghiệp vụ Catalog/Inventory | 43 | 0 | unit.txt |
| Hồi quy Accounts/Customers | 43 | 0 | accounts-regression.txt |
| Tích hợp SQL/seed/backup-restore cũ | 24 | 0 | integration.txt |
| Nghiệp vụ Excel, transaction và migration | 25 | 0 | excel-rules.txt |
| Luồng form với SQL thật | 7 | 0 | form-sql.txt |
| Tổng ca đã chạy | 142 | 0 | ../evidence/nhu-excel-2026-11-15/ |

Build Release 0 warning/0 error và publish thành công. Đã rà 11 ảnh layout, gồm vai trò quản lý, kho và thu ngân;
không cộng số ảnh vào 142 ca. Tỷ lệ trên ca đã chạy 142/(142+0) × 100% = 100%, không phải tỷ lệ kiểm thử toàn hệ thống.

### Các kết quả quan trọng

- Phiếu nhiều dòng 552.000đ lưu nháp không đổi tồn; ghi sổ tạo tồn bánh và nguyên liệu, ghi nhật ký và chuyển trạng thái trong cùng transaction.
- Ghi lại bị từ chối; hai phiên đồng thời chỉ một thành công. Lỗi dòng thứ hai rollback cả lô/nhật ký của dòng đầu.
- Phiếu đã ghi sổ không sửa được; phiên bản cũ của phiếu nháp không ghi đè dữ liệu người khác vừa lưu.
- Xuất nguyên liệu FEFO chia đúng qua hai lô, hỗ trợ lượng lẻ, chặn vượt tồn. Lô hết hạn không dùng sản xuất nhưng có thể xuất hủy.
- Kiểm kê 18 → 25,125 Kg giữ lượng nhập gốc 20 và lưu người/lý do; kiểm kê bánh có thể tăng hoặc về 0.
- Migration chạy lại không mất số dư. Ca legacy tạo đúng 9 bảng gốc, giữ bánh giá 0 và tồn 2 sau nâng cấp thành 15 bảng.
- Bộ form tạo sản phẩm 42.000đ/5 ngày/đơn vị Hộp, thêm phiếu 3 hộp × 20.000 và 2,5 Kg × 24.000 = 120.000đ.
  Nháp không tăng tồn; ghi sổ tăng đúng 3 hộp/2,5 Kg và khóa sửa. Thu ngân không có nút ghi kho.

### Phân biệt bằng chứng giao diện

`layout/*.png` là ảnh render control với giá trị minh họa để so bố cục với Excel.
`form-sql/*.png` ghi nhận control trong quá trình tự động phát sự kiện WinForms với SQL thật và message loop như ứng dụng.
Các phản hồi xác nhận trong bộ form được cấp bởi callback kiểm thử, không phải thao tác chuột/bàn phím Windows của người dùng.
Không tự động hóa màn hình đăng nhập cá nhân và không tạo mật khẩu mặc định cho bản phát hành.

## Bản mở thử và giới hạn

Bản `artifacts/publish` trỏ đến `MocBakery_ExcelDemo_bfe676ec4c`, CSDL mới có dữ liệu đầu kỳ theo mẫu và chưa có tài khoản.
Mở ứng dụng, chọn Thiết lập lần đầu để tự tạo chủ tiệm. Script seed demo chạy lại vẫn giữ 5 sản phẩm, 5 lô bánh tồn 103
và 2 lô nguyên liệu tồn 20 Kg; log `demo-seed.txt` ghi lần xác nhận. Đây là dữ liệu minh họa, không phải chứng từ thật.

Chưa gộp code nhóm từ main theo yêu cầu ưu tiên chỉnh phần Như. Chưa thử Sales/Orders mới với vai trò ThuNgan,
chưa chạy trên máy thứ hai, SQL Express 2022 riêng hoặc mọi mức DPI. Chưa đo hiệu năng dữ liệu lớn.
Các mục ngoài phần Như trong sidebar vẫn là điểm tích hợp của thành viên khác; không được tính là chức năng đã hoàn thành.
