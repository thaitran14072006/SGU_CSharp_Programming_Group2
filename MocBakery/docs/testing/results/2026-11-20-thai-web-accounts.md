# Kiểm tra và tiếp tục phần Thái ngày 20/11/2026

Thực thi: Codex theo yêu cầu kiểm tra và tiếp tục công việc. Nhánh `main`, commit nền
`0bd28c3d57d47241ea1ac74253c379598acddcf0`; thay đổi lần này chưa commit.
Phân công trong README: Trần Trung Thái phụ trách Accounts/Customers.

## Tiến độ đã đối chiếu

- Desktop đã có tài khoản, nhân viên liên kết, phân quyền và khách hàng; kết quả lịch sử nằm trong hồ sơ ngày 15/11.
- Web đã có đăng ký/đăng nhập/đăng xuất, hồ sơ, cập nhật hồ sơ và đổi mật khẩu.
- Commit `1ad696a` đã thêm 16 ca tích hợp SQL và job `customer-sql` trên CI.
- Chưa có minh chứng SQL đạt trong tài liệu tài khoản hiện hành. Lượt này không truy cập kết quả CI từ xa.

## Công việc tiếp tục

- Thêm `scripts/Validate-CustomerAccounts.ps1` chạy độc lập phần tài khoản khách.
- Bổ sung nghiệp vụ/HTTP tài khoản khách vào `scripts/Validate-Project.ps1`.
- Cả hai lệnh hỗ trợ yêu cầu SQL riêng, bắt buộc biến kết nối thử trước khi chạy.
- Lệnh riêng lưu build, nghiệp vụ, HTTP, trạng thái SQL và kết quả toàn lượt; dừng khi một bước lỗi.
- Log có thời gian và commit nền, không ghi chuỗi kết nối. Thư mục artifacts được loại khỏi Git.

## Kết quả thực tế

| Hạng mục | Kết quả |
| --- | --- |
| Build Release CustomerAccountChecks và WebAccountChecks | Đạt, mỗi project 0 warning, 0 error; `RestoreSqlClient=false`, không chứng minh SQL driver |
| CustomerAccountChecks | 36 đạt, 0 lỗi; repository bộ nhớ |
| WebAccountChecks | Không hoàn tất: kết nối HTTP tới loopback bị chặn, SocketException 10013; một ca cấu hình production đạt, không có kết luận đạt cho toàn bộ HTTP |
| SQL Express | `sqlcmd` không tìm thấy/kết nối được `.\SQLEXPRESS` |
| Docker | Không kết nối được pipe Docker Desktop Linux Engine |
| 16 ca SQL tài khoản khách | Chưa chạy; không có server thử khả dụng |
| Cú pháp hai script PowerShell | Đạt |
| Chặn `-Offline -IncludeSql` và thiếu biến kết nối thử | Đạt, báo lỗi trước build/SQL |

Lần chạy bằng `-Offline` lưu log tại
`MocBakery/artifacts/customer-accounts-20261120-122841-325/`.
Lỗi HTTP phát sinh từ quyền kết nối socket của môi trường chạy; chưa đủ bằng chứng kết luận lỗi ứng dụng.
Kết quả 50/50 HTTP ngày 18/11 là lịch sử, không xem là kết quả hôm nay.

## Việc còn lại

Chạy lệnh kiểm tra riêng trên môi trường cho phép kết nối loopback, sau đó chạy với
`-IncludeSql` trên server thử có quyền tạo CSDL hoặc xem job `customer-sql` của CI.
Đối chiếu hồ sơ Web và desktop bằng cùng CSDL trước khi nghiệm thu giao diện hai ứng dụng.
Chưa xác nhận ZeroTier, điện thoại, HTTPS triển khai hoặc SQL dùng chung trong lượt này.
