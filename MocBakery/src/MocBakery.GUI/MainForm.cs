using MocBakery.GUI.Forms.Accounts;
using MocBakery.GUI.Forms.Customers;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI;
internal sealed class MainForm : Form
{
    private readonly AppServices services;private readonly UserSession session;
    private readonly Panel body=new(){Dock=DockStyle.Fill,BackColor=Ui.Cream};
    internal bool SignedOut{get;private set;}
    internal MainForm(AppServices services,UserSession session)
    {
        this.services=services;this.session=session;Ui.Style(this,"Hệ thống quản lý tiệm bánh");
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Margin=Padding.Empty};
        root.ColumnStyles.Add(new(SizeType.Absolute,200));root.ColumnStyles.Add(new(SizeType.Percent,100));
        root.RowStyles.Add(new(SizeType.Absolute,65));root.RowStyles.Add(new(SizeType.Percent,100));Controls.Add(root);
        var header=new Label{Text=$"MỘC BAKERY  |  {session.HoTen} • {(session.VaiTro==Roles.ChuTiem?"Chủ tiệm":"Nhân viên")}",
            Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(20,0,0,0),Font=new Font("Segoe UI",14,FontStyle.Bold),BackColor=Color.White};
        root.Controls.Add(header,0,0);root.SetColumnSpan(header,2);
        var nav=Ui.Flow(true);nav.BackColor=Ui.Brown;nav.Padding=new Padding(12,15,8,10);root.Controls.Add(nav,0,1);root.Controls.Add(body,1,1);
        AddNav(nav,"Tổng quan",()=>Placeholder("Tổng quan"));
        AddNav(nav,"Bán hàng",()=>Placeholder("Bán hàng"));
        AddNav(nav,"Hóa đơn",()=>Placeholder("Hóa đơn"));
        AddNav(nav,"Đặt bánh",()=>Placeholder("Đặt bánh"));
        AddNav(nav,"Khách hàng",()=>ShowPage(new CustomerPage(services,session)));
        if(session.VaiTro==Roles.ChuTiem)AddNav(nav,"Tài khoản",()=>ShowPage(new AccountPage(services,session)));
        AddNav(nav,"Đổi mật khẩu",()=>{using var dialog=new ChangePasswordForm(services,session);dialog.ShowDialog(this);});
        AddNav(nav,"Đăng xuất",()=>{if(Ui.Confirm(this,"Đăng xuất khỏi phiên làm việc?")){services.Auth.Logout(session);SignedOut=true;Close();}});
        Placeholder(session.VaiTro==Roles.ChuTiem?"Tổng quan":"Bán hàng");
    }
    private static void AddNav(FlowLayoutPanel nav,string title,Action action)
    {
        var button=Ui.Button(title,(_,_)=>action(),true);button.Width=165;button.TextAlign=ContentAlignment.MiddleLeft;nav.Controls.Add(button);
    }
    internal void ShowPage(Control page)
    {
        while(body.Controls.Count>0){var old=body.Controls[0];body.Controls.Remove(old);old.Dispose();}
        page.Dock=DockStyle.Fill;body.Controls.Add(page);
    }
    private void Placeholder(string title)
    {
        var panel=Ui.Flow(true);panel.Controls.Add(Ui.Title(title));
        panel.Controls.Add(new Label{Text="Module này chưa được tích hợp.\nPhần hiện có: Khách hàng, Tài khoản và Đổi mật khẩu.",AutoSize=true});
        // Replace this routing with the corresponding member's page during integration.
        ShowPage(panel);
    }
}
