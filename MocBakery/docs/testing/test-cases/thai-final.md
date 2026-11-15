# Ca kiểm tra phần Thái và tích hợp bản chốt

Nguồn thực thi: `tests/Unit/CatalogInventory/FullSystemChecks.cs` (`--system`) và
`tests/System/GuiThaiWorkflow.cs` (`--verify-thai --owner`). Kết quả không lấy từ bảng kế hoạch này;
xem log thực tế trong `docs/testing/evidence/final-2026-11-15/`.

| Phạm vi | Tình huống | Kết quả mong đợi |
| --- | --- | --- |
| Khởi tạo | Tạo quản lý, thử khởi tạo lần hai | Một quản lý có hồ sơ liên kết; lần hai bị chặn |
| Nhân viên | Tên Unicode, mã NV, tìm tên/SĐT | Chuẩn hóa tên, tra đúng hồ sơ |
| Nhân viên | Sai/trùng SĐT, sai chức vụ, ngày tương lai | Lỗi đúng trường, không thêm/sửa dở dang |
| Tài khoản | Cấp login cho hồ sơ đang làm | Liên kết một tài khoản duy nhất trong transaction |
| Tài khoản | Trùng username/liên kết | Không để tài khoản hoặc liên kết mồ côi |
| Mật khẩu | Mật khẩu tạm/reset | Chỉ được đổi mật khẩu trước khi dùng nghiệp vụ |
| Mật khẩu | Đổi thành công, mở hai phiên cũ | Phải đăng nhập lại, cả hai phiên cũ bị chặn |
| Phân quyền | Thu ngân gọi quản trị; Kho gọi bán/khách/đơn/báo cáo | BLL từ chối, giả vai trò hiển thị không nâng quyền |
| Nhân sự | Tự nghỉ việc/hạ quyền quản lý đang dùng | Bị chặn |
| Nhân sự | Nghỉ việc, quay lại làm việc | Khóa login cùng transaction; quay lại không tự mở khóa |
| Khách hàng | Unicode, số 0 đầu, tìm ký tự % _ [ | Giữ dữ liệu, tìm ký tự theo nghĩa thường |
| Khách hàng | Trùng SĐT, sửa giữ SĐT, lịch sử mua | Chặn hồ sơ trùng, cập nhật hợp lệ, lịch sử khớp hóa đơn |
| KT06–KT09 | FEFO, thiếu tồn, lỗi SQL giữa chừng, tranh chiếc cuối | Đúng lô, rollback đầy đủ, đúng một lần bán |
| KT10–KT13 | Tạo/cọc/chuẩn bị/giao/hủy, thiếu tồn, lỗi SQL, giao đồng thời | Không trừ kho lúc tạo, trừ cọc một lần, không giao/hủy/sửa đơn đã kết thúc |
| Báo cáo | Khoảng ngày, quyền, tổng/doanh thu/top bán | Đối chiếu hóa đơn, không cộng cọc hai lần |
| Migration | Bản 15 bảng, chạy 03 hai lần | Thêm hồ sơ, giữ hash/khóa/liên kết/tồn |
| Form SQL | Sai SĐT, lưu Nhân viên/Tài khoản/Khách, chọn nhân viên | Lỗi trường, dữ liệu thật lưu/đọc đúng |
| Form SQL | Rời trang chưa lưu, chọn giữ lại | Trang hiện hành và dữ liệu nhập còn nguyên |

Các ca phá lỗi chỉ chạy trên database thử có tiền tố cố định; không dùng database nghiệp vụ.
