# Mộc Bakery

Cấu trúc theo tài liệu `Huong_dan_4_thanh_vien_MocBakery_Chuong_5_6.docx`.
Mã nguồn ứng dụng nằm trong `MocBakery/`; bài thực hành vẫn nằm trong `Lab01/` ở gốc repo.

## Công nghệ và kiến trúc

C# / .NET 10, Windows Forms, SQL Server 2022 Express, ADO.NET với Microsoft.Data.SqlClient.
Luồng xử lý: GUI → BLL → DAL → SQL Server. Models chứa Entities và DTOs dùng chung.

| Thư mục | Nội dung |
| --- | --- |
| `src/MocBakery.GUI/Forms/<Module>/` | Form và thao tác người dùng |
| `src/MocBakery.BLL/Services/<Module>/` | Quyền và quy tắc nghiệp vụ |
| `src/MocBakery.DAL/Repositories/<Module>/` | Truy vấn tham số và truy cập CSDL |
| `src/MocBakery.Models/Entities/` | Thực thể dùng chung |
| `src/MocBakery.Models/DTOs/<Module>/` | Dữ liệu trao đổi theo module |
| `database/schema/` | Script tạo bảng và ràng buộc |
| `database/seeds/` | Script dữ liệu mẫu |
| `database/queries/` | Truy vấn đối chiếu dữ liệu |
| `tests/Unit/` | Kiểm thử đơn vị |
| `tests/Integration/` | Kiểm thử tích hợp CSDL |
| `tests/System/` | Kiểm thử toàn hệ thống |
| `docs/testing/test-cases/` | Ca kiểm thử |
| `docs/testing/results/` | Kết quả thực tế, người chạy, ngày và commit |
| `docs/testing/defects/` | Lỗi, cách tái hiện và kết quả kiểm tra lại |
| `docs/testing/evidence/` | Ảnh và minh chứng đối chiếu dữ liệu |
| `docs/deployment/` | Hướng dẫn cài đặt, cấu hình, đóng gói và khôi phục |
| `docs/report/chapter05/` | Triển khai, kiểm thử và đánh giá kết quả |
| `docs/report/chapter06/` | Kết luận, hạn chế và hướng phát triển |

## Phân công theo chiều dọc

Mỗi thành viên thực hiện GUI, BLL, DAL, DTO, kiểm thử và nội dung báo cáo của module mình.

| Thành viên | Module |
| --- | --- |
| Trần Trung Thái | `Accounts`, `Customers` |
| Phạm Quỳnh Như | `Catalog`, `Inventory` |
| Nguyễn Thị Trúc Mai | `Sales` |
| Trần Thanh Nam | `Orders`, `Reports` |

## Phạm vi dữ liệu

Chín bảng thống nhất: `TaiKhoan`, `LoaiBanh`, `SanPham`, `LoBanh`, `KhachHang`,
`HoaDon`, `ChiTietHoaDon`, `DonDatBanh`, `ChiTietDonDat`.
Chạy script trong `database/schema/` trước, sau đó `database/seeds/`.
Chỉ đưa cấu hình mẫu không chứa bí mật lên Git; cấu hình theo máy đặt trong tệp `*.local.json`.

## Trạng thái khởi tạo

Đã tạo cấu trúc thư mục. Chưa tạo Solution, project, mã chức năng hoặc script SQL.
Các tệp `.gitkeep` giúp Git lưu thư mục trống; có thể xóa khi thư mục đã có tệp thực tế.
Khi triển khai, tạo Solution trong `MocBakery/` và bốn project tại đúng thư mục `src/` đã chuẩn bị.
Chỉ ghi kết quả kiểm thử và kết quả báo cáo sau khi thực hiện thực tế.
