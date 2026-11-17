using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Customers;
internal sealed class CustomerPage : BakeryPage, IUnsavedPage
{
    private readonly AppServices services;private readonly UserSession session;private readonly bool picker;
    private readonly TextBox keyword=Ui.Input(100),name=Ui.Input(100),phone=Ui.Input(15),address=Ui.Input(255),note=Ui.Input(255);
    private readonly DataGridView grid=Design.Grid(("MaKhachHang","Mã"),("HoTen","Họ tên"),("SoDienThoai","Số điện thoại"),("DiaChi","Địa chỉ"),("GhiChu","Ghi chú"));
    private readonly ErrorProvider errors;private int id;private string baseline="";private bool binding;
    // Orders/Sales can open CustomerPickerForm and receive the selected entity.
    internal event Action<KhachHang>? CustomerSelected;
    internal CustomerPage(AppServices services,UserSession session,bool picker=false):base(services,session,"Khách hàng","Lưu thông tin khách hàng và tra cứu lịch sử mua bánh.")
    {
        this.services=services;this.session=session;this.picker=picker;Dock=DockStyle.Fill;BackColor=Ui.Cream;
        errors=new(){ContainerControl=this};
        name.Name="CustomerName";phone.Name="CustomerPhone";address.Name="CustomerAddress";grid.Name="CustomerGrid";
        Tool("Tìm tên / số điện thoại",keyword,340);Primary("Tìm kiếm",async(_,_)=>{if(CanLeave())await Reload();});
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};layout.ColumnStyles.Add(new(SizeType.Absolute,300));layout.ColumnStyles.Add(new(SizeType.Percent,100));Body.Controls.Add(layout);
        var editor=Ui.Flow(true);layout.Controls.Add(editor,0,0);
        foreach(var input in new[]{name,phone,address,note}){input.Width=240;input.Margin=new Padding(0,0,0,8);}
        grid.Columns["MaKhachHang"]!.FillWeight=50;grid.Columns["HoTen"]!.FillWeight=140;grid.Columns["SoDienThoai"]!.FillWeight=120;
        Ui.Field(editor,"Họ tên *",name);Ui.Field(editor,"Số điện thoại *",phone);Ui.Field(editor,"Địa chỉ",address);Ui.Field(editor,"Ghi chú",note);
        Actions.Width=680;Actions.Controls.Add(Design.Button("Thêm mới",(_,_)=>{if(CanLeave())New();}));
        Actions.Controls.Add(Design.Button("Lưu",async(_,_)=>await Save(),true));
        Actions.Controls.Add(Design.Button("Lịch sử mua",async(_,_)=>await History()));
        if(picker) Actions.Controls.Add(Design.Button("Chọn khách",(_,_)=>Choose()));
        layout.Controls.Add(grid,1,0);
        grid.SelectionChanged+=(_,_)=>{if(!binding&&grid.SelectedRows.Count>0&&grid.SelectedRows[0].DataBoundItem is KhachHang a&&a.MaKhachHang!=id){if(!CanLeave()){binding=true;grid.ClearSelection();foreach(DataGridViewRow r in grid.Rows)if(r.DataBoundItem is KhachHang c&&c.MaKhachHang==id)r.Selected=true;binding=false;return;}id=a.MaKhachHang;name.Text=a.HoTen;phone.Text=a.SoDienThoai;address.Text=a.DiaChi;note.Text=a.GhiChu;baseline=State();}};
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;if(CanLeave())await Reload();}};baseline=State();
    }
    private Dictionary<string,Control> Fields()=>new(){["FullName"]=name,["Phone"]=phone,["Address"]=address,["Note"]=note};
    private string State()=>$"{name.Text}|{phone.Text}|{address.Text}|{note.Text}";
    public bool CanLeave()=>State()==baseline||Ui.Confirm(this,"Bỏ thông tin khách hàng chưa lưu?");
    private void New(){binding=true;id=0;grid.ClearSelection();name.Clear();phone.Clear();address.Clear();note.Clear();errors.Clear();name.Focus();binding=false;baseline=State();}
    private async Task Reload()=>await Ui.Run(this,errors,Fields(),async()=>{
        string key=keyword.Text;var rows=await Task.Run(()=>services.Customers.Search(session,key));
        binding=true;grid.DataSource=rows.ToList();Count(rows.Count);binding=false;New();
    });
    private async Task Save()=>await Ui.Run(this,errors,Fields(),async()=>{
        if(id>0&&!Ui.Confirm(this,"Lưu thay đổi thông tin khách hàng?"))return;
        var input=new KhachHang(id,name.Text,phone.Text,address.Text,note.Text);
        try{await Task.Run(()=>services.Customers.Save(session,input));}
        catch(Models.BusinessException ex) when(ex.Field=="Phone"&&ex.Message.Contains("đã tồn tại"))
        {
            // UC10: show existing matching customer so the user can reuse the record.
            var existing=await Task.Run(()=>services.Customers.Search(session,input.SoDienThoai.Trim()));
            ShowDuplicateCustomers(existing);throw;
        }
        keyword.Clear();var rows=await Task.Run(()=>services.Customers.Search(session,""));
        binding=true;grid.DataSource=rows.ToList();Count(rows.Count);binding=false;New();
    });
    internal void ShowDuplicateCustomers(IReadOnlyList<KhachHang> existing)
    {
        binding=true;
        try
        {
            grid.DataSource=existing.ToList();
            grid.ClearSelection();
            Count(existing.Count);
        }
        finally{binding=false;}
    }
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
    internal void Preview(){binding=true;grid.DataSource=new List<KhachHang>{new(1,"Ngọc Anh","0901234567","TP. Hồ Chí Minh",null),new(2,"Minh Khang","0907654321",null,null)};Count(2);binding=false;New();}
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
