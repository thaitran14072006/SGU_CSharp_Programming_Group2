using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;
namespace MocBakery.GUI.Forms.Accounts;

internal sealed class AccountPage : BakeryPage, IUnsavedPage
{
    private static readonly string[] RoleValues=[Roles.ChuTiem,Roles.ThuNgan,Roles.Kho,Roles.NhanVien];
    private readonly TextBox keyword=Design.Search("Tìm tên đăng nhập / họ tên"),username=Ui.Input(50),name=Ui.Input(100),password=Ui.Input(64,true);
    private readonly ComboBox employee=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=250},role=Ui.Choice("Quản lý","Thu ngân","Nhân viên kho","Nhân viên (cũ)"),active=Ui.Choice("Hoạt động","Khóa");
    private readonly DataGridView grid=Design.Grid(("TenDangNhap","Tên đăng nhập"),("HoTen","Họ tên"),("VaiTro","Vai trò"),("TrangThai","Trạng thái"));
    private readonly DataGridView permissions=Design.Grid(("Function","Chức năng"),("Allowed","Quyền hiệu lực"));
    private readonly Button reset;
    private List<AccountSummary> accounts=[];private List<NhanVien> staff=[];
    private int id;private bool binding;private string baseline="";
    internal AccountPage(AppServices services,UserSession session):base(services,session,"Tài khoản và phân quyền","Mỗi tài khoản liên kết một hồ sơ nhân viên đang làm.")
    {
        Tool("Tra cứu tài khoản",keyword,340);Primary("Tìm kiếm",(_,_)=>{if(CanLeave())Filter();});
        employee.Name="AccountEmployee";username.Name="AccountUsername";password.Name="AccountPassword";role.Name="AccountRole";active.Name="AccountActive";grid.Name="AccountGrid";permissions.Name="PermissionGrid";
        var split=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};split.ColumnStyles.Add(new(SizeType.Absolute,300));split.ColumnStyles.Add(new(SizeType.Percent,100));Body.Controls.Add(split);
        var editor=Ui.Flow(true);split.Controls.Add(editor,0,0);name.ReadOnly=true;
        foreach(var (label,input) in new (string,Control)[]{("Nhân viên *",employee),("Tên đăng nhập *",username),("Họ tên",name),("Vai trò *",role),("Trạng thái",active),("Mật khẩu tạm * (8–64 ký tự)",password)})
        {input.Width=240;input.Margin=new Padding(0,0,0,8);input.AccessibleName=label;Ui.Field(editor,label,input);}
        var lists=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};lists.RowStyles.Add(new(SizeType.Percent,40));lists.RowStyles.Add(new(SizeType.Percent,60));split.Controls.Add(lists,1,0);lists.Controls.Add(grid,0,0);lists.Controls.Add(permissions,0,1);permissions.RowTemplate.Height=26;
        Actions.Width=650;var save=Design.Button("Lưu tài khoản",async(_,_)=>await Save(),true);save.Width=160;Actions.Controls.Add(save);
        reset=Design.Button("Đặt lại mật khẩu",(_,_)=>Reset());reset.Width=160;Actions.Controls.Add(reset);
        Actions.Controls.Add(Design.Button("Thêm mới",(_,_)=>{if(CanLeave())New();}));
        grid.SelectionChanged+=(_,_)=>SelectAccount();role.SelectedIndexChanged+=(_,_)=>PermissionRows();
        employee.SelectedIndexChanged+=(_,_)=>{if(binding||id>0)return;if(employee.SelectedItem is EmployeeChoice choice){name.Text=choice.Value.HoTen;role.SelectedIndex=Array.IndexOf(RoleValues,choice.Value.ChucVu);}};
        grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex<0)return;string key=grid.Columns[e.ColumnIndex].Name;if(key=="VaiTro")e.Value=Roles.Display(e.Value?.ToString()??"");if(key=="TrangThai")e.Value=e.Value is true?"Hoạt động":"Khóa";};
        keyword.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;if(CanLeave())Filter();}};
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};New();
    }
    private sealed record EmployeeChoice(NhanVien Value){public override string ToString()=>$"{Value.MaHienThi} - {Value.HoTen}";}
    private string State()=>$"{employee.SelectedIndex}|{username.Text}|{name.Text}|{role.SelectedIndex}|{active.SelectedIndex}|{password.Text}";
    public bool CanLeave()=>State()==baseline||Ui.Confirm(this,"Bỏ thay đổi tài khoản chưa lưu?");
    private void Choices(int? selected=null)
    {
        employee.Items.Clear();foreach(var n in staff.Where(n=>n.MaNhanVien==selected||n.DangLam&&n.MaTaiKhoan is null))employee.Items.Add(new EmployeeChoice(n));
        employee.SelectedIndex=-1;for(int i=0;i<employee.Items.Count;i++)if(employee.Items[i] is EmployeeChoice choice&&choice.Value.MaNhanVien==selected)employee.SelectedIndex=i;
    }
    private void New(){binding=true;id=0;grid.ClearSelection();Choices();employee.Enabled=true;username.Clear();username.ReadOnly=false;name.Clear();password.Clear();password.Enabled=true;active.SelectedIndex=0;active.Enabled=false;role.SelectedIndex=1;reset.Enabled=false;Errors.Clear();binding=false;PermissionRows();baseline=State();}
    private void SelectAccount()
    {
        if(binding||grid.SelectedRows.Count==0||grid.SelectedRows[0].DataBoundItem is not AccountSummary a||a.MaTaiKhoan==id)return;
        if(!CanLeave()){binding=true;grid.ClearSelection();foreach(DataGridViewRow r in grid.Rows)if(r.DataBoundItem is AccountSummary current&&current.MaTaiKhoan==id)r.Selected=true;binding=false;return;}
        binding=true;id=a.MaTaiKhoan;Choices(staff.SingleOrDefault(n=>n.MaTaiKhoan==id)?.MaNhanVien);employee.Enabled=false;
        username.Text=a.TenDangNhap;username.ReadOnly=true;name.Text=a.HoTen;role.SelectedIndex=Array.IndexOf(RoleValues,a.VaiTro);active.SelectedIndex=a.TrangThai?0:1;active.Enabled=true;password.Clear();password.Enabled=false;reset.Enabled=id!=Session.MaTaiKhoan;binding=false;PermissionRows();baseline=State();
    }
    private void PermissionRows()
    {
        string r=RoleValues[Math.Max(0,role.SelectedIndex)];bool owner=r==Roles.ChuTiem;
        permissions.DataSource=new[]{("Bán hàng / Đơn đặt / Khách hàng",Roles.CanSell(r)),("Thêm / sửa danh mục và bánh",owner),("Nhập / Xuất / Kiểm kê kho",Roles.CanManageInventory(r)),("Nhân viên / Tài khoản",owner),("Báo cáo thống kê",owner)}.Select(x=>new{Function=x.Item1,Allowed=x.Item2?"Cho phép":"Không cho phép"}).ToList();
    }
    private void Filter(){binding=true;string key=keyword.Text.Trim();var rows=accounts.Where(a=>a.TenDangNhap.Contains(key,StringComparison.OrdinalIgnoreCase)||a.HoTen.Contains(key,StringComparison.OrdinalIgnoreCase)).ToList();grid.DataSource=rows;Count(rows.Count);binding=false;New();}
    private Task Reload()=>Run(async()=>{accounts=(await Task.Run(()=>Services.Accounts.List(Session))).ToList();staff=(await Task.Run(()=>Services.Staff.Search(Session))).ToList();Filter();});
    private Task Save()=>Run(async()=>
    {
        if(employee.SelectedItem is not EmployeeChoice choice)throw new BusinessException("Chọn nhân viên đang làm. Tạo hồ sơ ở màn hình Nhân viên trước.","Employee");
        if(id>0&&!Ui.Confirm(this,"Lưu thay đổi vai trò / trạng thái tài khoản?"))return;
        int current=id,employeeId=choice.Value.MaNhanVien;var input=new AccountInput(username.Text,name.Text,RoleValues[Math.Max(0,role.SelectedIndex)],active.SelectedIndex==0,password.Text);
        await Task.Run(()=>Services.Accounts.Save(Session,current,input,employeeId));password.Clear();baseline=State();await Reload();
    },new(){["Employee"]=employee,["Username"]=username,["FullName"]=name,["Role"]=role,["Active"]=active,["Password"]=password});
    private void Reset(){if(id<=0)return;using var dialog=new PasswordResetForm(Services,Session,id,username.Text);dialog.ShowDialog(this);}
    internal void Preview(){staff=[new(1,"Trần Trung Thái","0901112233",Roles.ChuTiem,new(2026,10,1),true,1),new(2,"Lê Thảo Vy","0902223344",Roles.ThuNgan,new(2026,10,1),true,2)];accounts=[new(1,"admin","Trần Trung Thái",Roles.ChuTiem,true),new(2,"thaovy","Lê Thảo Vy",Roles.ThuNgan,true)];Filter();if(grid.Rows.Count>0)grid.Rows[0].Selected=true;}
}
