# Lỗi phát hiện khi tích hợp phần Như

| Mã | Cách tái hiện và ảnh hưởng | Sửa | Kiểm tra lại |
| --- | --- | --- | --- |
| NH-D01 | Chạy schema bằng sqlcmd mặc định: CREATE TABLE/index lỗi 1934 vì QUOTED_IDENTIFIER; chặn tạo môi trường | Khai báo rõ các SET cần cho filtered index và computed column trong schema/seed | sqlcmd tạo CSDL demo mới thành công; bộ tích hợp chạy lại trên CSDL mới |
| NH-D02 | Thêm ba menu mới làm menu chủ tiệm tràn vùng điều hướng ở kích thước 1200×660 | Giảm chiều cao/khoảng cách nút, giữ cuộn cho cửa sổ nhỏ | Rà ảnh bố cục chủ tiệm thấy đủ Đăng xuất |
| NH-D03 | Tiêu đề cột chi tiết lô bị xuống dòng và cắt chữ | Cho phép tự tăng chiều cao header; phân bổ độ rộng theo nội dung | Rà lại ảnh LotDetailsPage sau publish |

Người sửa và kiểm tra tự động: Codex theo yêu cầu của Như, 14/11/2026.
Chưa có xác nhận kiểm thử chéo của Thái. Các ca manual/system chưa chạy được liệt kê riêng, không xem là lỗi đã được loại trừ.
