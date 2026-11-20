# Tiếp nhận đơn Web và hiển thị tiếng Việt

Cập nhật ngày 19/11/2026. Phần giỏ hàng của Mai đã được nối với một dịch vụ ghi đơn SQL,
dùng chung DonDatBanh và ChiTietDonDat của desktop. Các trang tiêu đề dùng Times New Roman
có đủ ký tự tiếng Việt, thay font gây tách dấu. Giữ nguyên nội dung Unicode của dữ liệu.

## Luồng sử dụng

1. Khách chọn bánh, thêm vào giỏ và đăng nhập.
2. Tại bước xác nhận, chọn ngày giờ nhận tại tiệm trong tương lai, nhập yêu cầu nếu có.
3. Gửi yêu cầu: server đọc lại giá từng dòng và tồn hợp lệ trong cùng transaction,
   ghi đơn và chi tiết rồi chuyển sang trang mã đơn. Lỗi ghi giữ nguyên giỏ và bản nháp.
4. Khách xem đơn ở `/don-hang` và `/don-hang/{id}`. Chỉ danh tính trong cookie được dùng;
   không nhận mã khách hoặc tổng tiền do browser giả mạo. Đơn của người khác trả 404.
5. Tại desktop, vào Đặt bánh theo yêu cầu, bấm Làm mới. Đơn mới có trạng thái Mới tiếp nhận
   và cột Tiếp nhận ghi Chờ tiếp nhận. Nhân viên có quyền bán hàng chọn đơn, bấm
   Tiếp nhận / chuẩn bị; tài khoản xử lý thực tế được ghi vào đơn.
6. Khi giao, desktop tạo một hóa đơn, trừ tồn theo FEFO và đổi trạng thái Đã giao.
   Hủy đơn yêu cầu lý do và không trừ kho. Khách tải lại trang để thấy trạng thái/lý do mới.

Gửi yêu cầu không giữ kho, không thanh toán online hoặc giao hàng tận nơi. Nhu cầu của
nhiều đơn mới có thể vượt tồn hiện tại; lúc giao vẫn phải kiểm tra tồn thực tế.

## Nâng cấp cơ sở dữ liệu

Bản thử tại localhost:5088 tự tạo CSDL LocalDB riêng có schema 01, tài khoản 04 và nâng
cấp 05. Không có thay đổi nào được chạy tự động lên CSDL chung của nhóm.

Để dùng tính năng này trên CSDL nhóm, sao lưu rồi chọn đúng CSDL thử trong SSMS và chạy
`database/schema/05_WebOrders.sql` sau schema 04. Có thể dùng Windows Authentication:

```powershell
sqlcmd -S TEN_MAY_SQL -d QuanLyTiemBanh -E -C -b -f 65001 -i .\MocBakery\database\schema\05_WebOrders.sql
```

Phải cập nhật **cả Web và desktop** từ bộ mã mới. Desktop cũ đọc MaTaiKhoan dạng bắt buộc
có thể không đọc được đơn Web mới chưa phân nhân viên. Không chạy schema 01 lên CSDL đã có.

Migration 05 không xóa bảng hoặc đơn cũ. Nó cho MaTaiKhoan nhận NULL lúc khách tự gửi,
thêm WebRequestKey và unique index theo khách/yêu cầu. Đơn quầy vẫn giữ nhân viên cũ.
Không tạo tài khoản nhân viên giả cho khách. Script chạy lại được mà không nhân đôi index.

## Tổ chức code và hợp đồng

- `WebOrderService` thực hiện `IWebOrderSubmission`, kiểm tra mã khách, số lượng, ngày nhận,
  ghi chú và các dòng giá đã được server xác nhận.
- `WebOrderRepository` ghi một transaction Serializable. Khóa tài khoản khách và unique
  index chống gửi trùng; cùng khách và RequestKey được trả lại cùng mã đơn, kể cả khi thử
  lại song song. Giá từng bánh vẫn phải khớp dù tổng tiền của hai giá thay đổi bù nhau.
