# Kết quả kiểm thử phần kho của Như ngày 20 tháng 11 năm 2026

## Phạm vi và môi trường

Windows x64, .NET SDK 11.0.100, target .NET 10, SQL LocalDB và Edge headless.
Desktop chạy các sự kiện form ngoài màn hình qua bộ kiểm tra hiện có; Web chạy HTTP
Kestrel thật và trình duyệt. Cơ sở dữ liệu thử tách biệt, không nhập/xuất/kiểm kê dữ liệu
nhóm trong đợt kiểm thử này. Không thay đổi cấu trúc database cho kho Web.

| Bộ kiểm tra thực chạy | Đạt | Không đạt |
| --- | ---: | ---: |
| PublicCatalogChecks gồm Catalog, đơn, admin và 20 ca kho Web mới | 110 | 0 |
| CatalogInventoryChecks --excel, nghiệp vụ và transaction kho | 25 | 0 |
| GuiChecks --verify-design --owner, sự kiện form desktop + SQL | 7 | 0 |
| Luồng Edge tạo NCC/nguyên liệu/phiếu, ghi sổ, kiểm kê, quay lại | 8 | 0 |

Edge còn kiểm tra 44 lượt trang kho ở chiều rộng 320/375/768/1440 px, không tràn ngang
và không lỗi JavaScript. Các lượt trang được thống kê riêng, không cộng vào ca nghiệp vụ.
Build Release không lỗi, không cảnh báo ở lần kiểm tra mã nguồn.

## Kết quả nghiệp vụ đã đối chiếu

Desktop: phiếu PN001 trong database thử có hai dòng, tổng 120.000đ. Dòng bánh 3 hộp
giá 20.000đ và nguyên liệu 2,5 kg giá 24.000đ. Nháp chưa tăng tồn; ghi sổ tạo hai lô và
hai nhật ký từ 0 → 3 và 0 → 2,5. Lượng còn khớp số đã nhập. Nút sửa/ghi sổ bị khóa sau
khi đã ghi sổ. Bộ thử Thu ngân chỉ có nút tra cứu.

HTTP: kiểm tra tổng phiếu 159.000đ với 2 bánh × 18.000đ và 5,125 kg × 24.000đ;
không tin tổng tiền/đơn vị giả mạo trong request. Kiểm tra phiên bản nháp cũ, thiếu hoặc
sai base64; từ chối lưu đè. Đăng lại ghi sổ không tạo thêm tồn. Lỗi số lô trùng ở dòng hai
rollback cả lô dòng một và nhật ký. Kiểm kê 5,125 → 6,25 kg giữ lượng nhập gốc 5,125.
Xuất 3 kg từ hai lô dùng 2 kg ở lô hạn gần trước, rồi 1 kg ở lô còn lại. Lệnh vượt tồn,
lý do rỗng, mã không tồn tại và vai trò bán hàng ghi kho đều bị chặn.

Lựa chọn hủy hàng quá hạn tách khỏi nội dung lý do. Lệnh xuất sử dụng có chữ “Hủy:”
trong lý do vẫn không lấy lô hết hạn; chỉ khi chọn hủy mới loại bỏ lô hết hạn theo quy tắc.

Trình duyệt: thêm ba dòng rồi bỏ dòng giữa, chỉ số tên trường được sắp lại đúng; lưu
phiếu hai dòng tổng 114.000đ. Nháp chưa tăng tồn; ghi sổ tăng đúng 3 bánh và 2,5 kg.
Thay đổi nháp chưa lưu khóa nút ghi sổ cho đến khi lưu/tải lại. Kiểm kê bột 2,5 → 3,125
kg hiện đúng nhật ký trên trang. Nút Hủy/quay lại của ba nhóm biểu mẫu về đúng danh sách.

## Minh chứng

Thư mục `docs/testing/evidence/nhu-warehouse-2026-11-20/` chứa log nguyên bản, kết quả
SELECT desktop, JSON kiểm tra trình duyệt và ảnh giao diện desktop/Web. Ảnh có dữ liệu
thử riêng, không phải ảnh vận hành tại cửa hàng hoặc dữ liệu nhóm.

## Chạy lại

```powershell
dotnet run --project MocBakery/tests/PublicCatalog/MocBakery.PublicCatalogChecks.csproj -c Release
dotnet MocBakery/tests/Unit/CatalogInventory/bin/Release/net10.0/MocBakery.CatalogInventoryChecks.dll --excel
.\MocBakery\tests\System\PrepareGuiFixture.ps1 -NewFixture
dotnet MocBakery/tests/System/bin/Release/net10.0-windows/MocBakery.GuiChecks.dll --verify-design --owner
```

Thêm `-- --serve` vào lệnh PublicCatalog để mở fixture 5088. Chạy
`tests/PublicCatalog/warehouse-browser-check.cjs` với Playwright + Edge để kiểm tra UI;
script từ chối chạy trên cổng Web nhóm 5080. Database GUI thử được giữ theo cơ chế của
PrepareGuiFixture để có thể mở SSMS đối chiếu. Không chạy lại fixture ghi dữ liệu trên
database nhóm. Chưa kiểm tra nhiều máy chủ, dữ liệu lớn hoặc thao tác đồng thời từ hai
trình duyệt thực tế; transaction/concurrency đã được kiểm tra qua các ca SQL riêng.
