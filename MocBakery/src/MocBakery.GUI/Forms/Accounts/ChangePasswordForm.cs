using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Accounts;
internal sealed class ChangePasswordForm : Form
{
    internal ChangePasswordForm(AppServices services,UserSession session)
    {
        Ui.Style(this,"Đổi mật khẩu"); MinimumSize = new Size(540,500); ClientSize = new Size(540,500);
        var panel = Ui.Flow(true); Controls.Add(panel); panel.Controls.Add(Ui.Title("Đổi mật khẩu"));
        var old = Ui.Input(64,true); var pass = Ui.Input(64,true); var confirm = Ui.Input(64,true);
        old.Name="CurrentPassword";pass.Name="NewPassword";confirm.Name="ConfirmPassword";
        Ui.Field(panel,"Mật khẩu hiện tại *",old); Ui.Field(panel,"Mật khẩu mới * (8–64 ký tự)",pass); Ui.Field(panel,"Xác nhận mật khẩu *",confirm);
        var show=new CheckBox {Name="ShowPasswords",Text="Hiện mật khẩu",AccessibleName="Hiện hoặc ẩn cả ba mật khẩu",AutoSize=true,Margin=new Padding(0,5,0,10)};
        show.CheckedChanged+=(_,_)=>
        {
            foreach(var input in new[]{old,pass,confirm})input.UseSystemPasswordChar=!show.Checked;
        };
        panel.Controls.Add(show);
        var errors=new ErrorProvider { ContainerControl=this }; Disposed += (_,_) => errors.Dispose();
        var save=Ui.Button("Lưu thay đổi",async (_,_) => await Ui.Run(this,errors,new() {["OldPassword"]=old,["Password"]=pass,["Confirmation"]=confirm},async () =>
        {
            string o=old.Text,p=pass.Text,c=confirm.Text;
            await Task.Run(() => services.Auth.ChangePassword(session,o,p,c));
            old.Clear();pass.Clear();confirm.Clear();
            MessageBox.Show(this,"Đổi mật khẩu thành công. Vui lòng đăng nhập lại."); DialogResult=DialogResult.OK; Close();
        }),true);
        var cancel=Ui.Button("Hủy",(_,_) => Close()); panel.Controls.Add(save); panel.Controls.Add(cancel); AcceptButton=save;CancelButton=cancel;
    }
}
