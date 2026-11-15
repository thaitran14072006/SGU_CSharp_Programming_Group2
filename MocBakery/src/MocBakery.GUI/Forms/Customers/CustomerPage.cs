using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Customers;
internal sealed class CustomerPage : UserControl
{
    private readonly AppServices services;private readonly UserSession session;private readonly bool picker;
    private readonly TextBox keyword=Ui.Input(100),name=Ui.Input(100),phone=Ui.Input(15),address=Ui.Input(255),note=Ui.Input(255);
    private readonly DataGridView grid=Ui.Grid(("MaKhachHang","Mã"),("HoTen","Họ tên"),("SoDienThoai","Số điện thoại"),("DiaChi","Địa chỉ"),("GhiChu","Ghi chú"));
    private readonly ErrorProvider errors;private readonly Label count=new(){AutoSize=true};private int id;
    // Orders/Sales can open CustomerPickerForm and receive the selected entity.
    internal event Action<KhachHang>? CustomerSelected;
    internal CustomerPage(AppServices services,UserSession session,bool picker=false)
    {
        this.services=services;this.session=session;this.picker=picker;Dock=DockStyle.Fill;BackColor=Ui.Cream;
        errors=new(){ContainerControl=this};
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(20)};
        layout.RowStyles.Add(new(SizeType.Absolute,60));layout.RowStyles.Add(new(SizeType.Absolute,65));
        layout.RowStyles.Add(new(SizeType.Absolute,245));layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,25));Controls.Add(layout);
        layout.Controls.Add(Ui.Title("Khách hàng"),0,0);
        var search=Ui.Flow();layout.Controls.Add(search,0,1);keyword.PlaceholderText="Tìm theo họ tên hoặc số điện thoại";
        search.Controls.Add(keyword);search.Controls.Add(Ui.Button("Tìm kiếm",async(_,_)=>await Reload()));
        var editor=Ui.Flow();layout.Controls.Add(editor,0,2);
        Ui.Field(editor,"Họ tên *",name);Ui.Field(editor,"Số điện thoại *",phone);Ui.Field(editor,"Địa chỉ",address);Ui.Field(editor,"Ghi chú",note);
        editor.SetFlowBreak(editor.Controls[editor.Controls.Count-1],true);
        editor.Controls.Add(Ui.Button("Thêm mới",(_,_)=>New()));
        editor.Controls.Add(Ui.Button("Lưu khách hàng",async(_,_)=>await Save(),true));
        editor.Controls.Add(Ui.Button("Lịch sử mua",async(_,_)=>await History()));
        if(picker) editor.Controls.Add(Ui.Button("Chọn khách hàng",(_,_)=>Choose()));
        layout.Controls.Add(grid,0,3);layout.Controls.Add(count,0,4);
        grid.SelectionChanged+=(_,_)=>{if(grid.SelectedRows.Count>0&&grid.SelectedRows[0].DataBoundItem is KhachHang a){id=a.MaKhachHang;name.Text=a.HoTen;phone.Text=a.SoDienThoai;address.Text=a.DiaChi;note.Text=a.GhiChu;}};
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Reload();}};
    }
    private Dictionary<string,Control> Fields()=>new(){["FullName"]=name,["Phone"]=phone,["Address"]=address,["Note"]=note};
    private void New(){id=0;grid.ClearSelection();name.Clear();phone.Clear();address.Clear();note.Clear();errors.Clear();name.Focus();}
    private async Task Reload()=>await Ui.Run(this,errors,Fields(),async()=>{
        string key=keyword.Text;var rows=await Task.Run(()=>services.Customers.Search(session,key));
        grid.DataSource=rows.ToList();count.Text=rows.Count==0?"Chưa có dữ liệu":$"{rows.Count} khách hàng";New();
    });
    private async Task Save()=>await Ui.Run(this,errors,Fields(),async()=>{
        if(id>0&&!Ui.Confirm(this,"Lưu thay đổi thông tin khách hàng?"))return;
        var input=new KhachHang(id,name.Text,phone.Text,address.Text,note.Text);
        try{await Task.Run(()=>services.Customers.Save(session,input));}
        catch(Models.BusinessException ex) when(ex.Field=="Phone"&&ex.Message.Contains("đã tồn tại"))
        {
            // UC10: show existing matching customer so the user can reuse the record.
            var existing=await Task.Run(()=>services.Customers.Search(session,input.SoDienThoai.Trim()));
            grid.DataSource=existing.ToList();count.Text=$"{existing.Count} khách hàng phù hợp";throw;
        }
        keyword.Clear();var rows=await Task.Run(()=>services.Customers.Search(session,""));
        grid.DataSource=rows.ToList();count.Text=$"{rows.Count} khách hàng";New();MessageBox.Show(this,"Đã lưu khách hàng.");
    });
    private async Task History()=>await Ui.Run(this,errors,Fields(),async()=>{
        int selected=id;var rows=await Task.Run(()=>services.Customers.History(session,selected));
        using var dialog=new Form();Ui.Style(dialog,"Lịch sử mua hàng");dialog.MinimumSize=new Size(750,400);dialog.ClientSize=new Size(850,450);
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,Padding=new Padding(15)};
        layout.RowStyles.Add(new(SizeType.Absolute,45));layout.RowStyles.Add(new(SizeType.Percent,100));dialog.Controls.Add(layout);
        layout.Controls.Add(new Label{Text=rows.Count==0?"Chưa có dữ liệu":"Hóa đơn của khách #"+selected,AutoSize=true},0,0);
        var history=Ui.Grid(("MaHoaDon","Mã hóa đơn"),("NgayLap","Ngày lập"),("TongTien","Tổng tiền"),("TienCocTru","Cọc đã trừ"),("SoTienThu","Còn thu"));
        foreach(string column in new[]{"TongTien","TienCocTru","SoTienThu"})history.Columns[column]!.DefaultCellStyle.Format="N0";
        history.Columns["NgayLap"]!.DefaultCellStyle.Format="dd/MM/yyyy HH:mm";history.DataSource=rows.ToList();layout.Controls.Add(history,0,1);dialog.ShowDialog(this);
    });
    private void Choose(){if(!picker)return;if(grid.SelectedRows.Count==0||grid.SelectedRows[0].DataBoundItem is not KhachHang a){MessageBox.Show(this,"Chọn khách hàng trước.");return;}CustomerSelected?.Invoke(a);}
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}
internal sealed class CustomerPickerForm : Form
{
    internal KhachHang? SelectedCustomer{get;private set;}
    internal CustomerPickerForm(AppServices services,UserSession session)
    {
        Ui.Style(this,"Chọn khách hàng");var page=new CustomerPage(services,session,true);Controls.Add(page);
        page.CustomerSelected+=a=>{SelectedCustomer=a;DialogResult=DialogResult.OK;Close();};
    }
}
