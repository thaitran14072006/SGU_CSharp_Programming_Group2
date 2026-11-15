using System.Drawing.Printing;
using System.Globalization;
using System.Text;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Reports;
namespace MocBakery.GUI.Forms.Reports;
internal sealed class ReportsPage:UserControl
{
 internal ReportsPage(AppServices services,UserSession session)
 {
  var errors=new ErrorProvider{ContainerControl=this};Disposed+=(_,_)=>errors.Dispose();
  var start=new DateTimePicker{Width=150,Format=DateTimePickerFormat.Short,Value=DateTime.Today.AddDays(-6)};
  var end=new DateTimePicker{Width=150,Format=DateTimePickerFormat.Short,Value=DateTime.Today};
  var grouping=Ui.Choice("Ngày","Tháng");grouping.Width=110;
  var summary=new Label{AutoSize=true,Font=new Font("Segoe UI",12,FontStyle.Bold)};
  var chart=new RevenueChart{Dock=DockStyle.Fill,BackColor=Color.White};
  var days=Ui.Grid(("Day","Ngày"),("InvoiceCount","Số hóa đơn"),("Revenue","Doanh thu (đ)"));
  var items=Ui.Grid(("ProductId","Mã"),("Name","Bánh bán chạy"),("Quantity","Số lượng"),("Revenue","Doanh thu (đ)"));
  var panel=Ui.Flow();Ui.Field(panel,"Từ ngày *",start);Ui.Field(panel,"Đến ngày *",end);
  Ui.Field(panel,"Nhóm theo",grouping);
  panel.Controls.Add(Ui.Button("Xem báo cáo",async(_,_)=>await LoadData(),true));
  panel.Controls.Add(Ui.Button("Xuất Excel (CSV)",(_,_)=>ExportCsv()));
  panel.Controls.Add(Ui.Button("In báo cáo",(_,_)=>PrintReport()));
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=4,ColumnCount=2,Padding=new Padding(12)};
  layout.ColumnStyles.Add(new(SizeType.Percent,50));layout.ColumnStyles.Add(new(SizeType.Percent,50));
  layout.RowStyles.Add(new(SizeType.Absolute,100));layout.RowStyles.Add(new(SizeType.Absolute,50));
  layout.RowStyles.Add(new(SizeType.Percent,46));layout.RowStyles.Add(new(SizeType.Percent,54));
  layout.Controls.Add(panel,0,0);layout.SetColumnSpan(panel,2);
  layout.Controls.Add(summary,0,1);layout.SetColumnSpan(summary,2);
  layout.Controls.Add(chart,0,2);layout.SetColumnSpan(chart,2);
  layout.Controls.Add(days,0,3);layout.Controls.Add(items,1,3);Controls.Add(layout);
  IReadOnlyList<RevenueDay> revenue=[];IReadOnlyList<ProductSales> best=[];Dashboard? overview=null;
  Load+=async(_,_)=>{if(!UiSmoke.Active)await LoadData();};
  grouping.SelectedIndexChanged+=(_,_)=>{if(revenue.Count>0)RefreshSeries();};
  async Task LoadData()=>await Ui.Run(this,errors,new(),async()=>
  {
   DateTime from=start.Value.Date,to=end.Value.Date;
   if(from>to)throw new BusinessException("Từ ngày phải nhỏ hơn hoặc bằng Đến ngày.");
   var result=await Task.Run(()=>(services.Reports.Overview(session,from,to),services.Reports.Revenue(session,from,to),services.Reports.Bestsellers(session,from,to)));
   if(IsDisposed)return;
   overview=result.Item1;revenue=result.Item2;best=result.Item3;
   items.DataSource=best.ToList();summary.Text=$"Doanh thu: {overview.Revenue:N0} đ     |     Hóa đơn: {overview.Invoices}     |     Đơn chờ: {overview.PendingOrders}";
   RefreshSeries();
  });
  void RefreshSeries()
  {
   var series=grouping.SelectedIndex==0
    ? revenue.Select(x=>(Label:x.Day.ToString("dd/MM"),x.InvoiceCount,x.Revenue)).ToList()
    : revenue.GroupBy(x=>new DateTime(x.Day.Year,x.Day.Month,1))
       .OrderBy(x=>x.Key).Select(x=>(Label:x.Key.ToString("MM/yyyy"),InvoiceCount:x.Sum(z=>z.InvoiceCount),Revenue:x.Sum(z=>z.Revenue))).ToList();
   days.DataSource=series.Select(x=>new ReportRow(x.Label,x.InvoiceCount,x.Revenue)).ToList();
   chart.Points=series.Select(x=>(x.Label,x.Revenue)).ToList();chart.Invalidate();
  }
  void ExportCsv()
  {
   if(overview is null){MessageBox.Show(this,"Hãy xem báo cáo trước.");return;}
   using var save=new SaveFileDialog{Filter="Tệp CSV mở bằng Excel (*.csv)|*.csv",FileName="MocBakery_DoanhThu.csv"};
   if(save.ShowDialog(this)!=DialogResult.OK)return;
   var sb=new StringBuilder("sep=;\r\n");
   sb.AppendLine("Ngày/Nhóm;Số hóa đơn;Doanh thu (đ)");
   foreach(var x in chart.Points)
   {
    var count=((IEnumerable<ReportRow>)days.DataSource!).First(z=>z.Day==x.Label).InvoiceCount;
    sb.AppendLine($"{Esc(x.Label)};{count};{x.Revenue.ToString(CultureInfo.InvariantCulture)}");
   }
   sb.AppendLine("Bánh bán chạy;Số lượng;Doanh thu (đ)");
   foreach(var x in best)sb.AppendLine($"{Esc(x.Name)};{x.Quantity};{x.Revenue.ToString(CultureInfo.InvariantCulture)}");
   File.WriteAllText(save.FileName,sb.ToString(),new UTF8Encoding(true));
   MessageBox.Show(this,"Đã xuất CSV. Mở bằng Excel để xem dữ liệu.");
  }
  static string Esc(string s)=>"\""+s.Replace("\"","\"\"")+"\"";
  void PrintReport()
  {
   if(overview is null){MessageBox.Show(this,"Hãy xem báo cáo trước.");return;}
   using var doc=new PrintDocument();
   doc.PrintPage+=(_,e)=>{if(e.Graphics is null)return;
    using var title=new Font("Segoe UI",15,FontStyle.Bold);using var body=new Font("Segoe UI",10);
    float y=60; e.Graphics.DrawString("MỘC BAKERY - BÁO CÁO DOANH THU",title,Brushes.Black,60,y);y+=45;
    e.Graphics.DrawString($"Từ {start.Value:dd/MM/yyyy} đến {end.Value:dd/MM/yyyy}",body,Brushes.Black,60,y);y+=30;
    e.Graphics.DrawString(summary.Text,body,Brushes.Black,60,y);y+=40;
    foreach(var x in revenue.Take(18)){e.Graphics.DrawString($"{x.Day:dd/MM/yyyy}    {x.InvoiceCount} hóa đơn    {x.Revenue:N0} đ",body,Brushes.Black,60,y);y+=23;}
    e.HasMorePages=false;
   };
   using var dialog=new PrintDialog{Document=doc};if(dialog.ShowDialog(this)==DialogResult.OK)doc.Print();
  }
 }
 private sealed record ReportRow(string Day,int InvoiceCount,decimal Revenue);
 private sealed class RevenueChart:Control
 {
  public List<(string Label,decimal Revenue)> Points{get;set;}=[];
  public RevenueChart(){DoubleBuffered=true;}
  protected override void OnPaint(PaintEventArgs e)
  {
   base.OnPaint(e);var g=e.Graphics;g.Clear(Color.White);
   using var font=new Font("Segoe UI",9);using var title=new Font("Segoe UI",11,FontStyle.Bold);
   g.DrawString("BIỂU ĐỒ DOANH THU",title,Brushes.SaddleBrown,12,8);
   if(Points.Count==0){g.DrawString("Chưa có dữ liệu",font,Brushes.Gray,20,52);return;}
   var area=new Rectangle(58,45,Math.Max(1,Width-80),Math.Max(1,Height-85));
   using var axis=new Pen(Color.LightGray);g.DrawLine(axis,area.Left,area.Bottom,area.Right,area.Bottom);
   decimal peak=Math.Max(1,Points.Max(x=>x.Revenue));int bars=Points.Count;
   int gap=Math.Max(2,area.Width/(bars*5));float bw=Math.Max(2,(float)(area.Width-gap*(bars+1))/bars);
   for(int i=0;i<bars;i++){
    var x=Points[i];float height=(float)(x.Revenue/peak)*area.Height;
    float left=area.Left+gap+i*(bw+gap);float top=area.Bottom-height;
    using var brush=new SolidBrush(Color.FromArgb(160,107,65));g.FillRectangle(brush,left,top,bw,height);
    if(bars<=12)g.DrawString(x.Label,font,Brushes.DimGray,left,area.Bottom+5);
   }
   g.DrawString($"{peak:N0} đ",font,Brushes.Gray,4,area.Top);
  }
 }
}
