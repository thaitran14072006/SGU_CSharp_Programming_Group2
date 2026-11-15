using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;
namespace MocBakery.GUI.Forms.Accounts;
internal sealed class AccountPage : UserControl
{
    private readonly AppServices services; private readonly UserSession session;
    private readonly TextBox username=Ui.Input(50), name=Ui.Input(100), password=Ui.Input(64,true);
    private static readonly string[] RoleValues=[Roles.ChuTiem,Roles.ThuNgan,Roles.Kho,Roles.NhanVien];
    private readonly ComboBox role=Ui.Choice("Quản lý","Thu ngân","Nhân viên kho"), active=Ui.Choice("Hoạt động","Khóa");
    private readonly DataGridView grid=Ui.Grid(("TenDangNhap","Tên đăng nhập"),("HoTen","Họ tên"),("VaiTro","Vai trò"),("TrangThai","Trạng thái"));
    private readonly Label count=new() { AutoSize=true };
    private readonly ErrorProvider errors; private int id;
    internal AccountPage(AppServices services,UserSession session)
    {
        this.services=services;this.session=session; Dock=DockStyle.Fill;BackColor=Ui.Cream;
        errors=new() {ContainerControl=this};
        var layout=new TableLayoutPanel {Dock=DockStyle.Fill,RowCount=4,ColumnCount=1,Padding=new Padding(20)};
        layout.RowStyles.Add(new(SizeType.Absolute,60));layout.RowStyles.Add(new(SizeType.Absolute,245));
        layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,25)); Controls.Add(layout);
        layout.Controls.Add(Ui.Title("Tài khoản và phân quyền"),0,0);
        var editor=Ui.Flow();layout.Controls.Add(editor,0,1);
        Ui.Field(editor,"Tên đăng nhập *",username);Ui.Field(editor,"Họ tên *",name);
        Ui.Field(editor,"Vai trò *",role);Ui.Field(editor,"Trạng thái *",active);Ui.Field(editor,"Mật khẩu ban đầu *",password);
        editor.Controls.Add(Ui.Button("Thêm mới",(_,_) => New()));
        editor.Controls.Add(Ui.Button("Lưu tài khoản",async (_,_) => await Save(),true));
        editor.Controls.Add(Ui.Button("Đặt lại mật khẩu",(_,_) => Reset()));
        editor.Controls.Add(Ui.Button("Tải lại",async (_,_) => await Reload()));
        layout.Controls.Add(grid,0,2);layout.Controls.Add(count,0,3);
        grid.SelectionChanged += (_,_) => SelectAccount();
        grid.CellFormatting += (_,e) =>
        {
            if(e.ColumnIndex < 0)return;
            if(grid.Columns[e.ColumnIndex].Name=="VaiTro")e.Value=Roles.Display(e.Value?.ToString()??"");
            if(grid.Columns[e.ColumnIndex].Name=="TrangThai")e.Value=e.Value is true?"Hoạt động":"Khóa";
        };
        Load += async (_,_) => { if(!UiSmoke.Active) await Reload(); }; New();
    }
    private void New() { id=0;grid.ClearSelection();username.Clear();username.ReadOnly=false;name.Clear();password.Clear();password.Enabled=true;if(role.Items.Count>3)role.Items.RemoveAt(3);role.SelectedIndex=1;active.SelectedIndex=0;active.Enabled=false;errors.Clear();username.Focus(); }
    private void SelectAccount()
    {
        if(grid.SelectedRows.Count==0 || grid.SelectedRows[0].DataBoundItem is not AccountSummary a)return;
        id=a.MaTaiKhoan;username.Text=a.TenDangNhap;username.ReadOnly=true;name.Text=a.HoTen;
        if(a.VaiTro==Roles.NhanVien&&role.Items.Count==3)role.Items.Add("Nhân viên (cũ)");role.SelectedIndex=Array.IndexOf(RoleValues,a.VaiTro);active.SelectedIndex=a.TrangThai?0:1;
        active.Enabled=true;password.Clear();password.Enabled=false;
    }
    private async Task Reload() => await Ui.Run(this,errors,Fields(),async () =>
    {
        var rows=await Task.Run(() => services.Accounts.List(session));
        grid.DataSource=rows.ToList();count.Text=rows.Count==0?"Chưa có dữ liệu":$"{rows.Count} tài khoản";New();
    });
    private Dictionary<string,Control> Fields() => new() {["Username"]=username,["FullName"]=name,["Role"]=role,["Active"]=active,["Password"]=password};
    private async Task Save() => await Ui.Run(this,errors,Fields(),async () =>
    {
        if(id>0&&!Ui.Confirm(this,"Lưu thay đổi tài khoản này?"))return;
        int current=id;var input=new AccountInput(username.Text,name.Text,RoleValues[Math.Max(0,role.SelectedIndex)],active.SelectedIndex==0,password.Text);
        await Task.Run(() => services.Accounts.Save(session,current,input));
        password.Clear();var rows=await Task.Run(() => services.Accounts.List(session));
        grid.DataSource=rows.ToList();count.Text=$"{rows.Count} tài khoản";New();MessageBox.Show(this,"Đã lưu tài khoản.");
    });
    private void Reset()
    {
        if(id==0){MessageBox.Show(this,"Chọn tài khoản cần đặt lại mật khẩu.");return;}
        using var dialog=new PasswordResetForm(services,session,id,username.Text);dialog.ShowDialog(this);
    }
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}
internal sealed class PasswordResetForm : Form
{
    internal PasswordResetForm(AppServices services,UserSession session,int id,string username)
    {
        Ui.Style(this,"Đặt lại mật khẩu");MinimumSize=new Size(540,420);ClientSize=new Size(540,420);
        var p=Ui.Flow(true);Controls.Add(p);p.Controls.Add(Ui.Title("Đặt lại mật khẩu"));
        p.Controls.Add(new Label {Text="Tài khoản: "+username,AutoSize=true});
        var pass=Ui.Input(64,true);var confirm=Ui.Input(64,true);
        Ui.Field(p,"Mật khẩu mới * (8–64 ký tự)",pass);Ui.Field(p,"Xác nhận *",confirm);
        var errors=new ErrorProvider {ContainerControl=this};Disposed+=(_,_)=>errors.Dispose();
        p.Controls.Add(Ui.Button("Xác nhận",async(_,_)=>await Ui.Run(this,errors,new(){["Password"]=pass,["Confirmation"]=confirm},async()=>{
            if(pass.Text!=confirm.Text)throw new BusinessException("Xác nhận mật khẩu không khớp.","Confirmation");
            if(!Ui.Confirm(this,"Đặt lại mật khẩu của "+username+"?"))return;
            string value=pass.Text;await Task.Run(()=>services.Accounts.ResetPassword(session,id,value));
            pass.Clear();confirm.Clear();MessageBox.Show(this,"Đã đặt lại mật khẩu.");Close();
        }),true));
    }
}
