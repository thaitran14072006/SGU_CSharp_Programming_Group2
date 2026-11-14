using MocBakery.GUI.Forms.Catalog;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.GUI.Forms.Inventory;
internal sealed class InventoryPage : UserControl
{
    private readonly AppServices services;
    private readonly UserSession session;
    private readonly TextBox keyword=Ui.Input(150);
    private readonly ComboBox product=ChoiceItem.Combo([new(0,"Tất cả sản phẩm")]);
    private readonly ComboBox filter=Ui.Choice("Tất cả lô","Có thể bán","Đã hết hạn","Ngừng bán");
    private readonly DataGridView stock=Ui.Grid(("MaSanPham","Mã"),("TenSanPham","Tên bánh"),("DonViTinh","Đơn vị"),
        ("DangKinhDoanh","Kinh doanh"),("TongTon","Tồn vật lý"),("TonCoTheBan","Có thể bán"),("TonHetHan","Hết hạn"),("TonNgungBan","Ngừng bán"));
    private readonly DataGridView lots=Ui.Grid(("MaLo","Mã lô"),("TenSanPham","Tên bánh"),("NgaySanXuat","Ngày SX"),
        ("HanSuDung","Hạn dùng"),("SoLuongNhap","SL nhập"),("SoLuongCon","SL còn"),("TrangThaiHienThi","Trạng thái"),("NguonNhap","Nguồn nhập"));
    private readonly TabControl tabs=new(){Dock=DockStyle.Fill};
    private readonly ErrorProvider errors;
    private readonly Label count=new(){AutoSize=true};
    internal InventoryPage(AppServices services,UserSession session)
    {
        this.services=services;this.session=session;Dock=DockStyle.Fill;BackColor=Ui.Cream;errors=new(){ContainerControl=this};
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(20)};
        layout.RowStyles.Add(new(SizeType.Absolute,60));layout.RowStyles.Add(new(SizeType.Absolute,155));
        layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,30));layout.RowStyles.Add(new(SizeType.Absolute,45));Controls.Add(layout);
        layout.Controls.Add(Ui.Title("Kho bánh và hạn dùng"),0,0);
        var tools=Ui.Flow();layout.Controls.Add(tools,0,1);keyword.PlaceholderText="Tìm tên bánh trong tổng tồn";
        product.Width=220;filter.Width=190;tools.Controls.Add(keyword);tools.Controls.Add(product);tools.Controls.Add(filter);tools.SetFlowBreak(filter,true);
        tools.Controls.Add(Ui.Button("Tìm / Tải lại",async(_,_)=>await Reload()));
        tools.Controls.Add(Ui.Button("Chi tiết lô",async(_,_)=>await Details()));
        if(session.VaiTro==Roles.ChuTiem)
        {
            tools.Controls.Add(Ui.Button("Nhập lô",async(_,_)=>await Receive(),true));
            tools.Controls.Add(Ui.Button("Ngừng bán lô",async(_,_)=>await Status(LotStatus.Stopped)));
            tools.Controls.Add(Ui.Button("Cho bán lại",async(_,_)=>await Status(LotStatus.Selling)));
        }
        var totals=new TabPage("Tổng tồn"){UseVisualStyleBackColor=true};totals.Controls.Add(stock);tabs.TabPages.Add(totals);
        var batches=new TabPage("Chi tiết lô • FEFO"){UseVisualStyleBackColor=true};batches.Controls.Add(lots);tabs.TabPages.Add(batches);
        foreach(string col in new[]{"NgaySanXuat","HanSuDung"})lots.Columns[col]!.DefaultCellStyle.Format="dd/MM/yyyy";
        lots.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        foreach(var (key,weight) in new[]{("MaSanPham",40),("TenSanPham",200),("DonViTinh",65),("DangKinhDoanh",100),("TongTon",90),("TonCoTheBan",90),("TonHetHan",85),("TonNgungBan",90)})
            stock.Columns[key]!.FillWeight=weight;
        foreach(var (key,weight) in new[]{("MaLo",45),("TenSanPham",180),("NgaySanXuat",100),("HanSuDung",100),("SoLuongNhap",70),("SoLuongCon",70),("TrangThaiHienThi",175),("NguonNhap",100)})
            lots.Columns[key]!.FillWeight=weight;
        stock.CellFormatting+=(_,e)=>{if(stock.Columns[e.ColumnIndex].Name=="DangKinhDoanh"&&e.Value is bool value){e.Value=value?"Đang bán":"Ngừng bán";e.FormattingApplied=true;}};
        stock.CellDoubleClick+=async(_,e)=>{if(e.RowIndex<0)return;if(stock.Rows[e.RowIndex].DataBoundItem is StockRow s){
            for(int i=0;i<product.Items.Count;i++)if(product.Items[i] is ChoiceItem item&&item.Id==s.MaSanPham)product.SelectedIndex=i;
            tabs.SelectedIndex=1;await Reload();}};
        layout.Controls.Add(tabs,0,2);layout.Controls.Add(count,0,3);
        layout.Controls.Add(new Label{Text="Hạn dùng có hiệu lực hết ngày ghi trên lô. Tồn hết hạn và ngừng bán có thể trùng nhau.\nNhấp đúp sản phẩm để xem lô. Tải lại để cập nhật sau bán hàng hoặc sang ngày mới.",Dock=DockStyle.Fill,AutoEllipsis=true},0,4);
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Reload();}};
    }
    private LotRow SelectedLot()=>tabs.SelectedIndex==1&&lots.SelectedRows.Count>0&&lots.SelectedRows[0].DataBoundItem is LotRow row
        ? row : throw new Models.BusinessException("Mở tab Chi tiết lô và chọn một lô trước.");
    private Task Run(Func<Task> action)=>Ui.Run(this,errors,[],action);
    private async Task RefreshRows()
    {
        int selected=ChoiceItem.Selected(product)??0;
        var products=await Task.Run(()=>services.Catalog.Products(session));product.Items.Clear();product.Items.Add(new ChoiceItem(0,"Tất cả sản phẩm"));
        foreach(var p in products)product.Items.Add(new ChoiceItem(p.MaSanPham,p.TenSanPham));product.SelectedIndex=0;
        for(int i=0;i<product.Items.Count;i++)if(product.Items[i] is ChoiceItem item&&item.Id==selected)product.SelectedIndex=i;
        int? id=ChoiceItem.Selected(product) is >0?ChoiceItem.Selected(product):null;
        string state=filter.SelectedIndex switch{1=>"Sellable",2=>"Expired",3=>"Stopped",_=>"All"};string key=keyword.Text;
        var totals=await Task.Run(()=>services.Inventory.Stock(session,key));
        stock.DataSource=totals.Where(s=>id is null||s.MaSanPham==id).ToList();
        var rows=await Task.Run(()=>services.Inventory.Lots(session,id,state));lots.DataSource=rows.ToList();
        count.Text=$"{stock.Rows.Count} sản phẩm • {rows.Count} lô theo bộ lọc • Cập nhật {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
    }
    private Task Reload()=>Run(RefreshRows);
    private Task Receive()=>Run(async()=>{
        var products=await Task.Run(()=>services.Catalog.Products(session,active:true));
        if(products.Count==0)throw new Models.BusinessException("Cần có sản phẩm đang kinh doanh trước khi nhập lô.");
        using var dialog=Editor(products,input=>Task.Run(()=>services.Inventory.Receive(session,input)));
        if(dialog.ShowDialog(this)==DialogResult.OK){tabs.SelectedIndex=1;filter.SelectedIndex=0;product.SelectedIndex=0;await RefreshRows();}
    });
    internal static EditDialog Editor(IReadOnlyList<SanPham> products,Func<LotInput,Task> save)
    {
        var choice=ChoiceItem.Combo(products.Select(p=>new ChoiceItem(p.MaSanPham,p.TenSanPham)));
        var source=Ui.Input(100);
        var produced=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Width=310,Value=DateTime.Today};
        var expiry=new DateTimePicker{Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",Width=310,Value=DateTime.Today};
        var qty=new NumericUpDown{Width=310,Minimum=1,Maximum=int.MaxValue,ThousandsSeparator=true,Value=1};
        return new EditDialog("Nhập lô bánh",[("Sản phẩm *","Product",choice),("Nguồn nhập","Source",source),
            ("Ngày sản xuất *","Produced",produced),("Hạn sử dụng *","Expiry",expiry),("Số lượng nhập *","Quantity",qty)],async()=>{
                var input=new LotInput(ChoiceItem.Selected(choice)??0,source.Text,DateOnly.FromDateTime(produced.Value),DateOnly.FromDateTime(expiry.Value),(int)qty.Value);
                await save(input);
            });
    }
    internal void ShowLotsPreview()=>tabs.SelectedIndex=1;
    private Task Status(string status)=>Run(async()=>{
        var row=SelectedLot();
        if(!Ui.Confirm(this,$"{(status==LotStatus.Stopped?"Ngừng bán":"Cho phép bán lại")} lô #{row.MaLo}?\nLô hết hạn hoặc sản phẩm ngừng kinh doanh vẫn không được bán."))return;
        await Task.Run(()=>services.Inventory.SetStatus(session,row.MaLo,status));await RefreshRows();
    });
    private Task Details()=>Run(()=>{
        var row=SelectedLot();
        MessageBox.Show(this,$"Lô #{row.MaLo} • {row.TenSanPham}\nNhập lúc: {row.NgayNhap:dd/MM/yyyy HH:mm}\nNguồn: {row.NguonNhap}\nNgày SX: {row.NgaySanXuat:dd/MM/yyyy}\nHạn dùng: {row.HanSuDung:dd/MM/yyyy}\nSố lượng nhập: {row.SoLuongNhap:N0}\nCòn lại: {row.SoLuongCon:N0}\n{row.TrangThaiHienThi}","Chi tiết lô bánh");
        return Task.CompletedTask;
    });
    internal void Preview()
    {
        stock.DataSource=new[]{new StockRow(1,"Bánh mì bơ tỏi","Cái",true,14,9,3,2)};
        lots.DataSource=new[]{new LotRow(1,1,"Bánh mì bơ tỏi",DateTime.Today,"Bếp Mộc",DateOnly.FromDateTime(DateTime.Today),DateOnly.FromDateTime(DateTime.Today),4,4,LotStatus.Selling,"Có thể bán",true)};
    }
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}
