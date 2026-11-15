using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Catalog;
internal sealed class CategoryPage:BakeryPage
{
    private readonly TextBox keyword=Design.Search("Nhập từ khóa để tìm kiếm...");
    private readonly ComboBox filter=Ui.Choice("Tất cả","Đang dùng","Ngừng dùng");
    private readonly DataGridView grid=Design.Grid(("Code","Mã"),("Name","Danh mục"),("Count","Số loại bánh"),("Status","Trạng thái"));
    private sealed record Row(LoaiBanh Source,int Count)
    {public string Code=>"DM"+Source.MaLoai.ToString("000");public string Name=>Source.TenLoai;public string Status=>Source.DangSuDung?"Đang dùng":"Ngừng dùng";}
    private Row? Selected=>grid.SelectedRows.Count>0?grid.SelectedRows[0].DataBoundItem as Row:null;
    internal CategoryPage(AppServices services,UserSession session):base(services,session,"Danh mục bánh","Phân nhóm bánh để tìm kiếm và bán hàng nhanh.")
    {
        Tool("Tìm danh mục",keyword,340);Tool("Bộ lọc",filter,250);
        if(session.VaiTro==Roles.ChuTiem)Primary("+ Thêm mới",async(_,_)=>await Edit(false));
        Body.Controls.Add(grid);var details=Design.Button("Chi tiết",(_,_)=>MessageBox.Show(this,Selected?.Source.MoTa??"Chọn danh mục trước.","Chi tiết danh mục"),secondary:true);Actions.Controls.Add(details);
        if(session.VaiTro==Roles.ChuTiem){Actions.Controls.Add(Design.Button("Sửa",async(_,_)=>await Edit(true)));var toggle=Design.Button("Ngừng / Dùng lại",async(_,_)=>await Toggle());toggle.Width=165;Actions.Controls.Add(toggle);}
        grid.Columns["Code"]!.FillWeight=65;grid.Columns["Name"]!.FillWeight=200;
        filter.SelectedIndexChanged+=async(_,_)=>{if(IsHandleCreated)await Reload();};
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Reload();}};
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
        grid.SelectionChanged+=(_,_)=>{foreach(var b in Actions.Controls.OfType<Button>())b.Enabled=Selected is not null;};
    }
    private Task Reload()=>Run(async()=>{
        string key=keyword.Text;int state=filter.SelectedIndex;
        var categories=await Task.Run(()=>Services.Catalog.Categories(Session,key));var products=await Task.Run(()=>Services.Catalog.Products(Session));
        grid.DataSource=categories.Where(c=>state==0||c.DangSuDung==(state==1)).Select(c=>new Row(c,products.Count(p=>p.MaLoai==c.MaLoai))).ToList();Count(grid.Rows.Count);
    });
    private Task Edit(bool existing)=>Run(async()=>{
        var selected=existing?Selected?.Source:null;if(existing&&selected is null)throw new BusinessException("Chọn danh mục trước.");
        using var dialog=Editor(selected,input=>Task.Run(()=>Services.Catalog.SaveCategory(Session,input)));
        if(dialog.ShowDialog(this)==DialogResult.OK)await RefreshAfterEdit();
    });
    private async Task RefreshAfterEdit()
    {
        string key=keyword.Text;var categories=await Task.Run(()=>Services.Catalog.Categories(Session,key));var products=await Task.Run(()=>Services.Catalog.Products(Session));
        grid.DataSource=categories.Where(c=>filter.SelectedIndex==0||c.DangSuDung==(filter.SelectedIndex==1)).Select(c=>new Row(c,products.Count(p=>p.MaLoai==c.MaLoai))).ToList();Count(grid.Rows.Count);
    }
    private Task Toggle()=>Run(async()=>{
        var selected=Selected?.Source??throw new BusinessException("Chọn danh mục trước.");
        if(!Ui.Confirm(this,(selected.DangSuDung?"Ngừng sử dụng":"Dùng lại")+" danh mục này? Các bánh và lịch sử liên quan vẫn được giữ."))return;
        await Task.Run(()=>Services.Catalog.SaveCategory(Session,selected with{DangSuDung=!selected.DangSuDung}));await RefreshAfterEdit();
    });
    internal static EditDialog Editor(LoaiBanh? selected,Func<LoaiBanh,Task> save)
    {
        var name=Ui.Input(100);name.Text=selected?.TenLoai;var note=Ui.Input(255);note.Text=selected?.MoTa;
        return new(selected is null?"Thêm danh mục":"Sửa danh mục",[("Tên danh mục *","Name",name),("Mô tả","Note",note)],()=>save(new(selected?.MaLoai??0,name.Text,note.Text,selected?.DangSuDung??true)));
    }
    internal void Preview(){grid.DataSource=new[]{new Row(new(1,"Bánh mì",null),12),new Row(new(2,"Bánh lạnh",null),8),new Row(new(3,"Bánh kem",null),10),new Row(new(4,"Bánh ngọt",null,false),6)};Count(4);}
}
