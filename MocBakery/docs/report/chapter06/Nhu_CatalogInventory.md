# Đóng góp Chương 6 của Như

Bản lịch sử trước Excel. Nội dung báo cáo hiện hành: `Nhu_TheoThietKeExcel.md` cùng thư mục.

## 6.1 Kết luận

Phần Catalog và Inventory cung cấp quản lý loại bánh, sản phẩm, nhập lô, tra cứu tồn và kiểm soát hạn dùng.
Phân quyền dùng chung với module tài khoản; các điều kiện tồn có thể bán được áp dụng nhất quán trong phần kho và truy vấn lô cho Sales.
Sản phẩm ngừng kinh doanh giữ lịch sử, lô ngừng bán/hết hạn không bị cộng vào tồn bán.

Sản phẩm bàn giao gồm mã nguồn các lớp, giao diện tích hợp, dữ liệu mẫu, truy vấn đối chiếu,
bộ ca và log kiểm thử, ảnh bố cục, bản publish theo máy và hướng dẫn sử dụng/tích hợp.
Đã vận dụng kiến trúc nhiều lớp, ADO.NET có tham số, ràng buộc CSDL, phân quyền BLL,
kiểm thử biên ngày và kiểm chứng dữ liệu bằng truy vấn SQL. Kết quả 110 ca tự động đạt được trình bày tại Chương 5.

## 6.2 Hạn chế

- Đã chạy luồng thêm loại/sản phẩm/nhập lô, sửa giá và mất kết nối khi đọc qua GUI với SQL/session fixture thật ngày 15/11. Chưa thử hộp thoại đăng nhập cá nhân, mọi trường sai, mọi thời điểm mất kết nối hoặc các mức DPI.
- Chưa kiểm thử chéo Sales vì mã Sales chưa tích hợp; chưa chứng minh cạnh tranh tồn hoặc rollback của giao dịch bán/giao đơn.
- Kiểm thử SQL thực hiện trên LocalDB; chưa chạy toàn bộ hệ thống trên máy thứ hai hoặc SQL Server Express 2022 riêng.
- Danh mục và kho hiện tải danh sách về bộ nhớ để lọc/tổng hợp; chưa phân trang và chưa đo hiệu năng dữ liệu lớn.
- Tồn hiển thị là dữ liệu tại thời điểm tải, không phải giữ chỗ. Các lần đọc có thể khác nhau khi có bán hàng đồng thời.
- Ngày nghiệp vụ dùng ngày local; cần thống nhất đồng hồ/múi giờ giữa các máy khi triển khai.
- Chưa có nghiệp vụ điều chỉnh tồn, hủy hàng, nhật ký nhập/xuất riêng hoặc cấu hình cảnh báo sắp hết hạn.

Không có bảng Nhà cung cấp độc lập là giới hạn phạm vi chín bảng đã thống nhất, không phải chức năng đã hiện thực còn lỗi.
Nguồn nhập hiện là chuỗi mô tả trên LoBanh, không được mô tả như một module quản lý nhà cung cấp.

## 6.3 Hướng phát triển

Ưu tiên tích hợp Sales/Orders và kiểm thử chéo FEFO, quyền, rollback và cạnh tranh tồn trước khi mở rộng tính năng.
Hoàn thành các ca thao tác GUI, mất kết nối và vận hành trên máy thứ hai; chốt quy ước ngày nghiệp vụ chung.
Sau đó bổ sung phân trang/lọc tại SQL, đo trên bộ dữ liệu xác định trước khi tối ưu.

Nhu cầu xử lý hàng hỏng/hết hạn nên được triển khai bằng nghiệp vụ điều chỉnh có lý do và nhật ký,
với thay đổi dữ liệu, màn hình, quyền và báo cáo tương ứng; không sửa tay lượng còn trên giao diện hiện tại.
Cảnh báo sắp hết hạn cần cấu hình số ngày và màn hình lọc/cảnh báo.
Quản lý nhà cung cấp hoặc nhiều chi nhánh cần thống nhất thêm bảng và khóa liên quan trước khi triển khai.
