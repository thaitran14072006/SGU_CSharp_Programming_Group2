# Phần kho của Như trên desktop và Web

Ngày kiểm tra 20/11/2026. Không sửa báo cáo Word trong đợt này.

## Cách dùng

Đăng nhập admin bằng tài khoản nhân viên của database đang dùng. Menu bổ sung Nhà cung
cấp, Nguyên liệu, Phiếu nhập kho, Tồn kho và hạn dùng. Các trang dùng cùng SQL Server và
cùng WarehouseService/WarehouseRepository với desktop, không có kho dữ liệu thứ hai.

1. Tạo nhà cung cấp, nhập tên, điện thoại, email/địa chỉ nếu có. Có thể ngừng hợp tác để
   giữ lịch sử. Phiếu chưa ghi sổ không được ghi sổ nếu nhà cung cấp đã ngừng hợp tác.
2. Tạo nguyên liệu với đơn vị và ngưỡng cảnh báo. Bánh vẫn quản lý tại Quản lý bánh.
3. Lập phiếu nhập, chọn nhà cung cấp, ngày nhập và các dòng bánh/nguyên liệu. Mỗi dòng
   có số lô, ngày sản xuất, hạn dùng, số lượng và đơn giá nhập. Thêm hoặc bỏ dòng trên Web.
4. Lưu nháp: lưu PhieuNhap/ChiTietPhieuNhap, chưa tăng tồn. Có thể mở lại và sửa nháp;
   phiên bản cũ bị từ chối để tránh ghi đè người khác. Tổng tiền và đơn vị lấy lại ở server.
5. Ghi sổ theo bản nháp đã lưu: tạo LoBanh/LoNguyenLieu và BienDongKho trong một
   transaction. Nếu một dòng lỗi thì rollback toàn bộ; không có lô hoặc nhật ký nửa chừng.
   Ghi sổ xong khóa biểu mẫu; bấm lại không tăng tồn lần hai. Thay đổi chưa lưu trên trang
   làm nút ghi sổ tạm khóa, cần lưu nháp hoặc tải lại trước khi tiếp tục.
6. Tồn kho và hạn dùng: chọn Bánh/Nguyên liệu, tìm tên hoặc số lô; lọc tồn thấp, sắp hết
   hạn, hết hạn, ngừng bán. Có liên kết nhật ký từng lô và kiểm kê.
7. Kiểm kê nhập số thực đếm và lý do; giữ lượng nhập gốc, ghi số cũ/số mới và tài khoản.
8. Xuất kho chọn mặt hàng và số lượng, phân bổ theo FEFO. Xuất sản xuất chỉ dùng lô hợp
   lệ còn hạn. Nếu hủy hàng hỏng/hết hạn, chọn “Hủy hàng hỏng hoặc hết hạn” và nhập lý do;
   Web chuyển sang quy tắc hủy dùng chung với desktop. Không sửa trực tiếp SoLuongCon trong SSMS.

Các liên kết quay lại/hủy trỏ về danh sách cha, các trang danh sách có liên kết về tổng
quan. Ứng dụng không dùng lịch sử trình duyệt để đoán trang đích.

## Quyền và dữ liệu

Chủ tiệm và vai trò Kho được quản lý nghiệp vụ kho mở rộng theo quyền WarehouseService
hiện hành của desktop. Thu ngân/Nhân viên chỉ tra cứu; chặn ghi ở Web lẫn BLL/DAL, không
chỉ ẩn nút. Danh mục bánh và giá bán vẫn chỉ Chủ tiệm sửa. Nhập lô bánh trực tiếp ở màn
Kho bánh cơ bản vẫn giữ quyền cũ; nên dùng Phiếu nhập nhiều dòng để có nhật ký nhập kho.

Không cần migration mới: dùng các bảng kho đã có từ schema 02 và tài khoản nhân viên
schema 03. Các schema 04–06 dùng cho tài khoản khách và đơn Web như hướng dẫn hiện có.
Không lưu mật khẩu SQL trong mã nguồn, tài liệu hoặc ảnh minh chứng.

## Giải thích để trình bày

Ví dụ nhập 3 hộp bánh giá nhập 20.000đ và 2,5 kg bột giá nhập 24.000đ/kg: tổng phiếu
120.000đ. Nháp chỉ lưu hai dòng, chưa có tồn mới. Ghi sổ tạo lô bánh 3 hộp và lô bột
2,5 kg, kèm hai nhật ký nhập. Hai ứng dụng đọc chung các lô này. Nếu sau đó đếm bột
3,125 kg thì kiểm kê chỉ đổi lượng còn và ghi trước/sau, lượng nhập 2,5 kg vẫn giữ nguyên.

Luồng: Giao diện → WarehouseService kiểm tra quyền/nghiệp vụ → WarehouseRepository
thực hiện SQL transaction → SQL Server lưu dữ liệu. Desktop và Web là hai giao diện gọi
cùng luồng xử lý. Không tự cộng tồn từ JavaScript hoặc tạo hóa đơn khi nhập kho.

Mở `database/queries/VerifyNhuWarehouse.sql` trong SSMS để đối chiếu phiếu, dòng hàng,
lô bánh/nguyên liệu và nhật ký. Script chỉ SELECT; có thể giới hạn bằng @MaPhieu.

Minh chứng và số ca đã chạy nằm ở `docs/testing/results/2026-11-20-nhu-warehouse-web.md`.
