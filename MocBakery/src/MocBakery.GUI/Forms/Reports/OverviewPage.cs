using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Reports;
internal sealed class OverviewPage:UserControl
{
 internal OverviewPage(AppServices services,UserSession session,Action openSales,Action<int> openOrder)
 {
  var errors=new ErrorProvider{ContainerControl=this};Disposed+=(_,_)=>errors.Dispose();
  var day=new DateTimePicker{Format=DateTimePickerFormat.Short,Width=160,Value=DateTime.Today};
  var revenue=new Label{AutoSize=true,Font=new Font("Segoe UI",16,FontStyle.Bold)};
  var count=new Label{AutoSize=true,Font=new Font("Segoe UI",13,FontStyle.Bold)};
  var low=Ui.Grid(("ProductId","Mã"),("Name","Bánh sắp hết"),("Quantity","Số lượng"));
  var pending=Ui.Grid(("Id","Mã đơn"),("Customer","Khách hàng"),("Pickup","Ngày nhận"),("Status","Trạng thái"));
  var bar=Ui.Flow();bar.Controls.Add(Ui.Title("TỔNG QUAN"));Ui.Field(bar,"Ngày thống kê *",day);
  bar.Controls.Add(Ui.Button("Mở bán hàng",(_,_)=>openSales(),true));
  var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,AutoScroll=true};
  var a=new Panel{Width=360,Height=80,BackColor=Color.White,Padding=new Padding(12)};a.Controls.Add(revenue);
  var b=new Panel{Width=300,Height=80,BackColor=Color.White,Padding=new Padding(12)};b.Controls.Add(count);
  panel.Controls.Add(a);panel.Controls.Add(b);
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=3,Padding=new Padding(16)};
  layout.ColumnStyles.Add(new(SizeType.Percent,50));layout.ColumnStyles.Add(new(SizeType.Percent,50));
  layout.RowStyles.Add(new(SizeType.Absolute,90));layout.RowStyles.Add(new(SizeType.Absolute,100));layout.RowStyles.Add(new(SizeType.Percent,100));
  layout.Controls.Add(bar,0,0);layout.SetColumnSpan(bar,2);layout.Controls.Add(panel,0,1);layout.SetColumnSpan(panel,2);
  var first=new GroupBox{Dock=DockStyle.Fill,Text="Bánh sắp hết (dưới 10 chiếc)",Padding=new Padding(10)};
  first.Controls.Add(low);var second=new GroupBox{Dock=DockStyle.Fill,Text="Đơn đặt sắp giao",Padding=new Padding(10)};
  second.Controls.Add(pending);layout.Controls.Add(first,0,2);layout.Controls.Add(second,1,2);Controls.Add(layout);
  day.ValueChanged+=async(_,_)=>await Reload();
  pending.CellDoubleClick+=(_,e)=>{if(e.RowIndex>=0&&pending.Rows[e.RowIndex].DataBoundItem is MocBakery.Models.DTOs.Reports.UpcomingOrder o)openOrder(o.Id);};
  Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
  async Task Reload()=>await Ui.Run(this,errors,new(),async()=>
  {
   var selected=day.Value.Date;
   var result=await Task.Run(()=>(services.Reports.Overview(session,selected,selected),services.Reports.LowStock(session),services.Reports.Upcoming(session,selected)));
   if(IsDisposed)return;
   revenue.Text=$"Doanh thu trong ngày\n{result.Item1.Revenue:N0} đ";
   count.Text=$"Hóa đơn đã lập\n{result.Item1.Invoices}";
   low.DataSource=result.Item2.ToList();pending.DataSource=result.Item3.ToList();
  });
 }
}
