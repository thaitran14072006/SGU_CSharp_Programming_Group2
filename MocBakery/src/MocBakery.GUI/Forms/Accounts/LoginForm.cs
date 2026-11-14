using System.Text.Json;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Accounts;
internal sealed class LoginForm : Form
{
    private readonly AppServices services;
    private readonly TextBox username = Ui.Input(50), password = Ui.Input(64,true);
    private readonly CheckBox remember = new() { Text = "Ghi nhớ tên đăng nhập", AutoSize = true };
    private readonly ErrorProvider errors;
    private readonly string rememberedFile = Path.Combine(AppContext.BaseDirectory,"username.local.json");
    internal UserSession? Session { get; private set; }
    internal LoginForm(AppServices services)
    {
        this.services = services; Ui.Style(this,"Đăng nhập"); errors = new() { ContainerControl = this };
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        split.ColumnStyles.Add(new(SizeType.Percent,43)); split.ColumnStyles.Add(new(SizeType.Percent,57)); Controls.Add(split);
        var brand = new Panel { Dock = DockStyle.Fill, BackColor = Ui.Brown, Padding = new Padding(45,140,25,30) };
        brand.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "MỘC BAKERY\n\nBánh ngon mỗi ngày\n\nHệ thống quản lý tiệm bánh", ForeColor = Ui.Cream, Font = new Font("Segoe UI",24,FontStyle.Bold) });
        split.Controls.Add(brand,0,0);
        var content = Ui.Flow(true); content.Padding = new Padding(70,75,25,25); split.Controls.Add(content,1,0);
        content.Controls.Add(Ui.Title("Chào mừng trở lại"));
        content.Controls.Add(new Label { Text = "Đăng nhập để bắt đầu ca làm việc",AutoSize = true,Margin = new Padding(0,0,0,25) });
        Ui.Field(content,"Tên đăng nhập *",username); Ui.Field(content,"Mật khẩu *",password);
        var show = new CheckBox { Text = "Hiện mật khẩu",AutoSize = true,Margin = new Padding(0,5,0,12) };
        show.CheckedChanged += (_,_) => password.UseSystemPasswordChar = !show.Checked;
        content.Controls.Add(show); content.Controls.Add(remember);
        var login = Ui.Button("Đăng nhập",async (_,_) => await SignIn(),true); content.Controls.Add(login);
        var exit = Ui.Button("Thoát",(_,_) => Close()); content.Controls.Add(exit);
        content.Controls.Add(Ui.Button("Thiết lập lần đầu",(_,_) => { using var setup = new SetupForm(services); setup.ShowDialog(this); }));
        AcceptButton = login; CancelButton = exit;
        try { if(File.Exists(rememberedFile)) { username.Text = JsonSerializer.Deserialize<string>(File.ReadAllText(rememberedFile)) ?? ""; remember.Checked = true; } }
        catch(Exception ex) when(ex is IOException or JsonException or UnauthorizedAccessException) { ErrorLog.Write(ex); }
        Shown += (_,_) => { if(username.Text.Length == 0) username.Focus(); else password.Focus(); };
    }
    private async Task SignIn() => await Ui.Run(this,errors,new() { ["Username"]=username,["Password"]=password },async () =>
    {
        string u = username.Text, p = password.Text;
        Session = await Task.Run(() => services.Auth.Login(u,p));
        try
        {
            if(remember.Checked) File.WriteAllText(rememberedFile,JsonSerializer.Serialize(u.Trim()));
            else if(File.Exists(rememberedFile)) File.Delete(rememberedFile);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { ErrorLog.Write(ex); }
        password.Clear(); DialogResult = DialogResult.OK; Close();
    });
    protected override void Dispose(bool disposing) { if(disposing) errors.Dispose(); base.Dispose(disposing); }
}
internal sealed class SetupForm : Form
{
    internal SetupForm(AppServices services)
    {
        Ui.Style(this,"Thiết lập lần đầu"); MinimumSize = new Size(540,570); ClientSize = new Size(540,570);
        var panel = Ui.Flow(true); Controls.Add(panel); panel.Controls.Add(Ui.Title("Tạo chủ tiệm"));
        panel.Controls.Add(new Label { Text="Chỉ dùng khi CSDL chưa có tài khoản.",AutoSize=true });
        var user=Ui.Input(50); var name=Ui.Input(100); var pass=Ui.Input(64,true); var confirm=Ui.Input(64,true);
        Ui.Field(panel,"Tên đăng nhập *",user); Ui.Field(panel,"Họ tên *",name);
        Ui.Field(panel,"Mật khẩu * (8–64 ký tự)",pass); Ui.Field(panel,"Xác nhận mật khẩu *",confirm);
        var errors = new ErrorProvider { ContainerControl=this }; Disposed += (_,_) => errors.Dispose();
        panel.Controls.Add(Ui.Button("Tạo tài khoản",async (_,_) => await Ui.Run(this,errors,new() {["Username"]=user,["FullName"]=name,["Password"]=pass,["Confirmation"]=confirm},async () =>
        {
            if(pass.Text != confirm.Text) throw new Models.BusinessException("Xác nhận mật khẩu không khớp.","Confirmation");
            string u=user.Text,n=name.Text,p=pass.Text;
            await Task.Run(() => services.Bootstrap(u,n,p));
            MessageBox.Show(this,"Đã tạo chủ tiệm. Bạn có thể đăng nhập."); Close();
        }),true));
    }
}
