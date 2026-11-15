using MocBakery.GUI.Forms.Accounts;
using MocBakery.GUI.Forms.Customers;
using MocBakery.GUI.Forms.Sales;
using MocBakery.GUI.Forms.Orders;
using MocBakery.GUI.Forms.Reports;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.GUI.Forms.Catalog;
using MocBakery.GUI.Forms.Inventory;
namespace MocBakery.GUI;
internal sealed class MainForm:Form
{
 private readonly AppServices services;private readonly UserSession session;
 private readonly Panel body=new(){Dock=DockStyle.Fill,BackColor=Ui.Cream};
 internal bool SignedOut{get;private set;}
 internal MainForm(AppServices services,UserSession session)
 {
  this.services=services;this.session=session;Ui.Style(this,"Hệ thống quản lý tiệm bánh");
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Margin=Padding.Empty};
  root.ColumnStyles.Add(new(SizeType.Absolute,200));root.ColumnStyles.Add(new(SizeType.Percent,100));root.RowStyles.Add(new(SizeType.Absolute,65));root.RowStyles.Add(new(SizeType.Percent,100));Controls.Add(root);
  var header=new Label{Text=$"MỘC BAKERY  |  {session.HoTen} • {(session.VaiTro==Roles.ChuTiem?"Chủ tiệm":"Nhân viên")}",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(20,0,0,0),Font=new Font("Segoe UI",14,FontStyle.Bold),BackColor=Color.White};root.Controls.Add(header,0,0);root.SetColumnSpan(header,2);
  var nav=Ui.Flow(true);nav.BackColor=Ui.Brown;nav.Padding=new Padding(12,15,8,10);root.Controls.Add(nav,0,1);root.Controls.Add(body,1,1);
  if(session.VaiTro==Roles.ChuTiem)AddNav(nav,"Tổng quan",()=>OpenOverview());
  AddNav(nav,"Bán hàng",()=>ShowPage(new SalesPage(services,session)));
  AddNav(nav,"Hóa đơn",()=>ShowPage(new SalesPage(services,session,showInvoices:true)));
  AddNav(nav,"Đặt bánh",()=>ShowPage(new OrderPage(services,session)));
  AddNav(nav,"Khách hàng",()=>ShowPage(new CustomerPage(services,session)));
  AddNav(nav,"Loại bánh",()=>ShowPage(new CategoryPage(services,session)));
  AddNav(nav,"Sản phẩm",()=>ShowPage(new ProductPage(services,session)));
  AddNav(nav,"Kho bánh",()=>ShowPage(new InventoryPage(services,session)));
  if(session.VaiTro==Roles.ChuTiem){AddNav(nav,"Báo cáo",()=>ShowPage(new ReportsPage(services,session)));AddNav(nav,"Tài khoản",()=>ShowPage(new AccountPage(services,session)));}
  AddNav(nav,"Đổi mật khẩu",()=>{using var dialog=new ChangePasswordForm(services,session);dialog.ShowDialog(this);});
  AddNav(nav,"Đăng xuất",()=>{if(Ui.Confirm(this,"Đăng xuất khỏi phiên làm việc?")){services.Auth.Logout(session);SignedOut=true;Close();}});
  if(session.VaiTro==Roles.ChuTiem)OpenOverview();else ShowPage(new SalesPage(services,session));
 }
 private void OpenOverview()=>ShowPage(new OverviewPage(services,session,
  ()=>ShowPage(new SalesPage(services,session)),
  id=>ShowPage(new OrderPage(services,session,id))));
 private static void AddNav(FlowLayoutPanel nav,string title,Action action){var button=Ui.Button(title,(_,_)=>action(),true);button.Width=165;button.TextAlign=ContentAlignment.MiddleLeft;nav.Controls.Add(button);}
 internal void ShowPage(Control page){while(body.Controls.Count>0){var old=body.Controls[0];body.Controls.Remove(old);old.Dispose();}page.Dock=DockStyle.Fill;body.Controls.Add(page);}
}
