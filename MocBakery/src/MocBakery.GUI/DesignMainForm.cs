using MocBakery.GUI.Forms.Accounts;
using MocBakery.GUI.Forms.Customers;
using MocBakery.GUI.Forms.Catalog;
using MocBakery.GUI.Forms.Inventory;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;
namespace MocBakery.GUI;
internal sealed class MainForm:Form
{
    private readonly AppServices services;private readonly UserSession session;
    private readonly Panel body=new(){Dock=DockStyle.Fill,BackColor=Design.Background};
    private readonly List<Button> navigation=[];
    internal bool SignedOut{get;private set;}
    internal MainForm(AppServices services,UserSession session)
    {
        this.services=services;this.session=session;Ui.Style(this,"Mộc Bakery");BackColor=Design.Background;
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Margin=Padding.Empty,Padding=Padding.Empty};
        root.ColumnStyles.Add(new(SizeType.Absolute,205));root.ColumnStyles.Add(new(SizeType.Percent,100));
        root.RowStyles.Add(new(SizeType.Absolute,34));root.RowStyles.Add(new(SizeType.Percent,100));Controls.Add(root);
        var bar=new Label{Text="BMS Desktop",Dock=DockStyle.Fill,BackColor=Design.Brown,ForeColor=Color.White,Font=new Font("Segoe UI",9,FontStyle.Bold),Padding=new Padding(20,7,0,0)};
        root.Controls.Add(bar,0,0);root.SetColumnSpan(bar,2);
        var sidebar=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,BackColor=Color.White,Margin=Padding.Empty};
        sidebar.RowStyles.Add(new(SizeType.Absolute,88));sidebar.RowStyles.Add(new(SizeType.Percent,100));sidebar.RowStyles.Add(new(SizeType.Absolute,65));root.Controls.Add(sidebar,0,1);
        var brand=new Panel{Dock=DockStyle.Fill};brand.Controls.Add(new Label{Text="MỘC",AutoSize=true,Location=new Point(20,15),Font=new Font("Segoe UI",28,FontStyle.Bold),ForeColor=Design.Brown});
        brand.Controls.Add(new Label{Text="BAKERY MANAGEMENT",AutoSize=true,Location=new Point(23,64),Font=new Font("Segoe UI",7,FontStyle.Bold),ForeColor=Design.Muted});sidebar.Controls.Add(brand,0,0);
        var nav=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(12,0,4,0)};sidebar.Controls.Add(nav,0,1);
        Add(nav,"Tổng quan",()=>Placeholder("Tổng quan"));Add(nav,"Bán hàng",()=>Placeholder("Bán hàng"));
        Add(nav,"Quản lý bánh",()=>ShowPage(new ProductPage(services,session)));Add(nav,"Danh mục bánh",()=>ShowPage(new CategoryPage(services,session)));
        Add(nav,"Hóa đơn",()=>Placeholder("Hóa đơn"));Add(nav,"Đặt bánh theo yêu cầu",()=>Placeholder("Đặt bánh theo yêu cầu"));
        Add(nav,"Khách hàng",()=>ShowPage(new CustomerPage(services,session)));
        Add(nav,"Tồn kho và hạn dùng",()=>ShowPage(new InventoryPage(services,session)));
        if(Roles.CanManageInventory(session.VaiTro))
        {
            Add(nav,"Phiếu nhập kho",()=>ShowPage(new ReceiptListPage(services,session)));
            Add(nav,"Nhà cung cấp",()=>ShowPage(new SupplierPage(services,session)));
        }
        if(session.VaiTro==Roles.ChuTiem)Add(nav,"Nhân viên",()=>Placeholder("Nhân viên"));
        Add(nav,"Báo cáo thống kê",()=>Placeholder("Báo cáo thống kê"));
        var profile=new Button{Dock=DockStyle.Fill,Text=session.HoTen+"\n"+Roles.Display(session.VaiTro)+"  ▾",TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(20,0,0,0),FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Design.Brown,Font=new Font("Segoe UI",9)};
        profile.FlatAppearance.BorderSize=0;sidebar.Controls.Add(profile,0,2);var menu=new ContextMenuStrip();
        if(session.VaiTro==Roles.ChuTiem)menu.Items.Add("Tài khoản và phân quyền",null,(_,_)=>ShowPage(new AccountPage(services,session)));
        menu.Items.Add("Đổi mật khẩu",null,(_,_)=>{using var dialog=new ChangePasswordForm(services,session);dialog.ShowDialog(this);});
        menu.Items.Add("Đăng xuất",null,(_,_)=>{if(CanLeavePage()&&Ui.Confirm(this,"Đăng xuất khỏi phiên làm việc?")){services.Auth.Logout(session);SignedOut=true;Close();}});
        profile.Click+=(_,_)=>menu.Show(profile,new Point(0,-menu.PreferredSize.Height));profile.Disposed+=(_,_)=>menu.Dispose();
        root.Controls.Add(body,1,1);FormClosing+=(_,e)=>{if(!SignedOut&&!CanLeavePage())e.Cancel=true;};
        ShowPage(new ProductPage(services,session));
    }
    private void Add(FlowLayoutPanel nav,string title,Action action)
    {
        var button=new Button{Text="▪  "+title,Tag=title,Size=new Size(177,33),Margin=new Padding(0,0,0,3),FlatStyle=FlatStyle.Flat,
            BackColor=Color.White,ForeColor=Design.Muted,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",8.5f),Cursor=Cursors.Hand};
        button.FlatAppearance.BorderSize=0;button.Click+=(_,_)=>action();navigation.Add(button);nav.Controls.Add(button);
    }
    private bool CanLeavePage()=>body.Controls.Count==0||body.Controls[0] is not IUnsavedPage page||page.CanLeave();
    internal void ShowPage(Control page)
    {
        if(!CanLeavePage()){page.Dispose();return;}
        while(body.Controls.Count>0){var old=body.Controls[0];body.Controls.Remove(old);old.Dispose();}page.Dock=DockStyle.Fill;body.Controls.Add(page);
        string title=page switch{ProductPage or ProductDetailPage=>"Quản lý bánh",CategoryPage=>"Danh mục bánh",InventoryPage or MaterialPage=>"Tồn kho và hạn dùng",ReceiptPage or ReceiptListPage=>"Phiếu nhập kho",SupplierPage=>"Nhà cung cấp",CustomerPage=>"Khách hàng",_=>""};
        foreach(var button in navigation){bool selected=button.Tag?.ToString()==title;button.BackColor=selected?Color.FromArgb(245,232,218):Color.White;button.ForeColor=selected?Design.Brown:Design.Muted;}
    }
    private void Placeholder(string title)
    {
        var page=new Panel{Dock=DockStyle.Fill,Padding=new Padding(30),BackColor=Design.Background};
        page.Controls.Add(new Label{Dock=DockStyle.Fill,Text=title+"\n\nChức năng này chưa được tích hợp từ thành viên phụ trách.",Font=new Font("Segoe UI",14),ForeColor=Design.Brown});ShowPage(page);
    }
}
