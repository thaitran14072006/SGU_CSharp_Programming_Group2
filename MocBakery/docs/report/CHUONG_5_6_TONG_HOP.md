# Triển khai kiểm thử và kết luận đồ án Mộc Bakery

Tài liệu tổng hợp chương 5 và 6 cho bản tích hợp ngày 15/11/2026. Nội dung phản ánh mã nguồn
và kiểm tra thực tế; dùng để ghép vào báo cáo chính của nhóm, giữ thông tin bìa và các chương 1–4
theo bản báo cáo đã được giảng viên/nhóm thống nhất.

## Chương 5 Triển khai và đánh giá kết quả

### 5.1 Môi trường và kiến trúc triển khai

Mộc Bakery là ứng dụng desktop Windows Forms viết bằng C# trên .NET 10, sử dụng SQL Server
và ADO.NET với Microsoft.Data.SqlClient. Môi trường kiểm tra thực tế là Windows x64,
.NET SDK 10.0.401 và SQL LocalDB. Bản bàn giao được publish Release Windows x64 self-contained,
kèm runtime để máy nhận không cần cài Visual Studio hoặc .NET riêng; máy nhận vẫn cần SQL Server.

Solution gồm bốn project chính. GUI chứa form, nhận dữ liệu và hiển thị lỗi. BLL xử lý quy tắc
nghiệp vụ, phiên đăng nhập và phân quyền. DAL thực hiện truy vấn có tham số và giao dịch SQL.
Models chứa entity, DTO và hợp đồng repository. Các project kiểm tra độc lập dùng cùng dịch vụ
nghiệp vụ và repository với ứng dụng.

Trần Trung Thái phụ trách Accounts/Customers; Phạm Quỳnh Như phụ trách Catalog/Inventory;
Nguyễn Thị Trúc Mai phụ trách Sales; Trần Thanh Nam phụ trách Orders/Reports.
Trong giai đoạn chốt, Như tiếp nhận hoàn thiện phần Thái và tích hợp các module trên main.
Giao diện sử dụng nền kem, nâu/cam, sidebar và các trang nội dung theo bộ thiết kế Excel nhóm cung cấp.

### 5.2 Triển khai các phân hệ

**Đăng nhập và nhân sự.** Đăng nhập kiểm tra tài khoản, trạng thái và hash mật khẩu.
Mật khẩu lưu bằng PBKDF2 với salt ngẫu nhiên; tên đăng nhập được ghi nhớ nếu người dùng chọn,
không ghi nhớ mật khẩu. Hồ sơ nhân viên tách khỏi tài khoản đăng nhập và liên kết một đối một
khi cấp tài khoản. Hồ sơ lưu tên, điện thoại, chức vụ, ngày vào làm và trạng thái. Nhân viên nghỉ
việc được giữ lịch sử, đồng thời khóa tài khoản liên quan trong cùng giao dịch.

Quản lý tạo tài khoản cho nhân viên đang làm, đặt vai trò và trạng thái, xem ma trận quyền.
Mật khẩu cấp mới hoặc đặt lại qua màn hình quản trị là mật khẩu tạm; người dùng phải đổi
trước khi dùng nghiệp vụ. Đổi thành công yêu cầu đăng nhập lại. Không cho tự khóa/hạ quyền
quản lý đang sử dụng hoặc làm mất quản lý hoạt động cuối cùng. BLL đọc lại trạng thái/hash/vai trò
ở mỗi thao tác, vì vậy ẩn menu không phải cơ chế bảo vệ duy nhất.

**Khách hàng.** Cho phép thêm/sửa, tìm tên/SĐT và xem lịch sử hóa đơn. Điện thoại là chuỗi
đúng 10 chữ số để giữ số 0 đầu và không trùng. Dữ liệu Unicode và ký tự tìm kiếm đặc biệt được
xử lý bằng truy vấn tham số. Khách lẻ có thể bán hàng mà không tạo hồ sơ. Hồ sơ đã có hóa đơn
được giữ để không mất liên kết lịch sử.

**Danh mục và bánh.** Danh mục có trạng thái đang dùng/ngừng dùng. Bánh có đơn vị, giá,
trạng thái kinh doanh, hạn dùng mặc định, ảnh và ngưỡng cảnh báo tồn. Bánh được tìm theo mã/tên,
lọc danh mục/trạng thái và mở chi tiết để sửa. Giá phải lớn hơn 0, tên tối đa 100 ký tự,
ảnh JPEG/PNG được kiểm tra và giới hạn dung lượng. Ngừng kinh doanh giữ lịch sử sản phẩm/lô.

