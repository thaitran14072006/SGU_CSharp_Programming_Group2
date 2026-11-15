# Minh chứng GUI với SQL thật ngày 15 tháng 11

Các PNG là ảnh chụp cửa sổ thực tế khi điều khiển ứng dụng Windows; tệp TXT cùng tên là cây accessibility của cùng bước.
Khác với thư mục `nhu-ui-preview`, đây là fixture SQL thật và thao tác trên các trang/dialog dùng chung của ứng dụng.
Account/session được khởi tạo trước bằng bộ chạy thử, không có ảnh hoặc mật khẩu đăng nhập cá nhân.

Xem kịch bản, phiên bản và giới hạn tại `../../results/2026-11-15-nhu-gui.md`.
`14-final-sql-check.txt` là dữ liệu cuối của lô đã nhập qua GUI. Hai snapshot SQL ghi trước/sau thử mất kết nối.
Các log unit/integration/regression là kết quả chạy lại; `expected-outage-log.txt` ghi đúng lỗi kết nối được cố ý gây ra.
