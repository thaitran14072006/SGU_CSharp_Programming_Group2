# Mộc Bakery

## Web Catalog của Như

ASP.NET Core MVC có trang chủ, danh mục, tìm/lọc, chi tiết và ảnh bánh, đọc cùng CSDL
desktop. [Bàn giao và cách chạy](docs/web/Nhu_BanGiao_Web.md),
[kết quả kiểm thử mới](docs/web/Nhu_KetQuaKiemThu_2026_11_18.md).
Giỏ hàng và ghi đơn Web chờ module của Mai/Nam; Catalog không cần migration mới.

## Tài khoản khách hàng Web đang phát triển

Đã có ASP.NET Core MVC cho đăng ký, đăng nhập, đăng xuất và xem thông tin tài khoản.
Hướng dẫn cấu hình CSDL chung, migration 04 và chạy tại localhost:5080:
[Tài khoản khách hàng Web](docs/deployment/CustomerWebAccounts.md).
Trang sửa hồ sơ và đổi mật khẩu Web chưa có; chưa xác nhận tích hợp SQL thật.
Các kết quả bản chốt bên dưới mô tả desktop và lịch sử kiểm thử trước phần Web.

## Bản chốt tích hợp ngày 15 tháng 11 năm 2026

Đã tích hợp phần Thái/Như/Mai/Nam trên main; bổ sung Nhân viên liên kết Tài khoản,
ma trận quyền, mật khẩu tạm và kiểm thử SQL toàn luồng. CSDL hiện hành có **16 bảng**.

- Bắt đầu tại [Hướng dẫn bàn giao](docs/deployment/CHOT_DO_AN.md).
- [Kịch bản demo và vấn đáp](docs/deployment/DEMO_VA_VAN_DAP.md).
- [Báo cáo tổng hợp chương 5–6](docs/report/CHUONG_5_6_TONG_HOP.md).
- [Báo cáo Word chương 5–6 có mục lục và ảnh minh chứng](docs/report/Bao_cao_MocBakery_Chuong_5_6.docx).
- [Kết quả bản chốt: 190 ca đạt](docs/testing/results/2026-11-15-final.md).
- Kiểm tra lại: `.\MocBakery\scripts\Validate-Project.ps1 -IncludeGui` từ gốc repository.
- CSDL 9 bảng nâng cấp bằng 02 rồi 03; CSDL 15 bảng chạy 03. Script 01 chỉ dùng cho CSDL mới.
- Bản Windows x64 kèm runtime và ZIP được tạo ở `MocBakery/artifacts/MocBakery-Final-2026-11-15`.
  Binary/cấu hình local không đưa lên Git. Script tạo demo mới: `scripts/Initialize-Demo.ps1`.

## Bản hiện hành theo thiết kế Excel

Phần Như hiện bám `Thiet_ke_giao_dien_quan_ly_tiem_banh.xlsx`: Quản lý bánh, Chi tiết bánh,
Danh mục bánh, Tồn kho và hạn dùng, Phiếu nhập kho. Có nguyên liệu, nhà cung cấp,
ảnh/hạn dùng mặc định, danh mục ngừng dùng, nháp/ghi sổ và xuất/kiểm kê có nhật ký.

- Hướng dẫn đang áp dụng: `docs/deployment/ExcelDesign_Update.md`.
- Kết quả mới: `docs/testing/results/2026-11-15-excel-design.md`.
- Báo cáo mới: `docs/report/chapter05/Nhu_TheoThietKeExcel.md`, `docs/report/chapter06/Nhu_TheoThietKeExcel.md`.
- CSDL mới chạy `database/schema/01_CreateDatabase.sql`; CSDL 9 bảng đang có chạy riêng
  `database/schema/02_Upgrade_ExcelDesign.sql` sau khi sao lưu. Không chạy 01 đè lên dữ liệu đang dùng.
- Dữ liệu mở đầu theo mẫu: `database/seeds/04_ExcelDesign_Demo.sql`, chỉ dùng trên CSDL thử.
- Bộ kiểm tra bổ sung: console CatalogInventoryChecks với `--excel`; test form SQL với GuiChecks `--verify-design --owner`.

Các mục lịch sử bên dưới mô tả bản khởi tạo trước Excel. Bản trên main đã tích hợp phần Như
với Bán hàng, Đơn đặt và Báo cáo của nhóm; giữ khung giao diện hiện hành của main.

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

## Phạm vi dữ liệu bản gốc

Bản gốc có chín bảng: `TaiKhoan`, `LoaiBanh`, `SanPham`, `LoBanh`, `KhachHang`,
`HoaDon`, `ChiTietHoaDon`, `DonDatBanh`, `ChiTietDonDat`.
Chạy script trong `database/schema/` trước, sau đó `database/seeds/`.
Chỉ đưa cấu hình mẫu không chứa bí mật lên Git; cấu hình theo máy đặt trong tệp `*.local.json`.

