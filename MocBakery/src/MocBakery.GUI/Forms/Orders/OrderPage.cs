using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Orders;
using MocBakery.Models.DTOs.Sales;
using MocBakery.GUI.Forms.Customers;
namespace MocBakery.GUI.Forms.Orders;
internal sealed class OrderPage:UserControl
{
 internal OrderPage(AppServices services,UserSession session,int focusOrderId=0)
 {
  var errors=new ErrorProvider{ContainerControl=this};Disposed+=(_,_)=>errors.Dispose();
  var orders=Ui.Grid(("Id","Mã đơn"),("Customer","Khách"),("Pickup","Ngày nhận"),("Total","Tổng tiền"),("Deposit","Cọc"),("Due","Còn thu"),("Status","Trạng thái"),("Receiver","Tiếp nhận"));
  orders.CellFormatting+=(_,e)=>{if(orders.Columns[e.ColumnIndex].Name=="Status"&&e.Value is string value)e.Value=DisplayStatus(value);};
  orders.ColumnHeadersDefaultCellStyle.WrapMode=DataGridViewTriState.True;
  orders.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize;
  foreach(DataGridViewColumn column in orders.Columns)column.MinimumWidth=52;
  orders.Columns["Pickup"]!.FillWeight=125;
  orders.Columns["Status"]!.FillWeight=115;
  var products=Ui.Grid(("Name","Sản phẩm"),("Price","Đơn giá"),("Stock","Tồn"));
  var lines=Ui.Grid(("Product","Sản phẩm"),("Quantity","SL"),("Price","Đơn giá"),("Total","Thành tiền"));
  foreach(var grid in new[]{orders,products,lines})
  {
   grid.ColumnHeadersDefaultCellStyle.SelectionBackColor=grid.ColumnHeadersDefaultCellStyle.BackColor;
   grid.ColumnHeadersDefaultCellStyle.SelectionForeColor=grid.ColumnHeadersDefaultCellStyle.ForeColor;
  }
  var chosen=new Dictionary<int,OrderLine>();int? customerId=null;string customerName="Chưa chọn khách";int editId=0;
  var phone=Ui.Input(15);phone.ReadOnly=true;phone.PlaceholderText="Chọn khách hàng để lấy SĐT";
  var totalLabel=new Label{AutoSize=true,Font=new Font("Segoe UI",11,FontStyle.Bold)};
  var dueLabel=new Label{AutoSize=true,Font=new Font("Segoe UI",11,FontStyle.Bold)};
  var statusLabel=new Label{AutoSize=true,MaximumSize=new Size(900,0),Text="Trạng thái: Mới tiếp nhận"};
  var cakeText=Ui.Input(100);cakeText.PlaceholderText="Ví dụ: Chúc mừng sinh nhật";
  var top=Ui.Flow();var customer=new Label{Text=customerName,AutoSize=true};var picker=Ui.Button("Chọn khách",(_,_)=>{using var f=new CustomerPickerForm(services,session);if(f.ShowDialog(this)==DialogResult.OK&&f.SelectedCustomer is {} c){customerId=c.MaKhachHang;customerName=c.HoTen;customer.Text=customerName;phone.Text=c.SoDienThoai;}});
  var pickup=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy HH:mm",Width=170,Value=DateTime.Now.AddDays(1)};
  var deposit=new NumericUpDown{Minimum=0,Maximum=1000000000,DecimalPlaces=2,Width=140};var note=Ui.Input(350);var quantity=new NumericUpDown{Minimum=1,Maximum=100000,Width=90};var reason=Ui.Input(255);reason.PlaceholderText="Lý do hủy";
  top.Controls.Add(Ui.Title("Đặt bánh theo yêu cầu"));
  top.SetFlowBreak(top.Controls[top.Controls.Count-1],true);
  top.Controls.Add(picker);top.Controls.Add(customer);Ui.Field(top,"Khách hàng / SĐT *",phone);
  Ui.Field(top,"Ngày giờ nhận *",pickup);Ui.Field(top,"Tiền cọc (đ) *",deposit);
  Ui.Field(top,"Nội dung trên bánh (tối đa 100 ký tự)",cakeText);
  Ui.Field(top,"Yêu cầu thêm",note);Ui.Field(top,"Số lượng",quantity);
  top.Controls.Add(totalLabel);top.Controls.Add(dueLabel);top.Controls.Add(statusLabel);
  top.Controls.Add(Ui.Button("Thêm bánh",(_,_)=>{if(products.CurrentRow?.DataBoundItem is not SalesProduct p){MessageBox.Show(this,"Chọn bánh.");return;}var old=chosen.GetValueOrDefault(p.Id);chosen[p.Id]=new(p.Id,p.Name,(old?.Quantity??0)+(int)quantity.Value,p.Price);ReloadLines();}));
  top.Controls.Add(Ui.Button("Xóa dòng",(_,_)=>{if(lines.CurrentRow?.DataBoundItem is OrderLine p)chosen.Remove(p.ProductId);ReloadLines();}));
  top.Controls.Add(Ui.Button("Tạo đơn",async(_,_)=>await Run(async()=>{int cid=customerId??throw new BusinessException("Chọn khách hàng.");await Task.Run(()=>services.Orders.Create(session,cid,pickup.Value,deposit.Value,BuildNote(),chosen.Values.ToArray()));Reset();await LoadOrders();}),true));
  top.Controls.Add(Ui.Button("Sửa đơn",async(_,_)=>await Run(async()=>{int cid=customerId??throw new BusinessException("Chọn khách hàng.");if(editId==0)throw new BusinessException("Chọn đơn trước.");await Task.Run(()=>services.Orders.Update(session,editId,cid,pickup.Value,deposit.Value,BuildNote(),chosen.Values.ToArray()));Reset();await LoadOrders();})));
  top.Controls.Add(Ui.Button("Tiếp nhận / chuẩn bị",async(_,_)=>await Run(async()=>{int id=Selected();await Task.Run(()=>services.Orders.Prepare(session,id));await LoadOrders();})));
  top.Controls.Add(Ui.Button("Giao đơn",async(_,_)=>await Run(async()=>{int id=Selected();if(!Ui.Confirm(this,"Giao đơn và trừ tồn kho?"))return;int invoice=await Task.Run(()=>services.Orders.Deliver(session,id));MessageBox.Show(this,$"Đã giao, hóa đơn #{invoice}.");await LoadOrders();})));
  top.Controls.Add(reason);top.Controls.Add(Ui.Button("Hủy đơn",async(_,_)=>await Run(async()=>{int id=Selected();if(!Ui.Confirm(this,"Hủy đơn này?"))return;await Task.Run(()=>services.Orders.Cancel(session,id,reason.Text));await LoadOrders();})));
  top.Controls.Add(Ui.Button("Làm mới",async(_,_)=>{Reset();await LoadOrders();}));
  deposit.ValueChanged+=(_,_)=>ReloadLines();
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=2,Padding=new Padding(10)};layout.ColumnStyles.Add(new(SizeType.Percent,50));layout.ColumnStyles.Add(new(SizeType.Percent,50));layout.RowStyles.Add(new(SizeType.Absolute,295));layout.RowStyles.Add(new(SizeType.Percent,50));layout.RowStyles.Add(new(SizeType.Percent,50));layout.Controls.Add(top,0,0);layout.SetColumnSpan(top,2);layout.Controls.Add(orders,0,1);layout.SetRowSpan(orders,2);layout.Controls.Add(products,1,1);layout.Controls.Add(lines,1,2);Controls.Add(layout);
  orders.SelectionChanged+=async(_,_)=>{if(orders.CurrentRow?.DataBoundItem is not OrderSummary o)return;editId=o.Id;customerId=o.CustomerId;customerName=o.Customer;customer.Text=customerName;pickup.Value=o.Pickup;deposit.Value=Math.Clamp(o.Deposit,deposit.Minimum,deposit.Maximum);note.Text=o.Note??"";cakeText.Clear();phone.Text=o.Contact?.Phone??"";statusLabel.Text="Trạng thái: "+DisplayStatus(o.Status)+(o.Contact is {} contact?$"\nNgười nhận: {contact.FullName} | {contact.Phone} | {contact.Email}":"");if(UiSmoke.Active)return;try{var ls=await Task.Run(()=>services.Orders.Lines(session,o.Id));if(IsDisposed)return;chosen.Clear();foreach(var l in ls)chosen[l.ProductId]=l;ReloadLines();}catch(Exception ex){ErrorLog.Write(ex);}};
  ReloadLines();
  Load+=async(_,_)=>{if(UiSmoke.Active)return;await Run(async()=>products.DataSource=(await Task.Run(()=>services.Sales.Products(session))).ToList());await LoadOrders();};
  int Selected()=>orders.CurrentRow?.DataBoundItem is OrderSummary o?o.Id:throw new BusinessException("Chọn đơn trước.");
  void ReloadLines(){lines.DataSource=chosen.Values.ToList();var total=chosen.Values.Sum(x=>x.Total);totalLabel.Text=$"Tổng giá: {total:N0} đ";dueLabel.Text=$"Còn lại: {total-deposit.Value:N0} đ";}
  string BuildNote()=>string.IsNullOrWhiteSpace(cakeText.Text)?note.Text:$"Nội dung bánh: {cakeText.Text.Trim()}"+(string.IsNullOrWhiteSpace(note.Text)?"":$" | {note.Text.Trim()}");
  static string DisplayStatus(string s)=>s switch{"MoiTiepNhan"=>"Mới tiếp nhận","DangChuanBi"=>"Đang chuẩn bị","DaGiao"=>"Đã giao","DaHuy"=>"Đã hủy",_=>s};
  void Reset(){editId=0;customerId=null;customer.Text="Chưa chọn khách";chosen.Clear();note.Clear();cakeText.Clear();phone.Clear();deposit.Value=0;ReloadLines();pickup.Value=DateTime.Now.AddDays(1);statusLabel.Text="Trạng thái: Mới tiếp nhận";}
  async Task Run(Func<Task> f)=>await Ui.Run(this,errors,new(),f);
  async Task LoadOrders()=>await Run(async()=>{var entries=(await Task.Run(()=>services.Orders.List(session))).ToList();orders.DataSource=entries;if(focusOrderId>0){foreach(DataGridViewRow row in orders.Rows)if(row.DataBoundItem is OrderSummary entry&&entry.Id==focusOrderId){row.Selected=true;orders.CurrentCell=row.Cells[0];break;}}});
 }
}
