using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;
namespace MocBakery.GUI.Forms.Accounts;
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
