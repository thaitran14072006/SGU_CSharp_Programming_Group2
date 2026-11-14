# Đóng góp Chương 5 của Như

## 5.1 Triển khai Catalog và Inventory

Phần Catalog và Inventory được hiện thực theo luồng GUI → BLL → DAL → SQL Server, sử dụng chung Models và AuthService của hệ thống.
Catalog quản lý loại bánh và sản phẩm; Inventory nhập lô, tra cứu tồn, hạn dùng và trạng thái bán.
Dữ liệu sử dụng ba bảng LoaiBanh, SanPham, LoBanh trong schema chín bảng của nhóm, không tạo bảng Nhà cung cấp riêng.

Giao diện gồm ba trang Loại bánh, Sản phẩm, Kho bánh, các hộp thoại nhập/sửa và hai tab tổng tồn/chi tiết lô.
Các trang dùng màu, phông chữ, thông báo lỗi và phiên đăng nhập chung với Accounts/Customers.
Nhân viên chỉ được tra cứu; quyền quản lý được kiểm tra lại tại BLL, kể cả khi gọi trực tiếp ngoài GUI.

Giá không được âm; số lượng nhập là số nguyên dương; ngày sản xuất không ở tương lai và hạn dùng không trước ngày sản xuất.
Sản phẩm ngừng kinh doanh được giữ nguyên định danh và lịch sử. Lô còn bán được khi sản phẩm đang kinh doanh,
lô được phép bán, còn số lượng, đã đến ngày sản xuất và chưa qua hạn dùng. Hạn dùng có hiệu lực đến hết ngày được ghi.
Danh sách lô cho Sales sắp theo hạn tăng dần, tiếp theo mã lô; phương thức dùng chung transaction không tự commit.

Môi trường kiểm tra thực tế là Windows, .NET SDK 10.0.401 và SQL Server LocalDB có sẵn trên máy.
Schema cũng được chạy bằng sqlcmd trên một CSDL demo mới. Chưa kiểm chứng riêng trên máy SQL Server Express 2022 của nhóm.
Bản Release được publish theo phương án phụ thuộc .NET Desktop Runtime 10; cấu hình kết nối đặt riêng theo máy.
Phiên bản mã nguồn dùng kiểm tra được xác định bởi commit triển khai và các log trong docs/testing/evidence.

## 5.2 Kiểm thử

Kiểm thử nghiệp vụ dùng repository trong bộ nhớ với ngày cố định để kiểm tra biên hạn dùng, vai trò, dữ liệu sai và tổng tồn.
Kiểm thử tích hợp dùng Repository thật và CSDL LocalDB mới cho mỗi lần chạy; kiểm chứng unique/FK,
truy vấn FEFO, dữ liệu lưu, trạng thái sản phẩm/lô, seed lặp lại và backup/restore sang CSDL khác.
Kết quả chi tiết, dữ liệu và lệnh chạy được lưu ở docs/testing/results/2026-11-14-nhu.md và bộ ca tương ứng.

| Bộ kiểm tra | Đạt | Không đạt |
| --- | ---: | ---: |
| Nghiệp vụ Catalog/Inventory | 43 | 0 |
| Tích hợp SQL Catalog/Inventory, seed và backup/restore | 24 | 0 |
| Hồi quy Accounts/Customers | 43 | 0 |
| Tổng ca tự động đã chạy | 110 | 0 |

Ca KT04 xác nhận giá âm và hạn trước ngày sản xuất bị từ chối, dữ liệu SQL không đổi.
Ca KT05 dùng hai lô hợp lệ 4+5, một lô hết hạn 3 và một lô ngừng bán 2: tồn vật lý 14, tồn có thể bán 9.
Lô có hạn hôm nay vẫn hợp lệ, sang hôm sau bị loại. Ngừng kinh doanh sản phẩm giữ nguyên các lô nhưng làm tồn có thể bán bằng 0.
Backup/restore đã so sánh các record sản phẩm và lô giữa CSDL nguồn và bản khôi phục độc lập.

Kiểm tra bố cục tạo 16 ảnh từ WinForms bản Release, gồm giao diện chủ tiệm/nhân viên và hộp thoại.
Các ảnh dùng dữ liệu minh họa, không chứng minh đã thao tác GUI với SQL hoặc chạy giao dịch bán/giao đơn.
Sáu nhóm ca manual/system NH-M01–NH-M06 chưa chạy; KT14–KT15 toàn hệ thống chưa được kết luận đạt.

## 5.3 Kết quả và đánh giá

Phạm vi mã chức năng Catalog/Inventory, giao diện tích hợp, dữ liệu demo, truy vấn đối chiếu,
bộ kiểm tra tự động và hướng dẫn sử dụng đã hoàn thành.
Tỷ lệ đạt của các ca tự động đã chạy là 110 / (110 + 0) × 100% = 100%; con số này không bao gồm sáu nhóm ca chưa chạy.
Không suy rộng tỷ lệ này thành mức hoàn thành kiểm thử toàn hệ thống.

Lỗi thiết lập QUOTED_IDENTIFIER khi tạo CSDL bằng sqlcmd và lỗi bố cục menu/header được phát hiện và sửa trong quá trình triển khai.
Chưa đo hiệu năng, chưa xác nhận với dữ liệu lớn, chưa kiểm thử chéo bởi thành viên khác và chưa chạy trên máy thứ hai.
Phần Sales/Orders chưa được tích hợp trên nhánh nền nên chưa có bằng chứng transaction bán hoặc giao đơn từ phần Như.
