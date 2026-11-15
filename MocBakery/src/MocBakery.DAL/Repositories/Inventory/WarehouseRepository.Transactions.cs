using System.Data;
using System.Data.Common;
using MocBakery.Models;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.DAL.Repositories.Inventory;
public sealed partial class WarehouseRepository
{
    public int SaveDraft(int actor,Receipt input)
    {
        using var c=db.Open();using var tx=c.BeginTransaction();Actor(c,tx,actor);ValidateLines(c,tx,input);
        int id=input.Id;
        if(id==0)
        {
            using var q=Command(c,tx,"INSERT dbo.PhieuNhap(MaNCC,NgayNhap,MaNguoiLap) OUTPUT INSERTED.MaPhieu VALUES(@supplier,@date,@actor)",("@supplier",input.SupplierId),("@date",input.Date),("@actor",actor));id=Id(q);
        }
        else
        {
            var original=ReadReceipt(c,tx,id,true);
            if(original.State!="Nhap")throw new BusinessException("Phiếu đã ghi sổ, không được sửa.");
            if(input.Version is null||!input.Version.SequenceEqual(original.Version!))throw new BusinessException("Phiếu đã được người khác sửa. Hãy tải lại.");
            using var q=Command(c,tx,"UPDATE dbo.PhieuNhap SET MaNCC=@supplier,NgayNhap=@date WHERE MaPhieu=@id; DELETE dbo.ChiTietPhieuNhap WHERE MaPhieu=@id",("@supplier",input.SupplierId),("@date",input.Date),("@id",id));q.ExecuteNonQuery();
        }
        foreach(var l in input.Lines)
        {
            using var q=Command(c,tx,"""
                INSERT dbo.ChiTietPhieuNhap(MaPhieu,LoaiHang,MaSanPham,MaNguyenLieu,SoLo,NgaySanXuat,HanSuDung,SoLuong,DonViTinh,DonGia)
                VALUES(@id,@kind,@product,@material,@lot,@produced,@expiry,@qty,@unit,@cost)
                """,("@id",id),("@kind",l.Kind),("@product",l.Kind==ItemKinds.Cake?l.ItemId:null),("@material",l.Kind==ItemKinds.Material?l.ItemId:null),
                ("@lot",l.LotCode),("@produced",l.Produced),("@expiry",l.Expiry),("@qty",l.Quantity),("@unit",l.Unit),("@cost",l.Cost));q.ExecuteNonQuery();
        }
        tx.Commit();return id;
    }
    public void Post(int actor,int id)
    {
        using var c=db.Open();using var tx=c.BeginTransaction();Actor(c,tx,actor);
        var receipt=ReadReceipt(c,tx,id,true);
        if(receipt.State!="Nhap")throw new BusinessException("Phiếu đã ghi sổ; không tăng tồn lần thứ hai.");
        ValidateLines(c,tx,receipt);
        foreach(var l in receipt.Lines)
        {
            string table=l.Kind==ItemKinds.Cake?"LoBanh":"LoNguyenLieu",key=l.Kind==ItemKinds.Cake?"MaSanPham":"MaNguyenLieu";
            using(var check=Command(c,tx,$"SELECT MaLo FROM dbo.{table} WITH(UPDLOCK,HOLDLOCK) WHERE {key}=@item AND SoLo=@lot",("@item",l.ItemId),("@lot",l.LotCode)))
                if(check.ExecuteScalar() is not null)throw new BusinessException("Số lô đã tồn tại cho mặt hàng. Đổi số lô trong phiếu nháp.");
            using var q=Command(c,tx,$"INSERT dbo.{table}({key},SoLo,NgayNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon,TinhTrang) OUTPUT INSERTED.MaLo VALUES(@item,@lot,@date,@produced,@expiry,@qty,@qty,N'DangBan')",
                ("@item",l.ItemId),("@lot",l.LotCode),("@date",receipt.Date),("@produced",l.Produced),("@expiry",l.Expiry),("@qty",l.Quantity));
            int lot=Id(q);Audit(c,tx,actor,l.Kind,lot,"Nhap",0,l.Quantity,"Ghi sổ "+receipt.Code,id);
        }
        using(var q=Command(c,tx,"UPDATE dbo.PhieuNhap SET TrangThai=N'DaGhiSo',MaNguoiGhi=@actor,NgayGhi=SYSDATETIME() WHERE MaPhieu=@id AND TrangThai=N'Nhap'",("@actor",actor),("@id",id)))
            if(q.ExecuteNonQuery()!=1)throw new BusinessException("Phiếu đã được ghi sổ bởi phiên khác.");
        tx.Commit();
    }
    public void ChangeStock(int actor,StockChange change)
    {
        var (itemTable,key)=ItemTable(change.Kind);string table=change.Kind==ItemKinds.Cake?"LoBanh":"LoNguyenLieu";
        string active=change.Kind==ItemKinds.Cake?"DangKinhDoanh":"DangSuDung";
        using var c=db.Open();using var tx=c.BeginTransaction();Actor(c,tx,actor);
        if(change.Operation=="KiemKe")
        {
            using var q=Command(c,tx,$"SELECT SoLuongCon FROM dbo.{table} WITH(UPDLOCK,HOLDLOCK) WHERE MaLo=@id AND {key}=@item",("@id",change.LotId),("@item",change.ItemId));
            object? value=q.ExecuteScalar();if(value is null)throw new BusinessException("Lô không còn tồn tại.");
            decimal before=Convert.ToDecimal(value);
            if(before==change.Quantity)throw new BusinessException("Số kiểm kê không đổi; không cần điều chỉnh.");
            UpdateLot(c,tx,table,change.LotId,before,change.Quantity);Audit(c,tx,actor,change.Kind,change.LotId,"KiemKe",before,change.Quantity,change.Reason,null);
        }
        else if(change.Operation=="Xuat")
        {
            bool disposal=change.Reason.StartsWith("Hủy:",StringComparison.Ordinal);
            using var q=Command(c,tx,$"""
                SELECT l.MaLo,l.SoLuongCon FROM dbo.{table} l WITH(UPDLOCK,HOLDLOCK)
                JOIN dbo.{itemTable} i WITH(HOLDLOCK) ON i.{key}=l.{key}
                WHERE l.{key}=@item AND l.SoLuongCon>0
                {(disposal?"":"AND i."+active+"=1 AND l.TinhTrang=N'DangBan' AND l.NgaySanXuat<=CAST(GETDATE() AS DATE) AND l.HanSuDung>=CAST(GETDATE() AS DATE)")}
                ORDER BY l.HanSuDung,l.MaLo
                """,("@item",change.ItemId));
            List<(int Id,decimal Quantity)> rows=[];using(var r=q.ExecuteReader())while(r.Read())rows.Add((r.GetInt32(0),Convert.ToDecimal(r.GetValue(1))));
            if(change.Quantity<=0||rows.Sum(l=>l.Quantity)<change.Quantity)throw new BusinessException("Số lượng xuất vượt tồn hợp lệ.","Quantity");
            decimal remaining=change.Quantity;
            foreach(var lot in rows)
            {
                decimal amount=Math.Min(remaining,lot.Quantity);if(amount==0)break;
                UpdateLot(c,tx,table,lot.Id,lot.Quantity,lot.Quantity-amount);Audit(c,tx,actor,change.Kind,lot.Id,"Xuat",lot.Quantity,lot.Quantity-amount,change.Reason,null);remaining-=amount;
            }
        }
        else throw new BusinessException("Loại điều chỉnh không hợp lệ.");
        tx.Commit();
    }
    private static void UpdateLot(DbConnection c,DbTransaction tx,string table,int id,decimal before,decimal after)
    {
        using var q=Command(c,tx,$"UPDATE dbo.{table} SET SoLuongCon=@after WHERE MaLo=@id AND SoLuongCon=@before",("@after",after),("@id",id),("@before",before));
        if(q.ExecuteNonQuery()!=1)throw new BusinessException("Tồn đã thay đổi. Tải lại và thử lại.");
    }
    private static void Audit(DbConnection c,DbTransaction tx,int actor,string kind,int lot,string operation,decimal before,decimal after,string reason,int? receipt)
    {
        using var q=Command(c,tx,"INSERT dbo.BienDongKho(MaLoBanh,MaLoNguyenLieu,MaPhieu,LoaiThaoTac,SoCu,SoMoi,MaNguoiLam,LyDo) VALUES(@cake,@material,@receipt,@operation,@before,@after,@actor,@reason)",
            ("@cake",kind==ItemKinds.Cake?lot:null),("@material",kind==ItemKinds.Material?lot:null),("@receipt",receipt),("@operation",operation),("@before",before),("@after",after),("@actor",actor),("@reason",reason));q.ExecuteNonQuery();
    }
}
