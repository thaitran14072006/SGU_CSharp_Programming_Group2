using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GUI.Forms.Catalog;
internal sealed class ProductPage:BakeryPage
{
    private readonly TextBox keyword=Design.Search("Nhập từ khóa để tìm kiếm...");
    private readonly ComboBox category=ChoiceItem.Combo([new(0,"Tất cả")]);
    private readonly ComboBox status=Ui.Choice("Tất cả","Đang bán","Ngừng bán");
    private readonly DataGridView grid=Design.Grid(("Code","Mã bánh"),("Name","Tên bánh"),("Category","Danh mục"),("Price","Giá bán (đ)"),("Stock","Tồn"),("Status","Trạng thái"));
    private sealed record Row(SanPham Source,string Category,long Stock)
    {public string Code=>"B"+Source.MaSanPham.ToString("000");public string Name=>Source.TenSanPham;public decimal Price=>Source.GiaBan;public string Status=>Source.DangKinhDoanh?"Đang bán":"Ngừng bán";}
    private Row? Selected=>grid.SelectedRows.Count>0?grid.SelectedRows[0].DataBoundItem as Row:null;
    internal ProductPage(AppServices services,UserSession session):base(services,session,"Quản lý bánh","Tra cứu và quản lý danh sách sản phẩm bánh.")
    {
        Tool("Từ khóa",keyword,340);var filters=new FlowLayoutPanel{WrapContents=false};category.Width=140;status.Width=110;category.Margin=Padding.Empty;status.Margin=Padding.Empty;filters.Controls.Add(category);filters.Controls.Add(status);Tool("Bộ lọc",filters,250);
        if(session.VaiTro==Roles.ChuTiem)Primary("+ Thêm mới",(_,_)=>Open(null,false));
        Body.Controls.Add(grid);grid.Columns["Price"]!.DefaultCellStyle.Format="#,##0.##";grid.Columns["Name"]!.FillWeight=160;grid.Columns["Code"]!.FillWeight=75;grid.Columns["Code"]!.MinimumWidth=80;grid.Columns["Stock"]!.FillWeight=55;
        Actions.Controls.Add(Design.Button("Chi tiết",(_,_)=>Open(Selected?.Source,true),secondary:true));
        if(session.VaiTro==Roles.ChuTiem){Actions.Controls.Add(Design.Button("Sửa",(_,_)=>Open(Selected?.Source,false)));var toggle=Design.Button("Ngừng / Bán lại",async(_,_)=>await Toggle());toggle.Width=160;Actions.Controls.Add(toggle);}
        grid.SelectionChanged+=(_,_)=>{foreach(var b in Actions.Controls.OfType<Button>())b.Enabled=Selected is not null;};
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Reload();}};
        status.SelectedIndexChanged+=async(_,_)=>{if(IsHandleCreated)await Reload();};
        category.SelectionChangeCommitted+=async(_,_)=>await Reload();Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
    }
    private Task Reload()=>Run(async()=>{
        int selected=ChoiceItem.Selected(category)??0;string key=keyword.Text;bool? active=status.SelectedIndex switch{1=>true,2=>false,_=>null};
        var categories=await Task.Run(()=>Services.Catalog.Categories(Session));category.Items.Clear();category.Items.Add(new ChoiceItem(0,"Tất cả"));
        foreach(var c in categories)category.Items.Add(new ChoiceItem(c.MaLoai,c.TenLoai));category.SelectedIndex=0;
        for(int i=0;i<category.Items.Count;i++)if(category.Items[i] is ChoiceItem item&&item.Id==selected)category.SelectedIndex=i;
        int? type=ChoiceItem.Selected(category) is >0?ChoiceItem.Selected(category):null;
        var products=await Task.Run(()=>Services.Catalog.Products(Session,key,type,active));var stock=await Task.Run(()=>Services.Inventory.Stock(Session));
        grid.DataSource=products.Select(p=>new Row(p,categories.FirstOrDefault(c=>c.MaLoai==p.MaLoai)?.TenLoai??"",stock.FirstOrDefault(s=>s.MaSanPham==p.MaSanPham)?.TonCoTheBan??0)).ToList();Count(grid.Rows.Count);
    });
    private void Open(SanPham? product,bool details)
    {
        if(details&&product is null){MessageBox.Show(this,"Chọn bánh trước.");return;}
        Design.Open(this,new ProductDetailPage(Services,Session,product,details||Session.VaiTro!=Roles.ChuTiem));
    }
    private Task Toggle()=>Run(async()=>{
        var p=Selected?.Source??throw new BusinessException("Chọn bánh trước.");
        if(!Ui.Confirm(this,(p.DangKinhDoanh?"Ngừng bán":"Cho bán lại")+" bánh này? Lịch sử được giữ nguyên."))return;
        await Task.Run(()=>Services.Catalog.SaveProduct(Session,p with{DangKinhDoanh=!p.DangKinhDoanh}));
        string key=keyword.Text;bool? active=status.SelectedIndex switch{1=>true,2=>false,_=>null};int? type=ChoiceItem.Selected(category) is >0?ChoiceItem.Selected(category):null;
        var categories=await Task.Run(()=>Services.Catalog.Categories(Session));var products=await Task.Run(()=>Services.Catalog.Products(Session,key,type,active));var stock=await Task.Run(()=>Services.Inventory.Stock(Session));
        grid.DataSource=products.Select(x=>new Row(x,categories.First(c=>c.MaLoai==x.MaLoai).TenLoai,stock.FirstOrDefault(s=>s.MaSanPham==x.MaSanPham)?.TonCoTheBan??0)).ToList();Count(grid.Rows.Count);
    });
    internal void Preview(){grid.DataSource=new[]{new Row(new(1,1,"Croissant bơ","Cái",28000,true,null),"Bánh mì",32),new Row(new(2,2,"Tiramisu mini","Cái",45000,true,null),"Bánh lạnh",18),new Row(new(3,3,"Bánh kem dâu","Cái",320000,true,null),"Bánh kem",5),new Row(new(4,4,"Cookie chocolate","Cái",18000,true,null),"Bánh quy",48),new Row(new(5,5,"Muffin việt quất","Cái",35000,false,null),"Bánh ngọt",0)};Count(5);}
}
internal sealed class ProductDetailPage:BakeryPage,IUnsavedPage
{
    private readonly SanPham? original;private readonly bool readOnly;private bool dirty,loading=true;
    private readonly TextBox code=Ui.Input(30),name=Ui.Input(100),note=Ui.Input(500);
    private readonly ComboBox category=ChoiceItem.Combo([]),unit=Ui.Choice("Cái","Hộp","Ổ"),state=Ui.Choice("Đang bán","Ngừng bán");
    private readonly NumericUpDown price=Design.Number(9999999999999999.99m,2),shelf=Design.Number(3650),low=Design.Number(1000000000,3);
    private readonly PictureBox picture=new(){SizeMode=PictureBoxSizeMode.Zoom,Width=60,Height=48};private byte[]? image;
    internal ProductDetailPage(AppServices services,UserSession session,SanPham? product,bool readOnly=false):base(services,session,"Chi tiết bánh","Thêm mới hoặc cập nhật thông tin một loại bánh.")
    {
        original=product;this.readOnly=readOnly;LayoutRoot.RowStyles[1].Height=0;
        name.Name="ProductName";category.Name="ProductCategory";unit.Name="ProductUnit";price.Name="ProductPrice";shelf.Name="ProductShelf";state.Name="ProductState";
        var card=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=5,Padding=new Padding(24),AutoScroll=true};
        card.ColumnStyles.Add(new(SizeType.Percent,50));card.ColumnStyles.Add(new(SizeType.Percent,50));for(int i=0;i<5;i++)card.RowStyles.Add(new(SizeType.Absolute,73));Body.Controls.Add(card);
        code.ReadOnly=true;code.Text=product is null?"Tự sinh":"B"+product.MaSanPham.ToString("000");name.Text=product?.TenSanPham;price.Value=product?.GiaBan??28000;
        shelf.Minimum=1;shelf.Value=product?.HanDungNgay??2;low.Value=product?.NguongCanhBao??5;state.SelectedIndex=product?.DangKinhDoanh is false?1:0;
        unit.SelectedItem=product?.DonViTinh??"Cái";note.Text=product?.MoTa;image=product?.AnhBanh;ShowImage();
        card.Controls.Add(Design.Field("Mã bánh",code),0,0);card.Controls.Add(Design.Field("Tên bánh *",name),1,0);
        card.Controls.Add(Design.Field("Danh mục *",category),0,1);card.Controls.Add(Design.Field("Giá bán (đ) *",price),1,1);
        card.Controls.Add(Design.Field("Đơn vị tính *",unit),0,2);card.Controls.Add(Design.Field("Hạn dùng (ngày) *",shelf),1,2);
        card.Controls.Add(Design.Field("Trạng thái",state),0,3);card.Controls.Add(Design.Field("Ngưỡng cảnh báo sắp hết",low),1,3);
        var imagePanel=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=false};imagePanel.Controls.Add(picture);var choose=Design.Button("Chọn ảnh",(_,_)=>ChooseImage(),secondary:true);choose.Width=110;imagePanel.Controls.Add(choose);
        card.Controls.Add(Design.Field("Ảnh bánh (JPG/PNG, tối đa 5 MB)",imagePanel),0,4);card.Controls.Add(Design.Field("Mô tả",note),1,4);
        if(!readOnly)Actions.Controls.Add(Design.Button("Lưu bánh",async(_,_)=>await Save(),true));
        Actions.Controls.Add(Design.Button(readOnly?"Quay lại":"Hủy",(_,_)=>Back(),secondary:true));
        if(readOnly){foreach(Control child in card.Controls)child.Enabled=false;}
        foreach(var control in new Control[]{name,note,category,unit,state,price,shelf,low})
        {
            control.TextChanged+=(_,_)=>{if(!loading)dirty=true;};
            if(control is NumericUpDown number)number.ValueChanged+=(_,_)=>{if(!loading)dirty=true;};
            if(control is ComboBox box)box.SelectedIndexChanged+=(_,_)=>{if(!loading)dirty=true;};
        }
        Load+=async(_,_)=>{
            if(UiSmoke.Active){category.Items.Add(new ChoiceItem(1,"Bánh mì"));category.SelectedIndex=0;loading=false;return;}
            await Run(async()=>{var categories=await Task.Run(()=>Services.Catalog.Categories(Session));
                foreach(var c in categories.Where(c=>c.DangSuDung||c.MaLoai==product?.MaLoai))category.Items.Add(new ChoiceItem(c.MaLoai,c.TenLoai));
                if(category.Items.Count>0)category.SelectedIndex=0;for(int i=0;i<category.Items.Count;i++)if(category.Items[i] is ChoiceItem c&&c.Id==product?.MaLoai)category.SelectedIndex=i;loading=false;});
        };
    }
    private Dictionary<string,Control> Fields()=>new(){["Name"]=name,["Category"]=category,["Unit"]=unit,["Price"]=price,["Shelf"]=shelf,["Threshold"]=low,["Image"]=picture,["Note"]=note};
    private Task Save()=>Run(async()=>{
        if(readOnly)return;
        if(original is{DangKinhDoanh:true}&&state.SelectedIndex==1&&!Ui.Confirm(this,"Ngừng bán bánh này? Lịch sử vẫn được giữ nguyên."))return;
        var input=new SanPham(original?.MaSanPham??0,ChoiceItem.Selected(category)??0,name.Text,unit.Text,price.Value,state.SelectedIndex==0,note.Text,(int)shelf.Value,image,low.Value);
        await Task.Run(()=>Services.Catalog.SaveProduct(Session,input));dirty=false;Back();
    },Fields());
    private void Back()=>Design.Open(this,new ProductPage(Services,Session));
    public bool CanLeave()=>!dirty||Ui.Confirm(this,"Bỏ các thay đổi chưa lưu của bánh này?");
    private void ChooseImage()
    {
        using var dialog=new OpenFileDialog{Filter="Ảnh bánh|*.jpg;*.jpeg;*.png",Title="Chọn ảnh bánh"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try{var info=new FileInfo(dialog.FileName);if(info.Length>5*1024*1024)throw new BusinessException("Ảnh tối đa 5 MB.");
            byte[] bytes=File.ReadAllBytes(dialog.FileName);using var stream=new MemoryStream(bytes);using var check=Image.FromStream(stream);
            if(check.RawFormat.Guid!=System.Drawing.Imaging.ImageFormat.Png.Guid&&check.RawFormat.Guid!=System.Drawing.Imaging.ImageFormat.Jpeg.Guid)throw new BusinessException("Chỉ chọn ảnh JPG hoặc PNG.");
            image=bytes;ShowImage();dirty=true;
        }catch(Exception ex) when(ex is IOException or ArgumentException or BusinessException){MessageBox.Show(this,ex.Message,"Ảnh không hợp lệ");}
    }
    private void ShowImage(){picture.Image?.Dispose();picture.Image=null;if(image is null)return;try{using var stream=new MemoryStream(image);using var source=Image.FromStream(stream);picture.Image=new Bitmap(source);}catch(Exception ex) when(ex is ArgumentException or OutOfMemoryException){picture.BackColor=Design.Header;}}
    protected override void Dispose(bool disposing){if(disposing)picture.Image?.Dispose();base.Dispose(disposing);}
}
