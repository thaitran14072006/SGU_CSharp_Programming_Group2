using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.DAL.Repositories.Inventory;
using MocBakery.Models.DTOs.Customers;
using MocBakery.Models.DTOs.Sales;
using MocBakery.Models.Entities;

namespace MocBakery.DAL.Repositories.Sales;

public sealed class SalesRepository(DbConnectionFactory db) : ISalesRepository
{
    private const string Sellable = LoBanhRepository.SellableLotCondition;

    public IReadOnlyList<SalesProduct> ListProducts()
    {
        using var connection = db.Open();
        using var command = Db.Command(connection, $"""
            SELECT p.MaSanPham,p.TenSanPham,p.GiaBan,COALESCE(SUM(CONVERT(bigint,l.SoLuongCon)),0),c.TenLoai
            FROM dbo.SanPham p JOIN dbo.LoaiBanh c ON c.MaLoai=p.MaLoai
            LEFT JOIN dbo.LoBanh l ON l.MaSanPham=p.MaSanPham AND {Sellable}
            WHERE p.DangKinhDoanh=1
            GROUP BY p.MaSanPham,p.TenSanPham,p.GiaBan,c.TenLoai ORDER BY p.TenSanPham
            """);
        command.Param("@day", ReadClock(connection).Date, DbType.Date);
        using var reader = command.ExecuteReader();
        var result = new List<SalesProduct>();
        while (reader.Read())
            result.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetDecimal(2), reader.GetInt64(3))
                { Category = reader.GetString(4) });
        return result;
    }

    public IReadOnlyList<InvoiceSummary> ListInvoices()
    {
        using var connection = db.Open();
        using var command = Db.Command(connection, """
            SELECT h.MaHoaDon,h.NgayLap,h.TongTien,h.TienCocTru,h.SoTienThu,
                   COALESCE(k.HoTen,N'Khách lẻ'),COALESCE(k.SoDienThoai,'')
            FROM dbo.HoaDon h LEFT JOIN dbo.KhachHang k ON k.MaKhachHang=h.MaKhachHang
            ORDER BY h.NgayLap DESC,h.MaHoaDon DESC
            """);
        using var reader = command.ExecuteReader();
        var result = new List<InvoiceSummary>();
        while (reader.Read())
            result.Add(new(reader.GetInt32(0), reader.GetDateTime(1), reader.GetDecimal(2), reader.GetDecimal(3), reader.GetDecimal(4))
                { CustomerName = reader.GetString(5), Phone = reader.GetString(6) });
        return result;
    }

    public IReadOnlyList<InvoiceLine> ReadInvoice(int invoiceId)
    {
        using var connection = db.Open();
        using var command = Db.Command(connection, """
            SELECT d.MaLo,p.TenSanPham,d.SoLuong,d.DonGia FROM dbo.ChiTietHoaDon d
            JOIN dbo.LoBanh l ON l.MaLo=d.MaLo JOIN dbo.SanPham p ON p.MaSanPham=l.MaSanPham
            WHERE d.MaHoaDon=@id ORDER BY p.TenSanPham,l.HanSuDung,d.MaLo
            """);
        command.Param("@id", invoiceId, DbType.Int32);
        using var reader = command.ExecuteReader();
        var result = new List<InvoiceLine>();
        while (reader.Read())
            result.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetDecimal(3)));
        return result;
    }

    public int Sell(int accountId, int? customerId, IReadOnlyList<CartItem> cart,
        Func<IEnumerable<LoBanh>, int, IReadOnlyList<LotAllocation>> allocate)
    {
        using var connection = db.Open();
        using var transaction = connection.BeginTransaction();
        int id = WriteInvoice(transaction, accountId, customerId, null, 0, cart, allocate);
        transaction.Commit();
        return id;
    }

    // Orders can call this within their transaction and commit after updating the order.
    // For orders the caller supplies the stored order prices and deposit.
    public static int WriteInvoice(DbTransaction transaction, int accountId, int? customerId,
        int? orderId, decimal deposit, IReadOnlyList<CartItem> cart,
        Func<IEnumerable<LoBanh>, int, IReadOnlyList<LotAllocation>> allocate)
    {
        if (cart.Count == 0 || cart.Any(item => item.ProductId <= 0 || item.Quantity <= 0 || item.UnitPrice < 0) ||
            cart.Select(item => item.ProductId).Distinct().Count() != cart.Count)
            throw new BusinessException("Danh sách sản phẩm lập hóa đơn không hợp lệ.");
        var connection = transaction.Connection ?? throw new InvalidOperationException("Giao dịch đã kết thúc.");
        DateTime now = ReadClock(connection, transaction);
        var invoice = new HoaDon { MaTK = accountId, MaKH = customerId, MaDon = orderId, NgayLap = now };
        foreach (var item in cart.OrderBy(item => item.ProductId))
        {
            using var product = Db.Command(connection, """
                SELECT GiaBan FROM dbo.SanPham WITH (UPDLOCK,HOLDLOCK)
                WHERE MaSanPham=@id AND DangKinhDoanh=1
                """, transaction);
            product.Param("@id", item.ProductId, DbType.Int32);
            if (product.ExecuteScalar() is not decimal currentPrice)
                throw new BusinessException("Sản phẩm đã ngừng kinh doanh hoặc không còn tồn tại.");
            if (orderId is null && currentPrice != item.UnitPrice)
                throw new BusinessException("Giá bán đã thay đổi. Hãy tải lại danh sách bánh.");
            var available = LoBanhRepository.ReadSellableLots(connection, transaction, item.ProductId,
                DateOnly.FromDateTime(now));
            foreach (var allocation in allocate(available, item.Quantity))
                invoice.ChiTiet.Add(new() { MaLo = allocation.LotId, SoLuong = allocation.Quantity, DonGia = item.UnitPrice });
        }

        invoice.TongTien = invoice.ChiTiet.Sum(line => line.ThanhTien);
        using var header = Db.Command(connection, """
            INSERT dbo.HoaDon(NgayLap,MaTaiKhoan,MaKhachHang,MaDon,TongTien,TienCocTru)
            OUTPUT INSERTED.MaHoaDon VALUES(@now,@actor,@customer,@order,@total,@deposit)
            """, transaction);
        header.Param("@now", now, DbType.DateTime2);
        header.Param("@actor", invoice.MaTK, DbType.Int32);
        header.Param("@customer", invoice.MaKH, DbType.Int32);
        header.Param("@order", invoice.MaDon, DbType.Int32);
        header.Param("@total", invoice.TongTien, DbType.Decimal);
        header.Param("@deposit", deposit, DbType.Decimal);
        int id = Convert.ToInt32(header.ExecuteScalar());

        foreach (var line in invoice.ChiTiet)
        {
            using var stock = Db.Command(connection, $"""
                UPDATE l SET SoLuongCon=SoLuongCon-@quantity FROM dbo.LoBanh l
                WHERE l.MaLo=@lot AND l.SoLuongCon>=@quantity AND {Sellable}
                """, transaction);
            stock.Param("@quantity", line.SoLuong, DbType.Int32);
            stock.Param("@lot", line.MaLo, DbType.Int32);
            stock.Param("@day", now.Date, DbType.Date);
            if (stock.ExecuteNonQuery() != 1) throw new BusinessException("Tồn kho đã thay đổi.");

            using var detail = Db.Command(connection, """
                INSERT dbo.ChiTietHoaDon(MaHoaDon,MaLo,SoLuong,DonGia)
                VALUES(@id,@lot,@quantity,@price)
                """, transaction);
            detail.Param("@id", id, DbType.Int32);
            detail.Param("@lot", line.MaLo, DbType.Int32);
            detail.Param("@quantity", line.SoLuong, DbType.Int32);
            detail.Param("@price", line.DonGia, DbType.Decimal);
            detail.ExecuteNonQuery();
        }
        return id;
    }

    private static DateTime ReadClock(DbConnection connection, DbTransaction? transaction = null)
    {
        using var command = Db.Command(connection, "SELECT SYSDATETIME()", transaction);
        return (DateTime)command.ExecuteScalar()!;
    }
}
