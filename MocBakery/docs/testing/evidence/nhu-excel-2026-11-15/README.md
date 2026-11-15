# Minh chứng bản theo thiết kế Excel

`layout/` là ảnh render các control WinForms đang dùng với dữ liệu minh họa, đối chiếu các sheet trong workbook.
`form-sql/` là ảnh form trong luồng phát sự kiện tự động với SQL thật: dữ liệu được lưu, phiếu nháp chưa tăng tồn,
ghi sổ tăng đúng một lần và khóa sửa. Hộp thoại xác nhận dùng phản hồi từ test host; không có thao tác đăng nhập cá nhân.

Log build, unit, hồi quy Accounts, tích hợp SQL cũ, nghiệp vụ Excel và form-SQL được lưu riêng.
Mã nguồn và giới hạn của lần kiểm tra được ghi trong `../../results/2026-11-15-excel-design.md`.
Các tệp cấu hình local, database/backup và binary không được đưa vào thư mục minh chứng hoặc lên Git.
