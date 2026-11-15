using MocBakery.GUI;
using MocBakery.GUI.Forms.Catalog;
using MocBakery.GUI.Forms.Inventory;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Inventory;
using MocBakery.Models.Entities;
namespace MocBakery.GuiChecks;
internal static class GuiDesignWorkflow
{
    internal static int Run(AppServices services,UserSession owner)
    {
        var errors=new List<Exception>();var confirmations=new List<string>();Ui.TestErrorSink=errors.Add;Ui.TestConfirmation=text=>{confirmations.Add(text);return true;};
        string folder=Environment.GetEnvironmentVariable("MOCBAKERY_UI_OUTPUT")??Path.Combine(AppContext.BaseDirectory,"design-sql-check");Directory.CreateDirectory(folder);
        int passed=0,failed=0;
        void Assert(bool value){if(!value)throw new Exception("Assertion failed");}
        void Test(string label,Action action){try{action();Console.WriteLine("PASS "+label);passed++;}catch(Exception ex){Console.WriteLine("FAIL "+label+": "+ex);failed++;}}
        void Wait(Func<bool> done)
        {
            var stop=DateTime.UtcNow.AddSeconds(12);
            while(!done()&&DateTime.UtcNow<stop){Application.DoEvents();Thread.Sleep(15);}
            Application.DoEvents();if(!done())throw new TimeoutException("UI did not reach expected state. "+string.Join("; ",errors.Select(e=>e.Message)));
        }
        void Snapshot(Form form,string file){form.PerformLayout();Application.DoEvents();using var image=new Bitmap(form.Width,form.Height);form.DrawToBitmap(image,new Rectangle(Point.Empty,form.Size));image.Save(Path.Combine(folder,file+".png"));}
        static T Find<T>(Control parent,string name) where T:Control=>(T)parent.Controls.Find(name,true).Single();
        static IEnumerable<Control> Walk(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(var nested in Walk(child))yield return nested;}}
        static Button Button(Control parent,string text)=>Walk(parent).OfType<Button>().Single(b=>b.Text==text);
        using var host=new MainForm(services,owner);host.StartPosition=FormStartPosition.Manual;host.Location=new Point(-20000,-20000);host.Show();Application.DoEvents();
        string suffix=Guid.NewGuid().ToString("N")[..6];int category=services.Catalog.SaveCategory(owner,new(0,"GUI Excel "+suffix,null));
        int supplier=services.Warehouse.SaveSupplier(owner,new(0,"GUI NCC "+suffix,"02812345678",null,null));
        int material=services.Warehouse.SaveMaterial(owner,new(0,"GUI Bột "+suffix,"Kg",true,5));
        string cakeName="GUI Bánh "+suffix;int product=0,receiptId=0;
        Test("GUI detail validation prevents empty product save",()=>{
            var page=new ProductDetailPage(services,owner,null);host.ShowPage(page);Wait(()=>Find<ComboBox>(page,"ProductCategory").Items.Count>0&&page.Enabled);
            errors.Clear();Button(page,"Lưu bánh").PerformClick();Wait(()=>errors.Count>0);Assert(errors.Single() is BusinessException{Field:"Name"});Assert(!services.Catalog.Products(owner).Any(p=>p.TenSanPham==cakeName));Snapshot(host,"01-detail-validation");
        });
        Test("GUI product detail saves fields through real SQL services",()=>{
            var page=Walk(host).OfType<ProductDetailPage>().Single();Find<TextBox>(page,"ProductName").Text=cakeName;
            var choices=Find<ComboBox>(page,"ProductCategory");for(int i=0;i<choices.Items.Count;i++)if(choices.Items[i] is ChoiceItem c&&c.Id==category)choices.SelectedIndex=i;
            Find<ComboBox>(page,"ProductUnit").SelectedItem="Hộp";Find<NumericUpDown>(page,"ProductPrice").Value=42000;Find<NumericUpDown>(page,"ProductShelf").Value=5;
            errors.Clear();Button(page,"Lưu bánh").PerformClick();Wait(()=>Walk(host).OfType<ProductPage>().Any()&&services.Catalog.Products(owner,cakeName).Count==1);
            var saved=services.Catalog.Products(owner,cakeName).Single();product=saved.MaSanPham;Assert(saved.GiaBan==42000&&saved.HanDungNgay==5&&saved.DonViTinh=="Hộp");Snapshot(host,"02-product-saved");
        });
        var receiptPage=new ReceiptPage(services,owner);host.ShowPage(receiptPage);Wait(()=>Find<ComboBox>(receiptPage,"ReceiptSupplier").Items.Count>0&&receiptPage.Enabled);
        var supplierChoices=Find<ComboBox>(receiptPage,"ReceiptSupplier");for(int i=0;i<supplierChoices.Items.Count;i++)if(supplierChoices.Items[i] is ChoiceItem c&&c.Id==supplier)supplierChoices.SelectedIndex=i;
        void AddLine(bool raw,string lot,decimal qty,decimal cost)
        {
            Exception? error=null;using var timer=new System.Windows.Forms.Timer{Interval=100};
            timer.Tick+=(_,_)=>{
                var dialog=Application.OpenForms.OfType<EditDialog>().FirstOrDefault(f=>f.Text.StartsWith("Dòng phiếu nhập",StringComparison.Ordinal));if(dialog is null)return;timer.Stop();
                try{
                    Find<ComboBox>(dialog,"Kind").SelectedIndex=raw?1:0;var item=Find<ComboBox>(dialog,"Item");int id=raw?material:product;
                    for(int i=0;i<item.Items.Count;i++)if(item.Items[i] is ChoiceItem option&&option.Id==id)item.SelectedIndex=i;
                    Find<TextBox>(dialog,"Lot").Text=lot;Find<NumericUpDown>(dialog,"Quantity").Value=qty;Find<NumericUpDown>(dialog,"Cost").Value=cost;
                    Find<DateTimePicker>(dialog,"Produced").Value=DateTime.Today;Find<DateTimePicker>(dialog,"Expiry").Value=DateTime.Today.AddDays(5);Button(dialog,"Lưu").PerformClick();if(dialog.Visible&&errors.Count>0)throw new Exception(string.Join("; ",errors.Select(e=>e.Message)));
                }catch(Exception ex){error=ex;dialog.DialogResult=DialogResult.Cancel;dialog.Close();}
            };
            timer.Start();Button(receiptPage,"+ Thêm dòng").PerformClick();if(error is not null)throw error;Application.DoEvents();
        }
        Test("GUI adds cake and raw material lines to receipt",()=>{
            errors.Clear();AddLine(false,"GUI-C-"+suffix,3,20000);AddLine(true,"GUI-NL-"+suffix,2.5m,24000);Assert(Find<DataGridView>(receiptPage,"ReceiptLines").Rows.Count==2&&errors.Count==0);Snapshot(host,"03-receipt-lines");
        });
        Test("GUI Save draft leaves stock unchanged and computes total",()=>{
            int before=services.Warehouse.Receipts(owner).Count;errors.Clear();Button(receiptPage,"Lưu nháp").PerformClick();Wait(()=>receiptPage.Enabled&&services.Warehouse.Receipts(owner).Count==before+1);
            var receipt=services.Warehouse.Receipts(owner).OrderByDescending(r=>r.Id).First();receiptId=receipt.Id;Assert(receipt.Total==120000&&receipt.State=="Nhap");Assert(!services.Warehouse.Stock(owner,ItemKinds.Cake,cakeName).Any());Snapshot(host,"04-saved-draft-no-stock");
        });
        Test("GUI Post receipt creates stock once and locks line editing",()=>{
            errors.Clear();Button(receiptPage,"Ghi sổ phiếu").PerformClick();Wait(()=>receiptPage.Enabled&&services.Warehouse.Receipt(owner,receiptId).State=="DaGhiSo");
            Assert(!Find<Button>(receiptPage,"AddReceiptLine").Enabled&&!Button(receiptPage,"Ghi sổ phiếu").Visible&&confirmations.Any(c=>c.Contains("Ghi sổ")));
            Assert(services.Warehouse.Stock(owner,ItemKinds.Cake,cakeName).Sum(l=>l.Remaining)==3&&services.Warehouse.Stock(owner,ItemKinds.Material,"GUI Bột "+suffix).Sum(l=>l.Remaining)==2.5m);Snapshot(host,"05-posted-locked");
        });
        Test("GUI stock screen loads both item types after posting",()=>{
            var page=new InventoryPage(services,owner);host.ShowPage(page);Wait(()=>page.Enabled&&Walk(page).OfType<DataGridView>().Single().Rows.Count>0);Snapshot(host,"06-cake-stock");
            var kind=Walk(page).OfType<ComboBox>().Single(c=>c.Items.Contains("Nguyên liệu"));kind.SelectedIndex=1;Wait(()=>page.Enabled&&Walk(page).OfType<DataGridView>().Single().Rows.Cast<DataGridViewRow>().Any(r=>r.DataBoundItem is WarehouseRow l&&l.ItemId==material));Snapshot(host,"07-material-stock");
        });
        Test("GUI cashier stock screen exposes no inventory write actions",()=>{
            var accounts=services.Accounts.List(owner);var account=accounts.FirstOrDefault(a=>a.TenDangNhap=="excel-cashier");
            if(account is null)services.Accounts.Save(owner,0,new("excel-cashier","Thu ngân thử",Roles.ThuNgan,true,"GuiFixtureOnly2026!"));
            var cashier=services.Auth.Login("excel-cashier","GuiFixtureOnly2026!");var page=new InventoryPage(services,cashier);host.ShowPage(page);Wait(()=>page.Enabled&&Walk(page).OfType<DataGridView>().Single().Rows.Count>0);
            var buttons=Walk(page).OfType<Button>().Select(b=>b.Text).ToList();Assert(!buttons.Contains("Nhập kho")&&!buttons.Contains("Xuất / Điều chỉnh")&&!buttons.Contains("Nguyên liệu"));Snapshot(host,"08-cashier-read-only");services.Auth.Logout(cashier);
        });
        host.Close();Ui.TestErrorSink=null;Ui.TestConfirmation=null;Console.WriteLine($"GUI SQL RESULT: {passed} passed, {failed} failed. Offscreen form events with real SQL; confirmation responses supplied by test host.");return failed==0?0:1;
    }
}