- `WebPurchaseRequest` có CustomerId, RequestKey, Items, ConfirmedTotal, Pickup, Note và
  ConfirmedLines. Controller cung cấp các trường khách/giá từ cookie và BLL giỏ,
  không dùng giá hoặc mã khách từ form. BLL khác gọi trực tiếp phải cung cấp ConfirmedLines.
- Trang theo dõi lọc theo CustomerId ngay trong SQL. Đăng nhập tài khoản khác xóa nháp,
  khóa xác nhận và mã đơn gần nhất của phiên trước, vẫn giữ các món đang chọn.
- `OrderSummary.AccountId` nay nullable; desktop nhận đơn sẽ gán người xử lý thật.
  `IOrderRepository.Prepare` nhận thêm accountId do AuthService xác thực.

## Kiểm chứng

- Release build thành công, 0 lỗi và 0 cảnh báo.
- PublicCatalog kèm tiếp nhận đơn: 58 ca SQL/HTTP đạt. Có chống gửi song song tám lần,
  kiểm tra giá/tồn/trạng thái, ngày nhận sai, ghi chú mã hóa, quyền xem đơn, lỗi giữa lúc
  ghi rollback cả header/chi tiết và xử lý desktop tới giao/hủy.
- Giỏ hàng: 19 ca đạt, gồm lỗi database giữ giỏ và không lộ chi tiết kết nối.
- Hệ thống desktop: 37 ca SQL đạt, gồm đơn quầy cũ, FEFO, giao đồng thời, rollback và báo cáo.
- Tài khoản Web: 50 ca hồi quy đạt.
- Trình duyệt kiểm tra 52 lượt trang ở 320, 375, 768, 1440 px, gồm giỏ, xác nhận, danh sách
  và chi tiết đơn; không tràn ngang/lỗi JavaScript. Đã gửi một đơn thực qua trình duyệt vào
  CSDL thử và xem receipt. Ảnh checkout/receipt cùng kết quả ở `docs/web/evidence`.

Tổng bốn bộ console 164 ca đạt, không cộng lượt chạy lặp. Lượt này kiểm chứng xử lý desktop
qua chính BLL/DAL mà WinForms dùng; chưa thao tác chuột trực tiếp trên cửa sổ desktop.

## Bổ sung thông tin người nhận ngày 20 tháng 11 năm 2026

Trang xác nhận nay bắt buộc họ tên người nhận, số điện thoại 10 chữ số và email đúng
định dạng trước khi gửi. Trình duyệt, MVC và BLL đều kiểm tra; người tắt kiểm tra của
trình duyệt vẫn không thể gửi thông tin thiếu/sai. Lỗi giữ nguyên bản nháp và chỉ rõ ô
cần sửa. Đã bỏ dòng Bước xác nhận và dòng giới thiệu tài khoản đang đăng nhập.

Thông tin được lưu riêng theo đơn bằng `WebPurchaseRequest.Contact`, không tự thay
hồ sơ tài khoản. Trang mã đơn và desktop hiển thị thông tin người nhận thực tế, kể cả
khi người đặt đổi tên trong hồ sơ về sau. Email dùng làm thông tin liên hệ; chưa có
tính năng xác minh địa chỉ email hoặc tự động gửi thư xác nhận.

Trước khi chạy bản mới trên CSDL nhóm, chạy thêm `database/schema/06_WebOrderContacts.sql`
sau script 05. Script bổ sung NguoiNhan, DienThoaiNhan và EmailNhan; không điền dữ liệu
giả cho đơn cũ. Bản thử tự áp dụng lên LocalDB riêng, chưa thay CSDL nhóm.
Desktop mới vẫn đọc được CSDL cũ thiếu các cột này và đơn cũ không có thông tin nhận.

Đợt kiểm tra này: 61 ca PublicCatalog/đơn Web, 19 ca giỏ và 37 ca hệ thống desktop đạt,
tổng 117 ca ở các bộ chịu ảnh hưởng. Có kiểm tra thiếu từng trường, sai định dạng,
giữ bản nháp, lưu snapshot riêng và hiển thị lại đúng thông tin. Kiểm tra trình duyệt
52 lượt trang ở bốn kích thước màn hình đạt và đã thử gửi đơn với ba trường bắt buộc.
