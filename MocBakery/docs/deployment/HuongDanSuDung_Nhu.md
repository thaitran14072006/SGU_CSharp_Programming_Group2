# Hướng dẫn sử dụng Mộc Bakery

Tài liệu này ghi nhận bản trước khi chỉnh theo Excel. Hướng dẫn bản hiện hành nằm trong `ExcelDesign_Update.md` cùng thư mục.

Phạm vi bản hiện tại gồm tài khoản, khách hàng, loại bánh, sản phẩm và kho bánh.
Bán hàng, hóa đơn, đặt bánh và tổng quan chưa tích hợp; các menu đó chỉ hiển thị thông báo.

## Chuẩn bị và mở ứng dụng

1. Máy chạy cần Windows và .NET Desktop Runtime 10 đúng kiến trúc bản phát hành. Máy phát triển dùng .NET SDK 10.
2. Tạo CSDL mới bằng `database/schema/01_CreateDatabase.sql`. Không chạy đè vào CSDL đang có các bảng nghiệp vụ.
3. Tùy chọn nạp `02_Customers.sql` và `03_CatalogInventory_Demo.sql` vào CSDL thử. Seed demo chỉ thêm dữ liệu mẫu chưa có, không reset số lượng.
4. Đặt `appsettings.local.json` cạnh executable theo `appsettings.example.json`. Tên server và database phải đúng môi trường. Biến `MOCBAKERY_CONNECTION_STRING`, nếu có, được ưu tiên hơn tệp này.
5. Mở `MocBakery.GUI.exe`. Khi chưa có tài khoản, chọn **Thiết lập lần đầu**, tự đặt tên đăng nhập, họ tên và mật khẩu chủ tiệm. Không có mật khẩu mặc định.

Trên máy đang phát triển đã chuẩn bị bản `artifacts/publish/MocBakery.GUI.exe` với cấu hình LocalDB riêng cho demo.
Cấu hình theo máy và binary không đưa lên Git. Người nhận cần publish và cấu hình lại theo máy của mình.

## Đăng nhập và khách hàng

Đăng nhập bằng tài khoản đã thiết lập. Chủ tiệm quản lý tài khoản, khóa/mở tài khoản và đặt lại mật khẩu.
Mỗi người dùng có thể đổi mật khẩu của mình. Menu Khách hàng hỗ trợ tìm theo tên/điện thoại,
thêm, sửa và xem lịch sử hóa đơn; dữ liệu điện thoại được kiểm tra trùng. Các chức năng này do Thái hiện thực.

## Quản lý loại bánh

Chọn **Loại bánh**. Nhập từ khóa rồi bấm **Tìm / Tải lại**; để trống để xem tất cả.
Chủ tiệm bấm **Thêm loại**, điền tên và mô tả rồi **Lưu**. Tên không được rỗng hoặc trùng.
Chọn dòng rồi **Sửa loại** để cập nhật. **Xóa loại** yêu cầu xác nhận và chỉ thành công khi chưa có sản phẩm sử dụng loại đó.
Nhân viên không có nút thêm/sửa/xóa.

## Quản lý sản phẩm

Chọn **Sản phẩm**, nhập tên cần tìm, chọn loại bánh và trạng thái, rồi bấm **Tìm / Tải lại**.
Chọn một dòng rồi **Xem chi tiết** để xem mô tả đầy đủ.

Chủ tiệm bấm **Thêm bánh**, điền tên, loại, đơn vị, giá và mô tả. Cần tạo loại bánh trước.
Giá được phép bằng 0, không được âm, tối đa 2 chữ số thập phân.
Chọn dòng rồi **Sửa / Ngừng bán** để sửa; bỏ chọn **Đang kinh doanh** để ngừng bán sản phẩm.
Sản phẩm ngừng kinh doanh vẫn giữ các lô và lịch sử; toàn bộ lô của nó không được tính vào tồn có thể bán.
Không có thao tác xóa sản phẩm.

## Nhập lô và tra cứu kho

1. Chọn **Kho bánh → Nhập lô** bằng tài khoản chủ tiệm.
2. Chọn sản phẩm đang kinh doanh; nhập nguồn (nếu có), ngày sản xuất, hạn dùng và số lượng nguyên dương.
3. Ngày sản xuất không được ở tương lai, hạn dùng không trước ngày sản xuất. Khi lưu, số lượng còn bằng số lượng nhập.
4. Tab **Tổng tồn** hiển thị tồn vật lý và tồn có thể bán. Tồn hết hạn/ngừng bán là chỉ số riêng, có thể chồng lấp; không cộng chúng như các nhóm loại trừ nhau.
5. Nhấp đúp một sản phẩm để chuyển sang **Chi tiết lô** của sản phẩm đó. Chọn bộ lọc rồi **Tìm / Tải lại**. Lô được sắp theo hạn dùng và mã lô tăng dần.
6. Chọn lô rồi **Chi tiết lô** để xem nguồn, thời điểm nhập, ngày tháng và số lượng.
7. Chủ tiệm dùng **Ngừng bán lô** hoặc **Cho bán lại**. Cho bán lại không bỏ qua hạn dùng hay trạng thái sản phẩm.

Lô còn bán được khi sản phẩm đang kinh doanh, lô đang bán, còn số lượng, đã đến ngày sản xuất và chưa qua ngày hạn dùng.
Ví dụ hạn dùng 14/11 thì còn được bán trong ngày 14/11 và hết hạn từ 15/11.
Không có nút sửa trực tiếp số lượng còn; phần Sales sẽ trừ tồn trong transaction khi được tích hợp.
Lô đã hết hạn có thể nhập để ghi nhận hàng thực tế nhưng luôn bị loại khỏi tồn bán; cần chủ tiệm ngừng bán hoặc xử lý hàng theo quy trình thực tế.

## Dữ liệu mẫu và xử lý sự cố

Ngay ngày nạp seed, `DEMO - Bánh mì bơ tỏi` có 14 bánh vật lý, 9 có thể bán, 3 hết hạn và 2 ngừng bán.
Có thêm lô đã bán hết và sản phẩm ngừng kinh doanh để đối chiếu. Sang ngày mới hoặc sau thao tác bán, con số hợp lệ sẽ thay đổi.
Bấm **Tải lại** sau thay đổi hoặc sang ngày mới; màn hình không tự giữ chỗ hàng.

Nếu kết nối thất bại, kiểm tra dịch vụ SQL, tên server/database, quyền Windows và cấu hình local rồi tải lại.
Giao diện hiện mã lỗi; log trong thư mục `logs` cạnh executable. Không đưa cấu hình chứa thông tin riêng lên Git.
Không tự xóa database hoặc chạy lại schema để xử lý lỗi kết nối. Đăng xuất sau khi sử dụng xong.