## Lịch sử triển khai ngày 14 tháng 11 năm 2026

Nguồn chuẩn: `BaoCao_QuanLyTiemBanh_Updated(3).docx`, UC01, UC02, UC03, UC10 và mục 4.4.
Đã có `MocBakery.sln`, bốn project, mã Accounts/Customers của Thái và script nền tảng 9 bảng.
Vai trò là `ChuTiem`/`NhanVien`; họ tên nằm trong `TaiKhoan`, không thêm bảng Nhân viên hoặc Nhà cung cấp.
Các module của thành viên khác chưa được tích hợp; trang thông báo chưa tích hợp không phải chức năng đã hoàn thành.

Mở Solution, đặt `MocBakery.GUI` làm Startup Project. Chạy `database/schema/01_CreateDatabase.sql`
trên CSDL mới rồi tùy chọn `database/seeds/02_Customers.sql`. Script không ghi đè bảng đã có.
Driver `Microsoft.Data.SqlClient` cần được restore từ NuGet. Kết nối mặc định là
`.\SQLEXPRESS`, CSDL `QuanLyTiemBanh`, Windows Authentication. Có thể đặt biến môi trường
`MOCBAKERY_CONNECTION_STRING` hoặc `appsettings.local.json` cạnh executable theo tệp mẫu.
Tại Đăng nhập, dùng **Thiết lập lần đầu** để tạo chủ tiệm khi bảng `TaiKhoan` rỗng; không có mật khẩu mặc định.

Kiểm tra nghiệp vụ: build `tests/Unit/MocBakery.UnitChecks.csproj` rồi chạy DLL tương ứng;
đây là bộ kiểm tra console, không dùng `dotnet test`. Kết quả và giới hạn ghi trong
`docs/testing/results/2026-11-14-thai.md`. README hướng dẫn chi tiết được bàn giao ngoài repo theo yêu cầu.

Các tệp `.gitkeep` giữ các thư mục chưa có nội dung. Chỉ ghi kết quả kiểm thử thực tế sau khi chạy.

## Lịch sử Catalog và Inventory ban đầu

Đã tích hợp menu Loại bánh, Sản phẩm, Kho bánh cho hai vai trò. Nhân viên chỉ tra cứu;
mọi lệnh ghi đều kiểm tra quyền chủ tiệm tại BLL. Sản phẩm chuyển ngừng kinh doanh,
không xóa lịch sử. Lô chỉ nhập mới hoặc đổi trạng thái, không sửa trực tiếp lượng còn.
Hạn dùng DATE có hiệu lực hết ngày ghi trên lô; dùng cùng ngày nghiệp vụ ở máy ứng dụng
và máy SQL Server. Dữ liệu demo dùng ngày SQL Server tại lúc chạy seed.

Chạy tùy chọn `database/seeds/03_CatalogInventory_Demo.sql` trên CSDL thử. Script chạy lại
không reset tồn đã bán. Đối chiếu bằng `database/queries/VerifyCatalogInventory.sql`.

- Hướng dẫn sử dụng: `docs/deployment/HuongDanSuDung_Nhu.md`.
- Bàn giao tích hợp Sales: `docs/deployment/BanGiao_CatalogInventory.md`.
- Ca kiểm thử và kết quả: `docs/testing/test-cases/nhu-catalog-inventory.md`,
  `docs/testing/results/2026-11-14-nhu.md`.
- Kiểm thử GUI với SQL thật ngày 15/11: `docs/testing/results/2026-11-15-nhu-gui.md`.
  Bộ chạy thử tách biệt và hướng dẫn ở `tests/System/README.md`.
- Đóng góp báo cáo: `docs/report/chapter05/Nhu_CatalogInventory.md` và
  `docs/report/chapter06/Nhu_CatalogInventory.md`.

Từ gốc repo:

```powershell
dotnet build .\MocBakery\MocBakery.sln
dotnet .\MocBakery\tests\Unit\CatalogInventory\bin\Debug\net10.0\MocBakery.CatalogInventoryChecks.dll
dotnet .\MocBakery\tests\Unit\CatalogInventory\bin\Debug\net10.0\MocBakery.CatalogInventoryChecks.dll --integration
```

Lệnh tích hợp mặc định dùng `(localdb)\MSSQLLocalDB`, tự tạo CSDL có tiền tố
`MocBakery_NhuTest_` và bản khôi phục `MocBakery_NhuRestore_`. Không ghi đè hay xóa
CSDL có sẵn. Có thể đặt `MOCBAKERY_TEST_CONNECTION` để chọn SQL Server thử khác.
CSDL thử do lần chạy đó tạo được dọn khi kết thúc để tránh chiếm dung lượng; thêm
`--keep-database` nếu cần giữ để đối chiếu. Tệp backup nằm trong thư mục `bin`.
