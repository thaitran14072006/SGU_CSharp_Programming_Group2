namespace MocBakery.Models.DTOs.Inventory;
public static class ItemKinds { public const string Cake="Banh", Material="NguyenLieu"; }
public sealed record Supplier(int Id,string Name,string Phone,string? Address,string? Email,bool Active=true);
public sealed record Material(int Id,string Name,string Unit,bool Active=true,decimal LowStock=5);
public sealed record WarehouseItem(string Kind,int Id,string Name,string Unit,bool Active,decimal LowStock,int ShelfDays=1)
{
    public string Code => (Kind==ItemKinds.Cake?"B":"NL")+Id.ToString("000");
    public override string ToString()=>Code+" - "+Name;
}
public sealed record ReceiptLine(string Kind,int ItemId,string LotCode,DateOnly Produced,DateOnly Expiry,
    decimal Quantity,string Unit,decimal Cost)
{
    public decimal Amount=>decimal.Round(Quantity*Cost,2,MidpointRounding.AwayFromZero);
}
public sealed record Receipt(int Id,int SupplierId,DateTime Date,string State,IReadOnlyList<ReceiptLine> Lines,byte[]? Version=null)
{
    public string Code=>Id==0?"Tự sinh":"PN"+Id.ToString("000");
    public decimal Total=>Lines.Sum(l=>l.Amount);
}
public sealed record ReceiptSummary(int Id,string Supplier,DateTime Date,string State,decimal Total)
{
    public string Code=>"PN"+Id.ToString("000");
    public string Status=>State=="Nhap"?"Nháp":"Đã ghi sổ";
}
public sealed record WarehouseLot(string Kind,int Id,int ItemId,string Code,string Name,string Unit,
    DateOnly Produced,DateOnly Expiry,decimal Received,decimal Remaining,bool Active,string State,decimal LowStock);
public sealed record WarehouseRow(string Kind,int Id,int ItemId,string Code,string Name,string Unit,
    DateOnly Expiry,decimal Remaining,string Warning,string State,bool Sellable);
public sealed record StockChange(string Kind,int LotId,int ItemId,string Operation,decimal Quantity,string Reason);
public sealed record StockHistory(DateTime Time,string Operation,decimal Before,decimal After,string Actor,string Reason);
