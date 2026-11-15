using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Catalog;
internal sealed class ProductPage : UserControl
{
    private readonly AppServices services;
    private readonly UserSession session;
    private readonly TextBox keyword=Ui.Input(150);
    private readonly ComboBox category=ChoiceItem.Combo([new(0,"Tất cả loại bánh")]);
    private readonly ComboBox active=Ui.Choice("Tất cả trạng thái","Đang kinh doanh","Ngừng kinh doanh");
    private readonly DataGridView grid=Ui.Grid(("MaSanPham","Mã"),("TenSanPham","Tên bánh"),("MaLoai","Mã loại"),
        ("DonViTinh","Đơn vị"),("GiaBan","Giá bán"),("DangKinhDoanh","Kinh doanh"),("MoTa","Mô tả"));
    private readonly ErrorProvider errors;
    private readonly Label count=new(){AutoSize=true};
    private Dictionary<int,string> categoryNames=[];
    internal ProductPage(AppServices services,UserSession session)
    {
        this.services=services;this.session=session;BackColor=Ui.Cream;Dock=DockStyle.Fill;errors=new(){ContainerControl=this};
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(20)};
        layout.RowStyles.Add(new(SizeType.Absolute,60));layout.RowStyles.Add(new(SizeType.Absolute,165));
        layout.RowStyles.Add(new(SizeType.Percent,100));layout.RowStyles.Add(new(SizeType.Absolute,35));Controls.Add(layout);
        layout.Controls.Add(Ui.Title("Sản phẩm"),0,0);
        var tools=Ui.Flow();layout.Controls.Add(tools,0,1);keyword.PlaceholderText="Tìm tên sản phẩm";
        category.Width=220;active.Width=210;
        tools.Controls.Add(keyword);tools.Controls.Add(category);tools.Controls.Add(active);
        tools.SetFlowBreak(active,true);
        tools.Controls.Add(Ui.Button("Tìm / Tải lại",async(_,_)=>await Reload()));
        tools.Controls.Add(Ui.Button("Xem chi tiết",async(_,_)=>await Details()));
        if(session.VaiTro==Roles.ChuTiem)
        {
            tools.Controls.Add(Ui.Button("Thêm bánh",async(_,_)=>await Edit(false),true));
            tools.Controls.Add(Ui.Button("Sửa / Ngừng bán",async(_,_)=>await Edit(true)));
        }
        grid.Columns["GiaBan"]!.DefaultCellStyle.Format="N2";
        grid.Columns["MaLoai"]!.HeaderText="Loại bánh";
        foreach(var (key,weight) in new[]{("MaSanPham",40),("TenSanPham",190),("MaLoai",110),("DonViTinh",65),("GiaBan",100),("DangKinhDoanh",110),("MoTa",150)})
            grid.Columns[key]!.FillWeight=weight;
        grid.CellFormatting+=(_,e)=>{
            string key=grid.Columns[e.ColumnIndex].Name;
            if(key=="DangKinhDoanh"&&e.Value is bool value){e.Value=value?"Đang bán":"Ngừng bán";e.FormattingApplied=true;}
            if(key=="MaLoai"&&e.Value is int id&&categoryNames.TryGetValue(id,out string? label)){e.Value=label;e.FormattingApplied=true;}
        };
        layout.Controls.Add(grid,0,2);layout.Controls.Add(count,0,3);
        Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Reload();}};
    }
    private SanPham? Selected=>grid.SelectedRows.Count>0?grid.SelectedRows[0].DataBoundItem as SanPham:null;
    private Task Run(Func<Task> action)=>Ui.Run(this,errors,[],action);
    private async Task RefreshRows()
    {
        int categoryId=ChoiceItem.Selected(category)??0;string key=keyword.Text;
        bool? status=active.SelectedIndex switch{1=>true,2=>false,_=>null};
        var categories=await Task.Run(()=>services.Catalog.Categories(session));
        categoryNames=categories.ToDictionary(c=>c.MaLoai,c=>c.TenLoai);
        category.Items.Clear();category.Items.Add(new ChoiceItem(0,"Tất cả loại bánh"));
        foreach(var c in categories)category.Items.Add(new ChoiceItem(c.MaLoai,c.TenLoai));
        category.SelectedIndex=0;
        for(int i=0;i<category.Items.Count;i++)if(category.Items[i] is ChoiceItem item&&item.Id==categoryId)category.SelectedIndex=i;
        int? filter=ChoiceItem.Selected(category) is >0?ChoiceItem.Selected(category):null;
        var rows=await Task.Run(()=>services.Catalog.Products(session,key,filter,status));
        grid.DataSource=rows.ToList();count.Text=$"{rows.Count} sản phẩm • Giữ sản phẩm ngừng kinh doanh để bảo toàn lịch sử";
    }
    private Task Reload()=>Run(RefreshRows);
    private Task Details()=>Run(()=>{
        var p=Selected??throw new Models.BusinessException("Chọn sản phẩm trước.");
        MessageBox.Show(this,$"{p.TenSanPham}\nMã: {p.MaSanPham} • Loại: {p.MaLoai}\nĐơn vị: {p.DonViTinh}\nGiá: {p.GiaBan:N2} đ\n{(p.DangKinhDoanh?"Đang kinh doanh":"Ngừng kinh doanh")}\n\n{p.MoTa}","Chi tiết sản phẩm");
        return Task.CompletedTask;
    });
    private Task Edit(bool existing)=>Run(async()=>{
        var p=existing?Selected:null;
        if(existing&&p is null)throw new Models.BusinessException("Chọn sản phẩm để sửa.");
        var categories=await Task.Run(()=>services.Catalog.Categories(session));
        if(categories.Count==0)throw new Models.BusinessException("Thêm loại bánh trước khi tạo sản phẩm.");
        using var dialog=Editor(p,categories,input=>Task.Run(()=>services.Catalog.SaveProduct(session,input)));
        if(dialog.ShowDialog(this)==DialogResult.OK)await RefreshRows();
    });
    internal static EditDialog Editor(SanPham? p,IReadOnlyList<LoaiBanh> categories,Func<SanPham,Task> save)
    {
        var name=Ui.Input(150);name.Text=p?.TenSanPham;
        var type=ChoiceItem.Combo(categories.Select(c=>new ChoiceItem(c.MaLoai,c.TenLoai)));
        for(int i=0;i<type.Items.Count;i++)if(type.Items[i] is ChoiceItem item&&item.Id==p?.MaLoai)type.SelectedIndex=i;
        var unit=Ui.Input(20);unit.Text=p?.DonViTinh??"Cái";
        var price=new NumericUpDown{Width=310,Minimum=0,Maximum=9999999999999999.99m,DecimalPlaces=2,ThousandsSeparator=true,Value=p?.GiaBan??0};
        var selling=new CheckBox{Text="Đang kinh doanh",Checked=p?.DangKinhDoanh??true,AutoSize=true};
        var note=Ui.Input(500);note.Text=p?.MoTa;
        return new EditDialog(p is null?"Thêm sản phẩm":"Sửa sản phẩm",
            [("Tên bánh *","Name",name),("Loại bánh *","Category",type),("Đơn vị tính *","Unit",unit),
             ("Giá bán *","Price",price),("Trạng thái","Active",selling),("Mô tả","Note",note)],async()=>{
                var input=new SanPham(p?.MaSanPham??0,ChoiceItem.Selected(type)??0,name.Text,unit.Text,price.Value,selling.Checked,note.Text);
                await save(input);
            });
    }
    internal void Preview(){categoryNames=new(){[1]="Bánh mì",[2]="Bánh ngọt"};grid.DataSource=new[]{new SanPham(1,1,"Bánh mì bơ tỏi", "Cái",25000,true,"Nướng mới trong ngày"),new SanPham(2,2,"Bánh kem dâu", "Cái",150000,false,"Sản phẩm tạm ngừng kinh doanh")};}
    protected override void Dispose(bool disposing){if(disposing)errors.Dispose();base.Dispose(disposing);}
}
