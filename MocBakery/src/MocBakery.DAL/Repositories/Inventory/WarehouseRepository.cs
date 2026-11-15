using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.DAL.Repositories.Inventory;
public sealed partial class WarehouseRepository(DbConnectionFactory db):IWarehouseRepository
{
    public IReadOnlyList<Supplier> Suppliers()
    {
        using var c=db.Open();using var q=Db.Command(c,"SELECT MaNCC,TenNCC,DienThoai,DiaChi,Email,DangHopTac FROM dbo.NhaCungCap ORDER BY TenNCC");
        using var r=q.ExecuteReader();List<Supplier> rows=[];
        while(r.Read())rows.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),Text(r,3),Text(r,4),r.GetBoolean(5)));return rows;
    }
    public int SaveSupplier(Supplier s)
    {
        using var c=db.Open();using var q=Command(c,null,s.Id==0?
            "INSERT dbo.NhaCungCap(TenNCC,DienThoai,DiaChi,Email,DangHopTac) OUTPUT INSERTED.MaNCC VALUES(@name,@phone,@address,@email,@active)":
            "UPDATE dbo.NhaCungCap SET TenNCC=@name,DienThoai=@phone,DiaChi=@address,Email=@email,DangHopTac=@active OUTPUT INSERTED.MaNCC WHERE MaNCC=@id",
            ("@name",s.Name),("@phone",s.Phone),("@address",s.Address),("@email",s.Email),("@active",s.Active),("@id",s.Id));
        try{return Id(q);}catch(DbException ex) when(Db.IsDuplicate(ex)){throw new BusinessException("Tên nhà cung cấp đã tồn tại.","Name");}
    }
    public IReadOnlyList<Material> Materials()
    {
        using var c=db.Open();using var q=Db.Command(c,"SELECT MaNguyenLieu,TenNguyenLieu,DonViTinh,DangSuDung,NguongCanhBao FROM dbo.NguyenLieu ORDER BY TenNguyenLieu");
        using var r=q.ExecuteReader();List<Material> rows=[];while(r.Read())rows.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetBoolean(3),r.GetDecimal(4)));return rows;
    }
    public int SaveMaterial(Material m)
    {
        using var c=db.Open();using var q=Command(c,null,m.Id==0?
            "INSERT dbo.NguyenLieu(TenNguyenLieu,DonViTinh,DangSuDung,NguongCanhBao) OUTPUT INSERTED.MaNguyenLieu VALUES(@name,@unit,@active,@low)":
            "UPDATE dbo.NguyenLieu SET TenNguyenLieu=@name,DonViTinh=@unit,DangSuDung=@active,NguongCanhBao=@low OUTPUT INSERTED.MaNguyenLieu WHERE MaNguyenLieu=@id",
            ("@name",m.Name),("@unit",m.Unit),("@active",m.Active),("@low",m.LowStock),("@id",m.Id));
        try{return Id(q);}catch(DbException ex) when(Db.IsDuplicate(ex)){throw new BusinessException("Tên nguyên liệu đã tồn tại.","Name");}
    }
    public IReadOnlyList<WarehouseItem> Items()
    {
        using var c=db.Open();using var q=Db.Command(c,"""
            SELECT N'Banh',MaSanPham,TenSanPham,DonViTinh,DangKinhDoanh,NguongCanhBao,HanDungNgay FROM dbo.SanPham
            UNION ALL SELECT N'NguyenLieu',MaNguyenLieu,TenNguyenLieu,DonViTinh,DangSuDung,NguongCanhBao,1 FROM dbo.NguyenLieu
            """);using var r=q.ExecuteReader();List<WarehouseItem> rows=[];
        while(r.Read())rows.Add(new(r.GetString(0),r.GetInt32(1),r.GetString(2),r.GetString(3),r.GetBoolean(4),r.GetDecimal(5),r.GetInt32(6)));return rows;
    }
    public IReadOnlyList<WarehouseLot> Lots()
    {
        using var c=db.Open();using var q=Db.Command(c,"""
            SELECT N'Banh',l.MaLo,l.MaSanPham,COALESCE(l.SoLo,N'L'+CONVERT(NVARCHAR(20),l.MaLo)),p.TenSanPham,p.DonViTinh,l.NgaySanXuat,l.HanSuDung,
                   CAST(l.SoLuongNhap AS DECIMAL(18,3)),CAST(l.SoLuongCon AS DECIMAL(18,3)),p.DangKinhDoanh,l.TinhTrang,p.NguongCanhBao
            FROM dbo.LoBanh l JOIN dbo.SanPham p ON p.MaSanPham=l.MaSanPham
            UNION ALL
            SELECT N'NguyenLieu',l.MaLo,l.MaNguyenLieu,l.SoLo,p.TenNguyenLieu,p.DonViTinh,l.NgaySanXuat,l.HanSuDung,l.SoLuongNhap,l.SoLuongCon,p.DangSuDung,l.TinhTrang,p.NguongCanhBao
            FROM dbo.LoNguyenLieu l JOIN dbo.NguyenLieu p ON p.MaNguyenLieu=l.MaNguyenLieu
            """);using var r=q.ExecuteReader();List<WarehouseLot> rows=[];
        while(r.Read())rows.Add(new(r.GetString(0),r.GetInt32(1),r.GetInt32(2),r.GetString(3),r.GetString(4),r.GetString(5),
            DateOnly.FromDateTime(r.GetDateTime(6)),DateOnly.FromDateTime(r.GetDateTime(7)),r.GetDecimal(8),r.GetDecimal(9),r.GetBoolean(10),r.GetString(11),r.GetDecimal(12)));return rows;
    }
    public IReadOnlyList<ReceiptSummary> Receipts()
    {
        using var c=db.Open();using var q=Db.Command(c,"""
            SELECT p.MaPhieu,n.TenNCC,p.NgayNhap,p.TrangThai,COALESCE(SUM(d.ThanhTien),0)
            FROM dbo.PhieuNhap p JOIN dbo.NhaCungCap n ON n.MaNCC=p.MaNCC LEFT JOIN dbo.ChiTietPhieuNhap d ON d.MaPhieu=p.MaPhieu
            GROUP BY p.MaPhieu,n.TenNCC,p.NgayNhap,p.TrangThai ORDER BY p.MaPhieu DESC
            """);using var r=q.ExecuteReader();List<ReceiptSummary> rows=[];
        while(r.Read())rows.Add(new(r.GetInt32(0),r.GetString(1),r.GetDateTime(2),r.GetString(3),r.GetDecimal(4)));return rows;
    }
    public Receipt GetReceipt(int id)
    {
        using var c=db.Open();return ReadReceipt(c,null,id,false);
    }
    private static Receipt ReadReceipt(DbConnection c,DbTransaction? tx,int id,bool locked)
    {
        int supplier;DateTime date;string state;byte[] version;
        using(var q=Command(c,tx,$"SELECT MaNCC,NgayNhap,TrangThai,PhienBan FROM dbo.PhieuNhap {(locked?"WITH (UPDLOCK,HOLDLOCK)":"")} WHERE MaPhieu=@id",("@id",id)))
        {using var r=q.ExecuteReader();if(!r.Read())throw new BusinessException("Phiếu không còn tồn tại.");supplier=r.GetInt32(0);date=r.GetDateTime(1);state=r.GetString(2);version=(byte[])r.GetValue(3);}
        using var lines=Command(c,tx,"SELECT LoaiHang,COALESCE(MaSanPham,MaNguyenLieu),SoLo,NgaySanXuat,HanSuDung,SoLuong,DonViTinh,DonGia FROM dbo.ChiTietPhieuNhap WHERE MaPhieu=@id ORDER BY MaDong",("@id",id));
        using var reader=lines.ExecuteReader();List<ReceiptLine> rows=[];
        while(reader.Read())rows.Add(new(reader.GetString(0),reader.GetInt32(1),reader.GetString(2),DateOnly.FromDateTime(reader.GetDateTime(3)),DateOnly.FromDateTime(reader.GetDateTime(4)),reader.GetDecimal(5),reader.GetString(6),reader.GetDecimal(7)));
        return new(id,supplier,date,state,rows,version);
    }
    public IReadOnlyList<StockHistory> History(string kind,int lotId)
    {
        string column=kind==ItemKinds.Cake?"MaLoBanh":kind==ItemKinds.Material?"MaLoNguyenLieu":throw new BusinessException("Loại hàng không hợp lệ.");
        using var c=db.Open();using var q=Command(c,null,$"SELECT b.ThoiDiem,b.LoaiThaoTac,b.SoCu,b.SoMoi,t.HoTen,b.LyDo FROM dbo.BienDongKho b JOIN dbo.TaiKhoan t ON t.MaTaiKhoan=b.MaNguoiLam WHERE b.{column}=@id ORDER BY b.MaBienDong DESC",("@id",lotId));
        using var r=q.ExecuteReader();List<StockHistory> rows=[];while(r.Read())rows.Add(new(r.GetDateTime(0),r.GetString(1),r.GetDecimal(2),r.GetDecimal(3),r.GetString(4),r.GetString(5)));return rows;
    }
    private static string? Text(DbDataReader reader,int ordinal)=>reader.IsDBNull(ordinal)?null:reader.GetString(ordinal);
    private static int Id(DbCommand q)=>Convert.ToInt32(q.ExecuteScalar()??throw new BusinessException("Dữ liệu đã thay đổi. Hãy tải lại."));
    private static DbCommand Command(DbConnection c,DbTransaction? tx,string sql,params (string Name,object? Value)[] args)
    {
        var q=Db.Command(c,sql,tx);
        foreach(var (name,value) in args)
        {
            object? input=value is DateOnly d?d.ToDateTime(TimeOnly.MinValue):value;
            DbType type=input switch{int=>DbType.Int32,decimal=>DbType.Decimal,bool=>DbType.Boolean,DateTime=>DbType.DateTime2,byte[]=>DbType.Binary,_=>DbType.String};
            q.Param(name,input,type,input is string or byte[]?-1:0);
            if(type==DbType.Decimal){q.Parameters[name].Precision=18;q.Parameters[name].Scale=3;}
        }
        return q;
    }
    private static void Actor(DbConnection c,DbTransaction tx,int actor)
    {
        using var q=Command(c,tx,"SELECT MaTaiKhoan FROM dbo.TaiKhoan WITH(UPDLOCK,HOLDLOCK) WHERE MaTaiKhoan=@id AND TrangThai=1 AND VaiTro IN(N'ChuTiem',N'Kho')",("@id",actor));
        if(q.ExecuteScalar() is null)throw new BusinessException("Không còn quyền thay đổi kho.");
    }
    private static void ValidateLines(DbConnection c,DbTransaction tx,Receipt receipt)
    {
        using(var q=Command(c,tx,"SELECT MaNCC FROM dbo.NhaCungCap WITH(UPDLOCK,HOLDLOCK) WHERE MaNCC=@id AND DangHopTac=1",("@id",receipt.SupplierId)))
            if(q.ExecuteScalar() is null)throw new BusinessException("Nhà cung cấp đã ngừng hợp tác.","Supplier");
        if(receipt.Lines.Count==0||receipt.Date>DateTime.Now)throw new BusinessException("Phiếu hoặc ngày nhập không hợp lệ.");
        foreach(var line in receipt.Lines)
        {
            var (table,key)=ItemTable(line.Kind);
            string active=line.Kind==ItemKinds.Cake?"DangKinhDoanh":"DangSuDung";
            using var q=Command(c,tx,$"SELECT DonViTinh FROM dbo.{table} WITH(UPDLOCK,HOLDLOCK) WHERE {key}=@id AND {active}=1",("@id",line.ItemId));
            if(q.ExecuteScalar() is not string unit||unit!=line.Unit)throw new BusinessException("Mặt hàng/đơn vị đã đổi hoặc ngừng dùng. Tải lại phiếu.");
            var date=DateOnly.FromDateTime(receipt.Date);
            if(line.Produced>date||line.Expiry<date||line.Quantity<=0||line.Cost<0||(line.Kind==ItemKinds.Cake&&(line.Quantity!=decimal.Truncate(line.Quantity)||line.Quantity>int.MaxValue)))
                throw new BusinessException("Dòng nhập có số lượng, giá hoặc ngày tháng không hợp lệ.");
        }
    }
    private static (string Table,string Key) ItemTable(string kind)=>kind switch{ItemKinds.Cake=>("SanPham","MaSanPham"),ItemKinds.Material=>("NguyenLieu","MaNguyenLieu"),_=>throw new BusinessException("Loại hàng không hợp lệ.")};
}