**Kho và phiếu nhập.** Quản lý bánh và nguyên liệu theo lô, ngày sản xuất, hạn dùng và lượng còn.
Phiếu có NCC, ngày nhập và nhiều dòng hàng. Lưu nháp không tăng tồn; ghi sổ tạo tất cả lô,
nhật ký và cập nhật trạng thái trong một transaction. Rowversion chặn sửa đè nháp; khóa và kiểm tra
trạng thái chặn ghi sổ lặp. Xuất kho theo FEFO; kiểm kê ghi lại lượng cũ/mới, người thực hiện và lý do,
giữ nguyên lượng nhập gốc. Có bộ lọc tồn thấp, sắp hết hạn, hết hạn và quản lý NCC/nguyên liệu.

**Bán hàng và hóa đơn.** Chọn bánh, thay đổi số lượng, gắn khách hoặc bán khách lẻ và lập hóa đơn.
Hệ thống lấy giá hiện hành, phân bổ các lô hợp lệ theo FEFO và giữ giá tại thời điểm bán trong chi tiết.
Giao dịch lưu header, chi tiết và trừ tồn cùng nhau. Khóa lô và lệnh trừ có điều kiện ngăn âm kho
khi nhiều người cùng mua sản phẩm cuối.

**Đơn đặt và báo cáo.** Đơn đặt lưu khách, ngày nhận, yêu cầu, bánh, tổng tiền và cọc;
tạo đơn chưa trừ tồn. Đơn chuyển từ mới tiếp nhận sang chuẩn bị rồi giao hoặc hủy trước giao.
Giao đơn tạo hóa đơn và trừ kho trong cùng transaction; hóa đơn lấy tổng đơn, trừ cọc một lần
để tính số còn thu. Đơn đã giao/hủy không được sửa hoặc giao lại.
Báo cáo tổng hợp doanh thu, số hóa đơn, bánh bán chạy, tồn thấp và đơn sắp nhận;
doanh thu lấy tổng hóa đơn, không cộng lại tiền cọc. Xuất báo cáo hiện là CSV mở được bằng Excel.

### 5.3 Cơ sở dữ liệu và triển khai

CSDL hiện có 16 bảng: TaiKhoan, NhanVien, KhachHang, LoaiBanh, SanPham, LoBanh,
HoaDon, ChiTietHoaDon, DonDatBanh, ChiTietDonDat, NhaCungCap, NguyenLieu,
LoNguyenLieu, PhieuNhap, ChiTietPhieuNhap và BienDongKho. Khóa ngoại giữ quan hệ lịch sử,
ràng buộc duy nhất chặn trùng tên đăng nhập, điện thoại khách/nhân viên và liên kết tài khoản.

Script 01 dùng cho CSDL mới. Migration 02 mở rộng bản 9 bảng thành 15 bảng theo Excel;
migration 03 thêm nhân viên và cờ đổi mật khẩu tạm. Khi nâng cấp, tài khoản cũ tự có hồ sơ
liên kết, giữ nguyên hash, vai trò và trạng thái khóa. Điện thoại chưa có không được tự bịa dữ liệu.
Các migration chạy lặp mà không reset tồn hoặc xóa lịch sử. Cần sao lưu trước khi nâng cấp CSDL thực tế.

Cấu hình kết nối đặt trong biến môi trường hoặc `appsettings.local.json` theo máy.
Bản source chỉ chứa cấu hình mẫu. Script Initialize-Demo tạo CSDL demo tên mới, không ghi đè
dữ liệu cũ. Người dùng tạo quản lý đầu tiên qua màn hình Thiết lập lần đầu; không có mật khẩu mặc định.

### 5.4 Phương pháp và kết quả kiểm thử

Nhóm sử dụng kiểm tra nghiệp vụ với repository trong bộ nhớ, kiểm tra tích hợp trên CSDL
tách biệt và kiểm tra sự kiện form WinForms với SQL thật. Ca lỗi SQL được gây có chủ đích
trên database thử để xác nhận rollback. Ca đồng thời chạy hai giao dịch độc lập để xác nhận
không ghi sổ/giao đơn hai lần và không âm tồn. Bộ kiểm tra chỉ xóa các CSDL thử do chính lần chạy tạo.

| Bộ kiểm tra | Số đạt | Số không đạt |
| --- | ---: | ---: |
| Accounts/Customers và FEFO nghiệp vụ | 45 | 0 |
| Catalog/Inventory nghiệp vụ | 43 | 0 |
| Tích hợp SQL, seed, Sales, backup/restore | 25 | 0 |
| Quy tắc Excel và giao dịch kho | 25 | 0 |
| Phần Thái và toàn luồng SQL, KT06–KT13 | 37 | 0 |
| Form Catalog/Inventory với SQL thật | 7 | 0 |
| Form Nhân viên/Tài khoản/Khách hàng với SQL thật | 8 | 0 |
| Tổng | 190 | 0 |

