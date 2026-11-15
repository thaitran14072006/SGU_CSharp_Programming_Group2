# Các điểm đã hoàn thiện ở bản chốt

| Điểm thiếu/lỗi | Thay đổi | Kiểm tra lại |
| --- | --- | --- |
| Chưa có hồ sơ Nhân viên tách khỏi login | Entity/BLL/DAL/GUI, bảng NhanVien và migration 03; liên kết duy nhất | full-system.txt, thai-gui.txt |
| Tài khoản chưa chọn nhân viên và chưa có ma trận | Cấp tài khoản cho nhân viên đang làm, transaction kiểm tra liên kết; hiển thị quyền | full-system.txt, thai-gui.txt |
| Reset/cấp mật khẩu chưa bắt buộc đổi lần đầu | Cờ mật khẩu tạm, chặn BLL cho đến khi đổi; yêu cầu đăng nhập lại | full-system.txt, accounts-customers.txt |
| Vai trò Kho có thể thấy và gọi các nghiệp vụ thu ngân | Ẩn menu tương ứng và RequireSales kiểm tra vai trò thật | full-system.txt, thai-gui.txt |
| Chưa xác nhận SQL cho phần Thái và KT10–KT13 | Bộ kiểm tra toàn luồng, lỗi giữa transaction, tranh chấp đồng thời | full-system.txt |
| Trang nhân viên/tài khoản/khách có thể mất dữ liệu chưa lưu | IUnsavedPage và xác nhận trước khi rời trang/đóng | thai-gui.txt |
| Migration 03 chạy sqlcmd thiếu QUOTED_IDENTIFIER cho filtered index | Đặt các SET option cần thiết trong script độc lập | nâng cấp fixture thành công, full-system.txt |
| Bàn giao còn rời rạc, một số hồ sơ ghi chưa chạy | Tài liệu chốt, báo cáo tổng hợp, kịch bản demo, script kiểm tra/cài demo/đóng gói | 2026-11-15-final.md |

Đây là các thay đổi thực tế trong đợt chốt, không phải tuyên bố đã loại trừ mọi lỗi tiềm ẩn.
