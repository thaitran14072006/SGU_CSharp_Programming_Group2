using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Catalog;
internal sealed class CategoryPage : UserControl
{
    private readonly AppServices services;
    private readonly UserSession session;
    private readonly TextBox keyword=Ui.Input(100);
    private readonly DataGridView grid=Ui.Grid(("MaLoai","Mã loại"),("TenLoai","Tên loại bánh"),("MoTa","Mô tả"));
    private readonly Label count=new(){AutoSize=true};
    private readonly ErrorProvider errors;
    internal CategoryPage(AppServices services,UserSession session)
    {
        this.services=services;this.session=session;BackColor=Ui.Cream;Dock=DockStyle.Fill;
        errors=new(){ContainerControl=this};
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(20)};
        layout.RowStyles.Add(new(SizeType.Absolute,60));layout.RowStyles.Add(new(SizeType.Absolute,120));
        layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,30));Controls.Add(layout);
        layout.Controls.Add(Ui.Title("Loại bánh"),0,0);
        var tools=Ui.Flow();layout.Controls.Add(tools,0,1);keyword.PlaceholderText="Tìm tên loại bánh";tools.Controls.Add(keyword);
        tools.Controls.Add(Ui.Button("Tìm / Tải lại",async(_,_)=>await Reload()));
        if(session.VaiTro==Roles.ChuTiem)
        {
            tools.Controls.Add(Ui.Button("Thêm loại",async(_,_)=>await Edit(false),true));
            tools.Controls.Add(Ui.Button("Sửa loại",async(_,_)=>await Edit(true)));
            tools.Controls.Add(Ui.Button("Xóa loại",async(_,_)=>await Delete()));
        }
        layout.Controls.Add(grid,0,2);layout.Controls.Add(count,0,3);
        grid.Columns["MaLoai"]!.FillWeight=45;grid.Columns["TenLoai"]!.FillWeight=160;grid.Columns["MoTa"]!.FillWeight=240;
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Reload();}};
    }
    private LoaiBanh? Selected=>grid.SelectedRows.Count>0?grid.SelectedRows[0].DataBoundItem as LoaiBanh:null;
    private Task Run(Func<Task> action)=>Ui.Run(this,errors,[],action);
    private async Task RefreshRows()
    {
        string key=keyword.Text;var rows=await Task.Run(()=>services.Catalog.Categories(session,key));
        grid.DataSource=rows.ToList();count.Text=$"{rows.Count} loại bánh • Nhân viên chỉ tra cứu";
    }
    private Task Reload()=>Run(RefreshRows);
    private Task Edit(bool existing)=>Run(async()=>{
        var selected=existing?Selected:null;
        if(existing&&selected is null)throw new Models.BusinessException("Chọn loại bánh để sửa.");
        using var dialog=Editor(selected,input=>Task.Run(()=>services.Catalog.SaveCategory(session,input)));
        if(dialog.ShowDialog(this)==DialogResult.OK)await RefreshRows();
    });
    internal static EditDialog Editor(LoaiBanh? selected,Func<LoaiBanh,Task> save)
    {
        var name=Ui.Input(100);name.Text=selected?.TenLoai;
        var note=Ui.Input(255);note.Text=selected?.MoTa;
        return new EditDialog(selected is null?"Thêm loại bánh":"Sửa loại bánh",
            [("Tên loại *","Name",name),("Mô tả","Note",note)],()=>save(new(selected?.MaLoai??0,name.Text,note.Text)));
    }
    private Task Delete()=>Run(async()=>{
        var selected=Selected??throw new Models.BusinessException("Chọn loại bánh để xóa.");
        if(!Ui.Confirm(this,$"Xóa loại '{selected.TenLoai}'? Chỉ xóa được loại chưa có sản phẩm."))return;
        await Task.Run(()=>services.Catalog.DeleteCategory(session,selected.MaLoai));await RefreshRows();
    });
    internal void Preview()=>grid.DataSource=new[]{new LoaiBanh(1,"Bánh mì","Bánh làm trong ngày"),new LoaiBanh(2,"Bánh ngọt","Bánh kem và bánh nướng")};
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}
