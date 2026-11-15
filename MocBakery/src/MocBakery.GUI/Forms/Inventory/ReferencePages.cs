using MocBakery.GUI.Forms.Catalog;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.GUI.Forms.Inventory;
internal sealed class SupplierPage:BakeryPage
{
    private readonly TextBox keyword=Design.Search("Mã / Tên / Số điện thoại...");private readonly ComboBox state=Ui.Choice("Tất cả","Đang hợp tác","Ngừng hợp tác");
    private readonly DataGridView grid=Design.Grid(("Code","Mã"),("Name","Tên đơn vị"),("Phone","Điện thoại"),("Address","Địa chỉ"),("Email","Email"),("Status","Trạng thái"));
    private sealed record Row(Supplier Source){public string Code=>"NCC"+Source.Id.ToString("000");public string Name=>Source.Name;public string Phone=>Source.Phone;public string? Address=>Source.Address;public string? Email=>Source.Email;public string Status=>Source.Active?"Đang hợp tác":"Ngừng hợp tác";}
    private Supplier? Selected=>grid.SelectedRows.Count>0?(grid.SelectedRows[0].DataBoundItem as Row)?.Source:null;
    internal SupplierPage(AppServices s,UserSession user):base(s,user,"Nhà cung cấp","Quản lý các đơn vị cung cấp bánh và nguyên liệu.")
    {
        Tool("Tìm nhà cung cấp",keyword,340);Tool("Bộ lọc",state,250);Body.Controls.Add(grid);
        if(Roles.CanManageInventory(user.VaiTro)){Primary("+ Thêm mới",async(_,_)=>await Edit(false));Actions.Controls.Add(Design.Button("Sửa",async(_,_)=>await Edit(true)));}
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Reload();}};state.SelectedIndexChanged+=async(_,_)=>{if(IsHandleCreated)await Reload();};Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
    }
    private Task Reload()=>Run(RefreshRows);
    private async Task RefreshRows(){var rows=await Task.Run(()=>Services.Warehouse.Suppliers(Session));grid.DataSource=rows.Where(s=>(state.SelectedIndex==0||s.Active==(state.SelectedIndex==1))&&(s.Name.Contains(keyword.Text,StringComparison.CurrentCultureIgnoreCase)||s.Phone.Contains(keyword.Text)||("NCC"+s.Id.ToString("000")).Equals(keyword.Text,StringComparison.OrdinalIgnoreCase))).Select(s=>new Row(s)).ToList();Count(grid.Rows.Count);}
    private Task Edit(bool existing)=>Run(async()=>{
        var s=existing?Selected:null;if(existing&&s is null)throw new BusinessException("Chọn nhà cung cấp trước.");
        var name=Ui.Input(150);name.Text=s?.Name;var phone=Ui.Input(11);phone.Text=s?.Phone;var address=Ui.Input(255);address.Text=s?.Address;var email=Ui.Input(255);email.Text=s?.Email;var active=Ui.Choice("Đang hợp tác","Ngừng hợp tác");active.SelectedIndex=s?.Active is false?1:0;
        using var dialog=new EditDialog("Nhà cung cấp",[("Tên đơn vị *","Name",name),("Điện thoại *","Phone",phone),("Địa chỉ","Address",address),("Email","Email",email),("Trạng thái","Active",active)],async()=>{
            if(s is{Active:true}&&active.SelectedIndex==1&&!Ui.Confirm(this,"Ngừng hợp tác? Lịch sử phiếu nhập vẫn được giữ."))throw new BusinessException("Chưa xác nhận.");
            var input=new Supplier(s?.Id??0,name.Text,phone.Text,address.Text,email.Text,active.SelectedIndex==0);await Task.Run(()=>Services.Warehouse.SaveSupplier(Session,input));
        });if(dialog.ShowDialog(this)==DialogResult.OK)await RefreshRows();
    });
    internal void Preview(){grid.DataSource=new[]{new Row(new(1,"An Phú","02812345678","TP.HCM","contact@example.test"))};Count(1);}
}
internal sealed class MaterialPage:BakeryPage
{
    private readonly DataGridView grid=Design.Grid(("Id","Mã"),("Name","Nguyên liệu"),("Unit","Đơn vị"),("LowStock","Ngưỡng sắp hết"),("Active","Trạng thái"));
    internal MaterialPage(AppServices services,UserSession session):base(services,session,"Nguyên liệu","Danh sách mặt hàng dùng khi lập phiếu nhập và xuất nguyên liệu.")
    {
        Body.Controls.Add(grid);Primary("+ Thêm nguyên liệu",async(_,_)=>await Edit(false));Actions.Controls.Add(Design.Button("Quay lại kho",(_,_)=>Design.Open(this,new InventoryPage(Services,Session)),secondary:true));Actions.Controls.Add(Design.Button("Sửa",async(_,_)=>await Edit(true)));
        grid.CellFormatting+=(_,e)=>{
            if(e.ColumnIndex<0)return;
            if(grid.Columns[e.ColumnIndex].Name=="Active"&&e.Value is bool active){e.Value=active?"Đang dùng":"Ngừng dùng";e.FormattingApplied=true;e.CellStyle!.ForeColor=active?Color.SeaGreen:Design.Muted;}
            if(grid.Columns[e.ColumnIndex].Name=="Id"&&e.Value is int id){e.Value="NL"+id.ToString("000");e.FormattingApplied=true;}
        };
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
    }
    private Task Reload()=>Run(RefreshRows);
    private async Task RefreshRows(){var rows=await Task.Run(()=>Services.Warehouse.Materials(Session));grid.DataSource=rows.ToList();Count(rows.Count);}
    private Task Edit(bool existing)=>Run(async()=>{
        var m=existing&&grid.SelectedRows.Count>0?grid.SelectedRows[0].DataBoundItem as Material:null;if(existing&&m is null)throw new BusinessException("Chọn nguyên liệu trước.");
        var name=Ui.Input(150);name.Text=m?.Name;var unit=Ui.Choice("Kg","Gram","Lít","Cái","Hộp");unit.SelectedItem=m?.Unit??"Kg";var low=Design.Number(1000000000,3);low.Value=m?.LowStock??5;var active=Ui.Choice("Đang dùng","Ngừng dùng");active.SelectedIndex=m?.Active is false?1:0;
        using var dialog=new EditDialog("Nguyên liệu",[("Tên *","Name",name),("Đơn vị *","Unit",unit),("Ngưỡng sắp hết","Threshold",low),("Trạng thái","Active",active)],async()=>{var input=new Material(m?.Id??0,name.Text,unit.Text,active.SelectedIndex==0,low.Value);await Task.Run(()=>Services.Warehouse.SaveMaterial(Session,input));});if(dialog.ShowDialog(this)==DialogResult.OK)await RefreshRows();
    });
    internal void Preview(){grid.DataSource=new[]{new Material(1,"Bột mì số 13","Kg",true,10),new Material(2,"Bơ lạt","Kg")};Count(2);}
}
