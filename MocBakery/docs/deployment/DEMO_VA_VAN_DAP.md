# Kịch bản demo và câu hỏi bảo vệ

Đọc `CHOT_DO_AN.md` và mở executable bản bàn giao trước buổi trình bày.
Thử trên CSDL demo riêng, không xóa hoặc sửa CSDL nhóm đang dùng. Không trình chiếu mật khẩu đang nhập.
Khoảng 10–15 phút cho kịch bản sau; đây là thời lượng gợi ý, không phải phép đo hiệu năng.

## Trình bày và demo

1. Giới thiệu Mộc Bakery, bốn lớp GUI → BLL → DAL → Models, CSDL SQL Server 16 bảng.
   Nêu phân công: Thái Accounts/Customers, Như Catalog/Inventory, Mai Sales, Nam Orders/Reports.
   Như tiếp nhận hoàn thiện phần Thái và tích hợp bản chốt.
2. Mở ứng dụng. Nếu CSDL mới, **Thiết lập lần đầu** để tạo quản lý rồi đăng nhập.
   Vào Nhân viên, bổ sung điện thoại quản lý; tạo một nhân viên Thu ngân có số 10 chữ số.
3. Vào Tài khoản, chọn nhân viên vừa tạo, đặt tên đăng nhập và mật khẩu tạm.
   Xem ma trận quyền. Đăng xuất, đăng nhập Thu ngân, đổi mật khẩu tạm và đăng nhập lại.
   Chỉ ra menu quản trị bị ẩn; BLL vẫn kiểm tra quyền khi gọi, không chỉ dựa vào menu.
4. Với Thu ngân, tạo khách hàng mới; nhập SĐT thiếu số để thấy lỗi rồi sửa đúng.
   Tìm lại theo SĐT, thử tạo trùng và chỉ ra giữ số 0 đầu, không tạo thêm hồ sơ trùng.
5. Với Quản lý, tạo danh mục/bánh hoặc dùng bánh mẫu. Mở Chi tiết bánh, sửa giá/hạn dùng/ảnh.
   Giá 0 hoặc tên trống bị chặn. Thử chuyển trang khi chưa lưu để thấy xác nhận.
6. Lập phiếu nhập: chọn NCC, thêm bánh và nguyên liệu với lô/NSX/HSD/lượng/giá.
   Lưu nháp rồi xem tồn chưa tăng. Ghi sổ và xem tồn tăng đúng; phiếu đã ghi sổ bị khóa sửa.
   Lọc sắp hết hạn/tồn thấp, giải thích nguyên tắc xuất FEFO và nhật ký kiểm kê.
7. Vào Bán hàng, thêm bánh vào giỏ, chỉnh số lượng và chọn khách.
   Thanh toán xong xem hóa đơn, lượng tồn và lịch sử mua của khách.
   Có thể không chọn khách để minh họa khách lẻ; không cần tạo hồ sơ cho mọi giao dịch.
8. Tạo đơn đặt có ngày nhận tương lai và cọc. Giải thích tạo đơn chưa trừ kho.
   Chuyển Chuẩn bị → Giao; xem hóa đơn và số tiền còn thu = tổng tiền − cọc.
   Thử giao lại/hủy đơn đã giao để thấy bị chặn.
9. Mở Báo cáo cùng khoảng ngày, đối chiếu tổng hóa đơn với doanh thu và bánh bán chạy.
   Giải thích tiền cọc không được cộng hai lần; CSV có thể mở bằng Excel.
10. Với Quản lý, cho nhân viên demo nghỉ việc sau xác nhận. Đăng nhập lại nhân viên sẽ bị chặn.
    Kết thúc bằng bảng 190 ca đạt, ví dụ rollback/concurrency và giới hạn kiểm tra máy thứ hai.

Nếu dữ liệu demo có nhiều giao dịch cũ, ghi lại tồn/doanh thu trước bước 7 để so chênh lệch.
Không dùng số 400.000đ của bộ kiểm thử làm doanh thu mặc định cho mọi CSDL demo.

## Những câu cần nắm

**Tại sao chia GUI/BLL/DAL?** GUI nhận và hiển thị; BLL kiểm tra dữ liệu, phiên và quyền;
DAL thực hiện truy vấn tham số/giao dịch; Models định nghĩa dữ liệu trao đổi. Giao diện gọi dịch vụ,
không tự viết SQL để quyết định quyền hay tính tồn.

**FEFO là gì?** Lấy lô có hạn dùng gần nhất trước, rồi đến mã lô khi cùng hạn.
Lô hết hạn/ngừng bán và bánh ngừng kinh doanh không được bán; ngày hết hạn vẫn có hiệu lực đến hết ngày.

**Làm sao tránh ghi sổ/giao đơn hai lần?** Transaction cùng khóa cập nhật/khóa phạm vi,
kiểm tra lại trạng thái trong transaction, ràng buộc liên kết hóa đơn và cập nhật tồn có điều kiện.
Phiếu nhập còn dùng rowversion để chặn sửa đè nháp.

**Nếu lỗi sau khi trừ một phần tồn?** Header, chi tiết, tồn và trạng thái cùng transaction;
không commit thì rollback toàn bộ. Bộ kiểm tra đã chèn lỗi SQL giữa chừng để xác nhận điều này.

**Hai người cùng mua bánh cuối?** Lô được khóa khi đọc phân bổ, lệnh trừ tồn còn kiểm tra lượng đủ.
Bộ kiểm tra đồng thời xác nhận đúng một giao dịch thành công và không âm tồn.

**Nháp khác ghi sổ thế nào?** Nháp lưu thông tin phiếu và tổng tiền, chưa tạo lô/tăng kho.
Ghi sổ kiểm tra NCC, mặt hàng, ngày/lượng/giá rồi tạo tất cả lô và nhật ký trong một transaction.

**Phân quyền có dựa vào combobox/menu không?** Không. Token phiên được phát khi đăng nhập;
BLL đọc lại tài khoản thật, hash, trạng thái và vai trò. Sửa dữ liệu hiển thị trong phiên không nâng được quyền.

**Lưu mật khẩu ra sao?** Hash PBKDF2 có salt ngẫu nhiên; không lưu mật khẩu rõ.
Không đọc lại được mật khẩu cũ, quản lý chỉ đặt lại mật khẩu tạm. Đổi/reset làm các phiên cũ mất hiệu lực.

**Tại sao tách Nhân viên và Tài khoản?** Hồ sơ là người và tình trạng làm việc; tài khoản là quyền truy cập.
Có thể tạo hồ sơ trước khi cấp tài khoản. Nghỉ việc giữ hồ sơ và lịch sử, đồng thời khóa đăng nhập liên quan.

**Vì sao doanh thu khác tiền thu của hóa đơn giao đơn?** Doanh thu lấy toàn bộ tổng hóa đơn;
tiền còn thu đã trừ cọc nhận trước. Không cộng lại cọc vào tổng doanh thu lần nữa.

**Có hạn chế gì?** Chưa quản lý nhiều chi nhánh, chưa tích hợp thanh toán điện tử,
chưa đánh giá tải lớn/máy thứ hai/máy in thật. Xuất báo cáo là CSV, không phải workbook XLSX.