Build Release thành công với 0 lỗi, 0 cảnh báo. Có 18 ảnh bố cục, không cộng ảnh vào số ca nghiệp vụ.
Tỷ lệ đạt trên các ca thực sự đã chạy là 190/190 = 100%; tỷ lệ này không đồng nghĩa bao phủ mọi lỗi.
Log đầy đủ và ảnh tại `docs/testing/evidence/final-2026-11-15/`;
kết quả chi tiết tại `docs/testing/results/2026-11-15-final.md`.

Trong ca đối chiếu toàn luồng, bán 6 bánh từ hai lô xuất đúng 4 + 2, hóa đơn 150.000đ.
Đơn 8 bánh tổng 200.000đ, cọc 50.000đ không trừ kho khi tạo; khi giao còn thu 150.000đ.
Đơn tiếp theo 2 bánh được hai phiên giao đồng thời nhưng chỉ tạo một hóa đơn. Tại điểm đối chiếu,
3 hóa đơn có doanh thu 400.000đ, tiền thu trên hóa đơn 340.000đ, bán 16 bánh và còn 18 bánh;
lịch sử khách và báo cáo cùng khớp. Ca riêng tranh chiếc bánh cuối có đúng một lần bán thành công.

Backup/restore bản thử độc lập giữ dữ liệu bánh/lô. Publish self-contained và executable đóng gói
đã chạy thành công trên máy hiện tại. Script demo tạo đúng 16 bảng, 5 bánh, tồn mẫu 103 và
chưa có tài khoản trước khi thiết lập lần đầu.

### 5.5 Đánh giá và phạm vi xác nhận

Các phân hệ chính đã tích hợp và có bằng chứng nghiệp vụ, SQL, transaction và phân quyền.
Những phần thiếu ở giai đoạn bàn giao cũ đã được bổ sung: nhân viên/tài khoản liên kết,
mật khẩu tạm, kiểm tra quyền Kho, migration nhân sự, kiểm thử KT10–KT13 và bộ chạy cài demo.
Các tài liệu ghi “chưa chạy” trước đây được giữ làm lịch sử và có đường dẫn tới kết quả mới.

Đợt kiểm tra chưa xác nhận máy thứ hai, nhiều mức DPI, máy in vật lý hoặc hiệu năng dữ liệu lớn.
Sự kiện form được phát tự động ngoài màn hình với callback xác nhận, chưa thay thế mọi thao tác tay.
Nhóm cần thử bộ ZIP trên máy bảo vệ để xác nhận môi trường SQL/kết nối trước buổi trình bày.

## Chương 6 Kết luận và hướng phát triển

### 6.1 Kết quả đạt được

Đồ án hiện thực ứng dụng quản lý tiệm bánh theo mô hình phân lớp, kết nối SQL Server và tích hợp
nhân sự/tài khoản, khách hàng, bánh/danh mục, kho/phiếu nhập, bán hàng, đơn đặt và báo cáo.
Dữ liệu liên kết được giữ lịch sử; nghiệp vụ quan trọng có kiểm tra phiên/quyền và transaction.
Giao diện bám bộ thiết kế đã cung cấp, có kiểm tra dữ liệu và xác nhận các thao tác quan trọng.

Quá trình tích hợp có bộ kiểm tra chạy lại được và bằng chứng đối chiếu số liệu. Bản bàn giao gồm
source trên main, migration/seed, executable Windows x64 kèm runtime, hướng dẫn triển khai,
kịch bản demo, báo cáo chương 5–6 và kết quả kiểm tra.

### 6.2 Hạn chế

Ứng dụng phục vụ mô hình một tiệm, chưa quản lý nhiều chi nhánh, chưa kết nối thanh toán điện tử
hoặc đồng bộ trực tuyến. Báo cáo xuất CSV, chưa tạo workbook Excel có định dạng nâng cao.
Sao lưu/khôi phục thực hiện qua công cụ SQL, chưa có trình quản trị riêng trong ứng dụng.
Chưa có số liệu đánh giá tải lớn, máy thứ hai hoặc in vật lý; không suy rộng từ các ca tự động
đã đạt để kết luận sẵn sàng cho mọi môi trường vận hành.

### 6.3 Hướng phát triển

Nhóm có thể bổ sung theo dõi nguyên liệu theo công thức sản xuất, nhiều chi nhánh, tích hợp máy in
hóa đơn/thanh toán, xuất XLSX/PDF chuẩn và nhật ký quản trị chi tiết hơn.
Trước khi mở rộng vận hành, cần kiểm tra trên máy người dùng, nhiều DPI, dữ liệu lớn và
quy trình backup/restore định kỳ, đồng thời tăng kiểm thử thao tác giao diện thực tế.
