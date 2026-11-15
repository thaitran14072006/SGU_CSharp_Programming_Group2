using MocBakery.GUI.Forms.Catalog;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.GUI.Forms.Inventory;
internal sealed class InventoryPage:BakeryPage
{
    private readonly TextBox keyword=Design.Search("Tìm tên hàng hoặc mã lô...");
    private readonly ComboBox kind=Ui.Choice("Bánh thành phẩm","Nguyên liệu"),filter=Ui.Choice("Tất cả","Sắp hết","Sắp hết hạn","Đã hết hạn","Ngừng dùng");
    private readonly DataGridView grid=Design.Grid(("Code","Mã lô"),("Name","Tên hàng"),("Remaining","Tồn"),("Unit","ĐVT"),("Expiry","Hạn dùng"),("Warning","Cảnh báo"));
    private string Kind=>kind.SelectedIndex==0?ItemKinds.Cake:ItemKinds.Material;
    private WarehouseRow? Selected=>grid.SelectedRows.Count>0?grid.SelectedRows[0].DataBoundItem as WarehouseRow:null;
    internal InventoryPage(AppServices services,UserSession session):base(services,session,"Tồn kho và hạn dùng","Theo dõi tồn bánh và nguyên liệu theo lô, cảnh báo hạn dùng.")
    {
        var selection=new FlowLayoutPanel{WrapContents=false};kind.Width=185;keyword.Width=150;kind.Margin=Padding.Empty;keyword.Margin=Padding.Empty;selection.Controls.Add(kind);selection.Controls.Add(keyword);
        Tool("Loại hàng / Tìm hàng",selection,340);Tool("Bộ lọc",filter,250);
        if(Roles.CanManageInventory(session.VaiTro))Primary("Nhập kho",(_,_)=>Design.Open(this,new ReceiptPage(Services,Session)));
        grid.Columns["Expiry"]!.DefaultCellStyle.Format="dd/MM/yyyy";grid.Columns["Remaining"]!.DefaultCellStyle.Format="0.###";
        grid.Columns["Code"]!.FillWeight=80;grid.Columns["Name"]!.FillWeight=180;grid.Columns["Unit"]!.FillWeight=55;
        grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex>=0&&grid.Columns[e.ColumnIndex].Name=="Warning")e.CellStyle!.ForeColor=e.Value?.ToString()=="Hết hạn"?Color.Firebrick:e.Value?.ToString()=="Bình thường"?Color.SeaGreen:Design.Orange;};
        Body.Controls.Add(grid);Actions.Controls.Add(Design.Button("Chi tiết",async(_,_)=>await Details(),secondary:true));
        if(Roles.CanManageInventory(session.VaiTro))
        {
            var change=Design.Button("Xuất / Điều chỉnh",async(_,_)=>await Change());change.Width=160;Actions.Controls.Add(change);
            Actions.Controls.Add(Design.Button("Nguyên liệu",(_,_)=>Design.Open(this,new MaterialPage(Services,Session))));
        }
        grid.SelectionChanged+=(_,_)=>{foreach(var button in Actions.Controls.OfType<Button>().Where(b=>b.Text!="Nguyên liệu"))button.Enabled=Selected is not null;};
        kind.SelectedIndexChanged+=async(_,_)=>{if(IsHandleCreated)await Reload();};filter.SelectedIndexChanged+=async(_,_)=>{if(IsHandleCreated)await Reload();};
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Reload();}};
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
    }
    private Task Reload()=>Run(RefreshRows);
    private async Task RefreshRows()
    {
        string selected=Kind,key=keyword.Text,state=filter.SelectedIndex switch{1=>"Low",2=>"Soon",3=>"Expired",4=>"Stopped",_=>"All"};
        var rows=await Task.Run(()=>Services.Warehouse.Stock(Session,selected,key,state));grid.DataSource=rows.ToList();Count(rows.Count);
    }
    private Task Details()=>Run(async()=>{
        var row=Selected??throw new BusinessException("Chọn lô trước.");var history=await Task.Run(()=>Services.Warehouse.History(Session,row.Kind,row.Id));
        using var dialog=new Form();Ui.Style(dialog,"Chi tiết lô "+row.Code);dialog.ClientSize=new Size(900,500);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(20)};layout.RowStyles.Add(new(SizeType.Absolute,85));layout.RowStyles.Add(new(SizeType.Percent,100));dialog.Controls.Add(layout);
        layout.Controls.Add(new Label{Text=$"{row.Code} • {row.Name}\nTồn: {row.Remaining:0.###} {row.Unit} • Hạn dùng: {row.Expiry:dd/MM/yyyy} • {row.Warning}\nLịch sử nhập, xuất và kiểm kê",AutoSize=true},0,0);
        var list=Design.Grid(("Time","Thời điểm"),("Operation","Thao tác"),("Before","Số cũ"),("After","Số mới"),("Actor","Người làm"),("Reason","Lý do"));list.Columns["Time"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";list.DataSource=history.ToList();layout.Controls.Add(list,0,1);dialog.ShowDialog(this);
    });
    private Task Change()=>Run(async()=>{
        var row=Selected??throw new BusinessException("Chọn một lô hoặc mặt hàng trước.");
        ComboBox operation=Ui.Choice("Xuất hàng theo FEFO","Kiểm kê lô đã chọn"),reason=Ui.Choice("Sản xuất","Hủy","Kiểm kê");
        var quantity=Design.Number(1000000000,row.Kind==ItemKinds.Cake?0:3);quantity.Value=1;var note=Ui.Input(450);
        using var dialog=new EditDialog("Xuất / Điều chỉnh",[("Mặt hàng: "+row.Name,"Mode",operation),("Số lượng xuất / Tồn thực tế *","Quantity",quantity),("Loại lý do *","ReasonType",reason),("Diễn giải lý do *","Reason",note)],async()=>{
            if(operation.SelectedIndex==0&&reason.SelectedIndex==2)throw new BusinessException("Chọn Sản xuất hoặc Hủy khi xuất hàng.","ReasonType");
            if(operation.SelectedIndex==1&&reason.SelectedIndex!=2)throw new BusinessException("Chọn lý do Kiểm kê khi điều chỉnh tồn thực tế.","ReasonType");
            if(string.IsNullOrWhiteSpace(note.Text))throw new BusinessException("Nhập diễn giải lý do.","Reason");
            if(!Ui.Confirm(this,operation.SelectedIndex==0?"Xuất theo các lô gần hết hạn trước?":"Lưu số tồn kiểm kê và nhật ký thay đổi?"))throw new BusinessException("Chưa xác nhận thay đổi.");
            var input=new StockChange(row.Kind,row.Id,row.ItemId,operation.SelectedIndex==0?"Xuat":"KiemKe",quantity.Value,reason.Text+": "+note.Text.Trim());
            await Task.Run(()=>Services.Warehouse.ChangeStock(Session,input));
        });
        if(dialog.ShowDialog(this)==DialogResult.OK)await RefreshRows();
    });
    internal void Preview()
    {
        var today=DateOnly.FromDateTime(DateTime.Today);grid.DataSource=new[]{
            new WarehouseRow(ItemKinds.Cake,1,1,"L25101","Croissant bơ","Cái",today.AddDays(1),32,"Sắp hết hạn","DangBan",true),
            new WarehouseRow(ItemKinds.Cake,2,2,"L25102","Tiramisu mini","Cái",today.AddDays(5),18,"Bình thường","DangBan",true),
            new WarehouseRow(ItemKinds.Material,1,1,"NL25101","Bột mì số 13","Kg",today.AddDays(30),8,"Sắp hết","DangBan",true),
            new WarehouseRow(ItemKinds.Material,2,2,"NL25102","Bơ lạt","Kg",today.AddDays(15),12,"Bình thường","DangBan",true)};Count(4);
    }
}
