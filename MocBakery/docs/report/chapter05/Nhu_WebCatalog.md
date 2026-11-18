# Phần Như bổ sung chương 5 triển khai Web Catalog

Web khách xem bánh dùng ASP.NET Core MVC cùng Models, BLL, DAL, SQL Server của WinForms.
Như thực hiện home, layout/menu/CSS, danh mục, thực đơn tìm/lọc và chi tiết. Tài khoản
khách thuộc Thái; giỏ và ghi đơn chờ Mai/Nam tích hợp.

Khách xem không cần đăng nhập. Thực đơn tìm theo tên/mã, lọc danh mục/tình trạng, sắp
tên/giá và phân trang. Chi tiết có tên, mô tả, đơn vị, giá, ảnh và cờ còn bánh. Tìm rỗng
có hướng dẫn bỏ lọc; bánh mất/ngừng bán trả 404. Thiếu ảnh hiện thông báo thay thế.

Nền kem, chữ nâu, khoảng trống và ít khung bao; ba cột sản phẩm trên máy tính chuyển
hai cột điện thoại. Menu xuống dòng; ô tìm/nút tìm cả hàng; bộ lọc hai cột. Trường có
nhãn, nút có focus và liên kết bàn phím bỏ qua menu.

PublicProduct chỉ chứa trường khách cần, không có số tồn, lô, giá vốn hay tài khoản.
PublicCatalogService trim/giới hạn từ khóa, kiểm tra bộ lọc/phân trang. CheckSelection
trả giá mới và cờ đủ lượng cho BLL giỏ; không giữ chỗ. Mai đọc lại khi xác nhận, Nam
kiểm tra trong transaction ghi đơn. Quyền ghi CatalogService quản trị giữ nguyên.

DAL chỉ công khai bánh kinh doanh thuộc danh mục đang dùng. Tồn tính cùng điều kiện
lô desktop: đang bán, còn lượng, đã sản xuất, chưa quá hạn theo ngày SQL Server;
ngày hết hạn vẫn hợp lệ. Không đủ lô thì hiện “Tạm hết bánh”. SQL tham số hóa, escape
LIKE, sort theo danh sách cố định. HTML/API/ảnh không cache; tải lại thấy thay đổi từ
desktop. Không cần migration mới.

HTML: `/`, `/banh`, `/danh-muc`, `/danh-muc/{id}`, `/banh/{id}`.
API: `/api/danh-muc`, `/api/banh`, `/api/banh/{id}`; ảnh `/banh/{id}/anh`.
Sai input 400, bánh ẩn 404, lỗi dịch vụ 503 không lộ SQL. Tài liệu/hợp đồng tại
`docs/web`; bốn ảnh home/detail 375/1440 px tại `docs/web/evidence` để minh họa báo cáo.
