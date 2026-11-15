using MocBakery.GUI.Forms.Catalog;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Inventory;
namespace MocBakery.GUI.Forms.Inventory;
internal sealed class ReceiptListPage:BakeryPage
{
    private readonly TextBox keyword=Design.Search("Tìm mã phiếu hoặc tên nhà cung cấp...");
    private readonly ComboBox state=Ui.Choice("Tất cả","Nháp","Đã ghi sổ");
    private readonly DataGridView grid=Design.Grid(("Code","Mã phiếu"),("Supplier","Nhà cung cấp"),("Date","Ngày nhập"),("Total","Tổng tiền"),("Status","Trạng thái"));
    internal ReceiptListPage(AppServices services,UserSession session):base(services,session,"Phiếu nhập kho","Tra cứu phiếu nhập bánh hoặc nguyên liệu từ nhà cung cấp.")
    {
        Tool("Tìm phiếu",keyword,340);Tool("Bộ lọc",state,250);if(Roles.CanManageInventory(session.VaiTro))Primary("+ Lập phiếu nhập",(_,_)=>Design.Open(this,new ReceiptPage(Services,Session)));
        Body.Controls.Add(grid);grid.Columns["Date"]!.DefaultCellStyle.Format="dd/MM/yyyy";grid.Columns["Total"]!.DefaultCellStyle.Format="#,##0.##";
        Actions.Controls.Add(Design.Button("Xem phiếu",(_,_)=>{if(grid.SelectedRows.Count>0&&grid.SelectedRows[0].DataBoundItem is ReceiptSummary receipt)Design.Open(this,new ReceiptPage(Services,Session,receipt.Id));},secondary:true));
        keyword.KeyDown+=async(_,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Reload();}};state.SelectedIndexChanged+=async(_,_)=>{if(IsHandleCreated)await Reload();};Load+=async(_,_)=>{if(!UiSmoke.Active)await Reload();};
    }
    private Task Reload()=>Run(async()=>{var rows=await Task.Run(()=>Services.Warehouse.Receipts(Session));grid.DataSource=rows.Where(r=>(state.SelectedIndex==0||r.State==(state.SelectedIndex==1?"Nhap":"DaGhiSo"))&&(r.Code.Contains(keyword.Text,StringComparison.OrdinalIgnoreCase)||r.Supplier.Contains(keyword.Text,StringComparison.CurrentCultureIgnoreCase))).ToList();Count(grid.Rows.Count);});
    internal void Preview(){grid.DataSource=new[]{new ReceiptSummary(1,"An Phú",DateTime.Today,"Nhap",480000),new ReceiptSummary(2,"Mộc Bakery",DateTime.Today,"DaGhiSo",150000)};Count(2);}
}
internal sealed class ReceiptPage:BakeryPage,IUnsavedPage
{
    private Receipt current;private readonly List<ReceiptLine> lines=[];private IReadOnlyList<WarehouseItem> items=[];
    private readonly ComboBox supplier=ChoiceItem.Combo([]);private readonly DateTimePicker date=Design.Date(DateTime.Today);
    private readonly DataGridView grid=Design.Grid(("Item","Mặt hàng"),("Quantity","Số lượng"),("Unit","ĐVT"),("Cost","Đơn giá (đ)"),("Amount","Thành tiền (đ)"));
    private readonly Label title=new(){AutoSize=true,Font=new Font("Segoe UI",17,FontStyle.Bold)},total=new(){AutoSize=true,Font=new Font("Segoe UI",16,FontStyle.Bold),ForeColor=Design.Orange},status=new(){AutoSize=true,Font=new Font("Segoe UI",10,FontStyle.Bold),ForeColor=Color.SeaGreen};
    private readonly FlowLayoutPanel lineButtons=new(){Dock=DockStyle.Fill,WrapContents=false};private readonly Button save,post;
    private bool dirty,loading=true;
    private sealed record LineRow(ReceiptLine Source,string Item)
    {public decimal Quantity=>Source.Quantity;public string Unit=>Source.Unit;public decimal Cost=>Source.Cost;public decimal Amount=>Source.Amount;}
    internal ReceiptPage(AppServices services,UserSession session,int id=0):base(services,session,"Phiếu nhập kho","Lập phiếu nhập bánh hoặc nguyên liệu từ nhà cung cấp.")
    {
        current=new(id,0,DateTime.Today,"Nhap",[]);LayoutRoot.RowStyles[1].Height=0;supplier.Name="ReceiptSupplier";grid.Name="ReceiptLines";
        var card=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=5,Padding=new Padding(20)};
        card.RowStyles.Add(new(SizeType.Absolute,36));card.RowStyles.Add(new(SizeType.Absolute,75));card.RowStyles.Add(new(SizeType.Absolute,45));card.RowStyles.Add(new(SizeType.Percent,100));card.RowStyles.Add(new(SizeType.Absolute,65));Body.Controls.Add(card);
        card.Controls.Add(title,0,0);var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};header.ColumnStyles.Add(new(SizeType.Percent,60));header.ColumnStyles.Add(new(SizeType.Percent,40));header.Controls.Add(Design.Field("Nhà cung cấp *",supplier),0,0);header.Controls.Add(Design.Field("Ngày nhập *",date),1,0);card.Controls.Add(header,0,1);
        var addLine=Design.Button("+ Thêm dòng",async(_,_)=>await EditLine(false));addLine.Name="AddReceiptLine";addLine.Width=145;lineButtons.Controls.Add(addLine);lineButtons.Controls.Add(Design.Button("Sửa dòng",async(_,_)=>await EditLine(true)));
        lineButtons.Controls.Add(Design.Button("Bỏ dòng",(_,_)=>{if(grid.SelectedRows.Count>0&&grid.SelectedRows[0].Index<lines.Count&&Ui.Confirm(this,"Bỏ dòng khỏi phiếu nháp?")){lines.RemoveAt(grid.SelectedRows[0].Index);dirty=true;Bind();}},secondary:true));
        card.Controls.Add(lineButtons,0,2);card.Controls.Add(grid,0,3);grid.Columns["Cost"]!.DefaultCellStyle.Format="#,##0.##";grid.Columns["Amount"]!.DefaultCellStyle.Format="#,##0.##";grid.Columns["Quantity"]!.DefaultCellStyle.Format="0.###";grid.Columns["Item"]!.FillWeight=240;
        var summary=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};summary.ColumnStyles.Add(new(SizeType.Percent,60));summary.ColumnStyles.Add(new(SizeType.Percent,40));status.Margin=new Padding(0,20,0,0);total.Margin=new Padding(0,17,0,0);summary.Controls.Add(status,0,0);summary.Controls.Add(total,1,0);card.Controls.Add(summary,0,4);
        post=Design.Button("Ghi sổ phiếu",async(_,_)=>await Post(),true);save=Design.Button("Lưu nháp",async(_,_)=>await Save());
        Actions.Controls.Add(post);Actions.Controls.Add(save);Actions.Controls.Add(Design.Button("Quay lại",(_,_)=>Design.Open(this,new ReceiptListPage(Services,Session)),secondary:true));
        supplier.SelectedIndexChanged+=(_,_)=>{if(!loading)dirty=true;};date.ValueChanged+=(_,_)=>{if(!loading)dirty=true;};
        Load+=async(_,_)=>{
            if(UiSmoke.Active){Preview();return;}
            await Run(async()=>{
                if(id>0)current=await Task.Run(()=>Services.Warehouse.Receipt(Session,id));
                items=await Task.Run(()=>Services.Warehouse.Items(Session));var suppliers=await Task.Run(()=>Services.Warehouse.Suppliers(Session));
                foreach(var s in suppliers.Where(s=>s.Active||s.Id==current.SupplierId))supplier.Items.Add(new ChoiceItem(s.Id,s.Name+(s.Active?"":" (ngừng hợp tác)")));
                if(supplier.Items.Count>0)supplier.SelectedIndex=0;for(int i=0;i<supplier.Items.Count;i++)if(supplier.Items[i] is ChoiceItem s&&s.Id==current.SupplierId)supplier.SelectedIndex=i;
                date.Value=current.Date;lines.AddRange(current.Lines);Bind();loading=false;
            });
        };
    }
    private void Bind()
    {
        title.Text="PHIẾU NHẬP "+current.Code;
        grid.DataSource=lines.Select(l=>new LineRow(l,(items.FirstOrDefault(i=>i.Kind==l.Kind&&i.Id==l.ItemId)?.Name??"Mặt hàng")+" · "+l.LotCode)).ToList();
        total.Text="Tổng tiền   "+lines.Sum(l=>l.Amount).ToString("#,##0.##")+" đ";
        bool editable=current.State=="Nhap"&&Roles.CanManageInventory(Session.VaiTro);lineButtons.Enabled=editable;supplier.Enabled=editable;date.Enabled=editable;save.Visible=editable;post.Visible=editable;
        status.Text=current.State=="Nhap"?"Nháp · Chưa cộng tồn kho":"Đã ghi sổ · Đã cộng tồn kho";
    }
    private Task EditLine(bool existing)=>Run(async()=>{
        ReceiptLine? selected=existing&&grid.SelectedRows.Count>0?lines[grid.SelectedRows[0].Index]:null;
        if(existing&&selected is null)throw new BusinessException("Chọn dòng hàng trước.");
        if(current.State!="Nhap")throw new BusinessException("Phiếu đã ghi sổ không được sửa.");
        if(items.All(i=>!i.Active))throw new BusinessException("Cần tạo bánh hoặc nguyên liệu đang sử dụng trước.");
        var type=Ui.Choice("Bánh thành phẩm","Nguyên liệu");type.SelectedIndex=selected?.Kind==ItemKinds.Material?1:0;
        var item=ChoiceItem.Combo([]);var lot=Ui.Input(50);lot.Text=selected?.LotCode??"L"+DateTime.Now.ToString("yyyyMMddHHmmssff");
        var produced=Design.Date(selected?.Produced.ToDateTime(TimeOnly.MinValue)??date.Value.Date);
        var expiry=Design.Date(selected?.Expiry.ToDateTime(TimeOnly.MinValue)??date.Value.Date.AddDays(1));
        var quantity=Design.Number(1000000000,3);quantity.Minimum=0.001m;quantity.Value=selected?.Quantity??1;
        var unit=Ui.Input(20);unit.ReadOnly=true;var cost=Design.Number(1000000000,2);cost.Value=selected?.Cost??0;
        void SetItem(bool resetDate){var chosen=items.FirstOrDefault(i=>i.Kind==(type.SelectedIndex==0?ItemKinds.Cake:ItemKinds.Material)&&i.Id==ChoiceItem.Selected(item));unit.Text=chosen?.Unit??"";quantity.DecimalPlaces=type.SelectedIndex==0?0:3;quantity.Minimum=type.SelectedIndex==0?1:0.001m;if(resetDate&&chosen is not null)expiry.Value=date.Value.Date.AddDays(chosen.ShelfDays);}
        void FillItems(){item.Items.Clear();foreach(var i in items.Where(i=>i.Kind==(type.SelectedIndex==0?ItemKinds.Cake:ItemKinds.Material)&&i.Active))item.Items.Add(new ChoiceItem(i.Id,i.Name));if(item.Items.Count>0)item.SelectedIndex=0;SetItem(true);}
        type.SelectedIndexChanged+=(_,_)=>FillItems();item.SelectedIndexChanged+=(_,_)=>SetItem(true);FillItems();
        if(selected is not null){for(int i=0;i<item.Items.Count;i++)if(item.Items[i] is ChoiceItem option&&option.Id==selected.ItemId)item.SelectedIndex=i;SetItem(false);expiry.Value=selected.Expiry.ToDateTime(TimeOnly.MinValue);}
        using var dialog=new EditDialog("Dòng phiếu nhập",[("Loại hàng *","Kind",type),("Mặt hàng *","Item",item),("Số lô *","Lot",lot),("Ngày sản xuất *","Produced",produced),("Hạn sử dụng *","Expiry",expiry),("Số lượng *","Quantity",quantity),("Đơn vị tính","Unit",unit),("Đơn giá (đ) *","Cost",cost)],()=>{
            var input=new ReceiptLine(type.SelectedIndex==0?ItemKinds.Cake:ItemKinds.Material,ChoiceItem.Selected(item)??0,lot.Text.Trim(),DateOnly.FromDateTime(produced.Value),DateOnly.FromDateTime(expiry.Value),quantity.Value,unit.Text,cost.Value);
            if(input.ItemId<=0)throw new BusinessException("Chọn mặt hàng.","Item");if(input.LotCode.Length==0)throw new BusinessException("Nhập số lô.","Lot");
            var receiveDate=DateOnly.FromDateTime(date.Value);if(input.Produced>receiveDate||input.Expiry<receiveDate)throw new BusinessException("Ngày sản xuất ≤ ngày nhập ≤ hạn dùng.","Expiry");
            if(input.Quantity<=0||(input.Kind==ItemKinds.Cake&&input.Quantity!=decimal.Truncate(input.Quantity)))throw new BusinessException("Số lượng bánh phải là số nguyên dương.","Quantity");
            int index=selected is null?-1:lines.IndexOf(selected);if(index<0)lines.Add(input);else lines[index]=input;dirty=true;return Task.CompletedTask;
        });if(dialog.ShowDialog(this)==DialogResult.OK)Bind();await Task.CompletedTask;
    });
    private async Task SaveData()
    {
        var input=current with{SupplierId=ChoiceItem.Selected(supplier)??0,Date=date.Value,Lines=lines.ToList()};
        int id=await Task.Run(()=>Services.Warehouse.SaveDraft(Session,input));current=await Task.Run(()=>Services.Warehouse.Receipt(Session,id));dirty=false;Bind();
    }
    private Task Save()=>Run(SaveData,new(){["Supplier"]=supplier,["Date"]=date});
    private Task Post()=>Run(async()=>{
        if(!Ui.Confirm(this,"Ghi sổ phiếu và tăng tồn? Phiếu đã ghi sổ sẽ khóa chỉnh sửa."))return;
        if(current.Id==0||dirty)await SaveData();int id=current.Id;await Task.Run(()=>Services.Warehouse.Post(Session,id));current=await Task.Run(()=>Services.Warehouse.Receipt(Session,id));dirty=false;Bind();
    },new(){["Supplier"]=supplier,["Date"]=date});
    public bool CanLeave()=>!dirty||Ui.Confirm(this,"Bỏ các thay đổi chưa lưu của phiếu nhập?");
    internal void Preview()
    {
        loading=true;items=[new(ItemKinds.Material,1,"Bột mì số 13","Kg",true,10)];supplier.Items.Clear();supplier.Items.Add(new ChoiceItem(1,"An Phú"));supplier.SelectedIndex=0;
        current=new(1,1,DateTime.Today,"Nhap",[]);lines.Clear();lines.Add(new(ItemKinds.Material,1,"NL25101",DateOnly.FromDateTime(DateTime.Today),DateOnly.FromDateTime(DateTime.Today.AddDays(60)),20,"Kg",24000));Bind();loading=false;
    }
}
