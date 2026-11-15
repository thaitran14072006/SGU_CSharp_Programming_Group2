# KT10–KT13 — Trần Thanh Nam

| Mã | Tiền điều kiện / đầu vào | Các bước | Kết quả mong đợi | Mức |
|---|---|---|---|---|
| KT10 | Khách, bánh, giá hợp lệ; tồn trước T. Đơn 200.000đ, cọc 50.000đ, ngày nhận tương lai | Tạo đơn, xem bảng DonDatBanh và LoBanh | Đơn mới; TongTien=200000; TienCoc=50000; tồn vẫn T | Tích hợp |
| KT11 | Đơn KT10 có hàng đủ giao | Chuẩn bị rồi giao, xem hóa đơn, chi tiết, tồn và báo cáo | Trạng thái DaGiao, một hóa đơn, còn thu 150000, doanh thu 200000, trừ đúng tồn | Tích hợp |
| KT12 | Đơn ở trạng thái DangChuanBi; thử thiếu tồn, giao hai lần/đồng thời, gây lỗi SQL giữa giao dịch trên CSDL thử | Thực hiện các tình huống riêng biệt; so trạng thái/hóa đơn/tồn trước và sau | Giao hỏng rollback, không có hóa đơn trùng, không trừ tồn dở dang | Tích hợp |
| KT13 | Đơn ở các trạng thái MoiTiepNhan/DangChuanBi/DaGiao | Thử hủy thiếu lý do, hủy có lý do, sửa/hủy đơn đã giao | Thiếu lý do bị chặn; chỉ hủy trước giao; đơn giao/hủy không được sửa | Nghiệp vụ |

Cần lưu: ngày giờ, người chạy, commit, tiền điều kiện, đầu vào, bước, kết quả thực tế, Đạt/Không đạt/Chưa chạy và ảnh/chứng cứ SQL. KT11 cần đối chiếu tổng hóa đơn, **không cộng lại tiền cọc**. KT12 chỉ chạy thao tác phá lỗi trên CSDL thử; không tác động dữ liệu vận hành.
