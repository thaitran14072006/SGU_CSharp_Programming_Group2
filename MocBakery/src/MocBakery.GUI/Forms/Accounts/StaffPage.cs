using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Accounts;

internal sealed class StaffPage : BakeryPage, IUnsavedPage
{
    private readonly TextBox keyword=Design.Search("Mã / tên / điện thoại"),name=Ui.Input(100),phone=Ui.Input(10);
    private readonly ComboBox role=Ui.Choice("Quản lý","Thu ngân","Nhân viên kho","Nhân viên (cũ)"),status=Ui.Choice("Đang làm","Nghỉ việc");
    private readonly DateTimePicker start=Design.Date(DateTime.Today);
    private readonly DataGridView grid=Design.Grid(("MaHienThi","Mã NV"),("HoTen","Họ tên"),("SoDienThoai","Điện thoại"),("ChucVu","Chức vụ"),("DangLam","Trạng thái"));
    private static readonly string[] RoleValues=[Roles.ChuTiem,Roles.ThuNgan,Roles.Kho,Roles.NhanVien];
    private int id;private string baseline="";private bool binding;
    internal StaffPage(AppServices services,UserSession session):base(services,session,"Nhân viên","Quản lý hồ sơ nhân viên và tài khoản liên quan.")
    {
        Tool("Tìm nhân viên",keyword,340);Primary("Tìm kiếm",async(_,_)=>{if(CanLeave())await Reload();});
        name.Name="StaffName";phone.Name="StaffPhone";role.Name="StaffRole";status.Name="StaffStatus";grid.Name="StaffGrid";
        var split=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};split.ColumnStyles.Add(new(SizeType.Absolute,300));split.ColumnStyles.Add(new(SizeType.Percent,100));Body.Controls.Add(split);
        var editor=Ui.Flow(true);split.Controls.Add(editor,0,0);split.Controls.Add(grid,1,0);
        foreach(var (label,input) in new (string,Control)[]{("Họ tên *",name),("Điện thoại *",phone),("Chức vụ *",role),("Ngày vào làm *",start),("Trạng thái",status)})
        {input.Width=240;input.Margin=new Padding(0,0,0,8);Ui.Field(editor,label,input);input.AccessibleName=label;}
        grid.Columns["HoTen"]!.FillWeight=170;grid.Columns["MaHienThi"]!.FillWeight=70;grid.Columns["SoDienThoai"]!.FillWeight=130;
        Actions.Controls.Add(Design.Button("Lưu",async(_,_)=>await Save(),true));Actions.Controls.Add(Design.Button("Thêm mới",(_,_)=>{if(CanLeave())New();}));
        grid.SelectionChanged+=(_,_)=>SelectEmployee();
        grid.CellFormatting+=(_,e)=>{if(e.ColumnIndex<0)return;string key=grid.Columns[e.ColumnIndex].Name;if(key=="ChucVu")e.Value=Roles.Display(e.Value?.ToString()??"");if(key=="DangLam")e.Value=e.Value is true?"Đang làm":"Nghỉ việc";};
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;if(CanLeave())await Reload();}};
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};New();
    }
    private string State()=>$"{name.Text}|{phone.Text}|{role.SelectedIndex}|{start.Value.Date:O}|{status.SelectedIndex}";
    public bool CanLeave()=>State()==baseline||Ui.Confirm(this,"Bỏ thay đổi hồ sơ nhân viên chưa lưu?");
    private void New(){binding=true;id=0;grid.ClearSelection();name.Clear();phone.Clear();role.SelectedIndex=1;status.SelectedIndex=0;start.Value=DateTime.Today;Errors.Clear();binding=false;baseline=State();}
    private void SelectEmployee()
    {
        if(binding||grid.SelectedRows.Count==0||grid.SelectedRows[0].DataBoundItem is not NhanVien a)return;
        if(a.MaNhanVien==id)return;
        if(!CanLeave()){binding=true;grid.ClearSelection();foreach(DataGridViewRow r in grid.Rows)if(r.DataBoundItem is NhanVien n&&n.MaNhanVien==id)r.Selected=true;binding=false;return;}
        id=a.MaNhanVien;name.Text=a.HoTen;phone.Text=a.SoDienThoai;role.SelectedIndex=Array.IndexOf(RoleValues,a.ChucVu);start.Value=a.NgayVaoLam.ToDateTime(TimeOnly.MinValue);status.SelectedIndex=a.DangLam?0:1;baseline=State();
    }
    private Task Reload()=>Run(async()=>{string key=keyword.Text;var rows=await Task.Run(()=>Services.Staff.Search(Session,key));binding=true;grid.DataSource=rows.ToList();Count(rows.Count);binding=false;New();});
    private Task Save()=>Run(async()=>
    {
        if(id>0&&!Ui.Confirm(this,status.SelectedIndex==1?"Lưu nghỉ việc và khóa tài khoản liên quan?":"Lưu hồ sơ và đồng bộ họ tên/chức vụ tài khoản liên quan?"))return;
        var input=new NhanVien(id,name.Text,phone.Text,RoleValues[Math.Max(0,role.SelectedIndex)],DateOnly.FromDateTime(start.Value),status.SelectedIndex==0);
        await Task.Run(()=>Services.Staff.Save(Session,input));baseline=State();await Reload();
    },new(){["FullName"]=name,["Phone"]=phone,["Role"]=role,["StartDate"]=start,["Active"]=status});
    internal void Preview(){binding=true;grid.DataSource=new List<NhanVien>{new(1,"Trần Trung Thái","0901112233",Roles.ChuTiem,new(2026,10,1),true,1),new(2,"Lê Thảo Vy","0902223344",Roles.ThuNgan,new(2026,10,1),true,2)};Count(2);binding=false;New();}
}
