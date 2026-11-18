# Kết quả kiểm thử Như ngày 18 tháng 11 năm 2026

Windows, SDK 11.0.100 target .NET 10, SQL Server LocalDB, Edge qua Playwright.
Solution Release build đạt, 0 lỗi, 0 cảnh báo.

| Bộ console | Đạt | Lỗi |
| --- | ---: | ---: |
| PublicCatalog mới với SQL và HTTP thật | 36 | 0 |
| Unit chung | 45 | 0 |
| CatalogInventory | 43 | 0 |
| CustomerAccounts với bộ nhớ thử | 36 | 0 |
| WebAccounts HTTP với bộ nhớ thử | 23 | 0 |
| Tổng không cộng lượt lặp | 183 | 0 |

PublicCatalog chạy Release; hồi quy chạy Debug vừa build lại. Các chương trình console
không được phát hiện qua `dotnet test`. [Log mới](evidence/public-catalog-checks.txt).

Ca mới bao phủ công khai bánh/danh mục, danh mục rỗng, tìm tên/mã, escape LIKE/injection,
phối hợp lọc/sắp, phân trang giữ query, input sai trả 400, ID mất/ẩn trả 404, mã hóa XSS,
DTO không lộ tồn/tài khoản, no-store, header an toàn, ảnh sai và lỗi SQL trả 503 không lộ
kết nối. Tồn chỉ tính lô đang bán, còn lượng, đã sản xuất, chưa quá hạn; ngày hết hạn
vẫn tính. Fixture desktop và Web cùng 7 bánh hợp lệ.

Đã kiểm chứng chủ tiệm sửa giá 45.000 thành 49.000, thêm/xóa ảnh, ngừng bánh/danh mục,
ngừng lô: HTTP reload phản ánh đúng. Nhân viên không sửa giá qua BLL. CheckSelection
đọc giá/lượng mới, từ chối lượng sai và bánh ẩn, không giữ/trừ/ghi kho. Định dạng giá
Việt Nam giữ phần lẻ khi có.

Sáu trang (home, thực đơn, danh mục, chi tiết, tìm rỗng, đăng nhập) tại 320, 375, 768,
1440 px: 24 lượt không cuộn ngang, không lỗi JavaScript. Thao tác điện thoại tìm/sắp/
mở chi tiết đạt; mã hóa script và liên kết bỏ qua menu bằng bàn phím đạt.
[Kết quả trình duyệt](evidence/browser-result.json).
Đã xem lại bốn ảnh [home máy tính](evidence/home-1440.png),
[home điện thoại](evidence/home-375.png), [chi tiết máy tính](evidence/detail-1440.png),
[chi tiết điện thoại](evidence/detail-375.png). Dữ liệu thử có tên chứa ký tự đặc biệt;
fixture không có ảnh nên dùng placeholder. Phục vụ ảnh PNG được kiểm tra riêng qua HTTP.

Không kiểm thử tải lớn/Internet hoặc bấm trực tiếp WinForms trong đợt này. Sửa desktop
qua chính CatalogService, InventoryService, AuthService và repository GUI sử dụng.
Kết quả GUI ngày 15/11 là lịch sử riêng. Chưa nghiệm thu giỏ/đơn Mai/Nam; cần kiểm tra
chéo sau tích hợp. Không sửa schema hoặc CSDL nhóm.
