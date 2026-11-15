using MocBakery.GUI;
using MocBakery.GUI.Forms.Accounts;
using MocBakery.GUI.Forms.Customers;
using MocBakery.GUI.Forms.Inventory;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
namespace MocBakery.GuiChecks;

internal static class GuiThaiWorkflow
{
    internal static int Run(AppServices services,UserSession owner)
    {
        var errors=new List<Exception>();Ui.TestErrorSink=errors.Add;Ui.TestConfirmation=_=>true;
        int passed=0,failed=0;string suffix=Guid.NewGuid().ToString("N")[..6];
        string folder=Environment.GetEnvironmentVariable("MOCBAKERY_UI_OUTPUT")??Path.Combine(AppContext.BaseDirectory,"thai-sql-check");Directory.CreateDirectory(folder);
        void Assert(bool ok){if(!ok)throw new Exception("Assertion failed");}
        void Test(string title,Action action){try{errors.Clear();action();Console.WriteLine("PASS "+title);passed++;}catch(Exception ex){Console.WriteLine("FAIL "+title+": "+ex);failed++;}}
        void Wait(Func<bool> done){DateTime limit=DateTime.UtcNow.AddSeconds(12);while(!done()&&DateTime.UtcNow<limit){Application.DoEvents();Thread.Sleep(15);}Application.DoEvents();if(!done())throw new TimeoutException(string.Join("; ",errors.Select(x=>x.Message)));}
        static IEnumerable<Control> Walk(Control p){foreach(Control c in p.Controls){yield return c;foreach(var nested in Walk(c))yield return nested;}}
        static T Find<T>(Control p,string name)where T:Control=>(T)p.Controls.Find(name,true).Single();
        static Button Button(Control p,string text)=>Walk(p).OfType<Button>().Single(b=>b.Text==text);
        using var host=new MainForm(services,owner){StartPosition=FormStartPosition.Manual,Location=new Point(-20000,-20000)};host.Show();Application.DoEvents();
        void Snapshot(string file){host.PerformLayout();Application.DoEvents();using var bitmap=new Bitmap(host.Width,host.Height);host.DrawToBitmap(bitmap,new Rectangle(Point.Empty,host.Size));bitmap.Save(Path.Combine(folder,file+".png"));}
        var staff=new StaffPage(services,owner);host.ShowPage(staff);Wait(()=>staff.Enabled&&Find<DataGridView>(staff,"StaffGrid").DataSource is not null);
        string employeeName="GUI Thái "+suffix,phone="09"+Random.Shared.Next(10000000,99999999);
        Test("Thai GUI staff validates phone without SQL insert",()=>{Find<TextBox>(staff,"StaffName").Text=employeeName;Find<TextBox>(staff,"StaffPhone").Text="123";Button(staff,"Lưu").PerformClick();Wait(()=>staff.Enabled&&errors.Count>0);Assert(errors.Single() is BusinessException{Field:"Phone"}&&services.Staff.Search(owner,employeeName).Count==0);Snapshot("01-staff-validation");});
        Test("Thai GUI saves staff through real SQL",()=>{Find<TextBox>(staff,"StaffPhone").Text=phone;Button(staff,"Lưu").PerformClick();Wait(()=>staff.Enabled&&services.Staff.Search(owner,employeeName).Count==1);Assert(errors.Count==0);Snapshot("02-staff-saved");});
        var account=new AccountPage(services,owner);host.ShowPage(account);Wait(()=>account.Enabled&&Find<DataGridView>(account,"AccountGrid").DataSource is not null);
        string username="gui_"+suffix;
        Test("Thai GUI account requires employee selection",()=>{Button(account,"Lưu tài khoản").PerformClick();Wait(()=>account.Enabled&&errors.Count>0);Assert(errors.Single() is BusinessException{Field:"Employee"});});
        Test("Thai GUI creates linked account and displays permission matrix",()=>{
            var choices=Find<ComboBox>(account,"AccountEmployee");for(int i=0;i<choices.Items.Count;i++)if(choices.Items[i]?.ToString()?.Contains(employeeName)==true)choices.SelectedIndex=i;
            Find<TextBox>(account,"AccountUsername").Text=username;Find<TextBox>(account,"AccountPassword").Text="GuiTemp2026!";Find<ComboBox>(account,"AccountRole").SelectedIndex=1;
            Button(account,"Lưu tài khoản").PerformClick();Wait(()=>account.Enabled&&services.Accounts.List(owner).Any(a=>a.TenDangNhap==username));
            var a=services.Accounts.List(owner).Single(a=>a.TenDangNhap==username);Assert(services.Staff.Search(owner,employeeName).Single().MaTaiKhoan==a.MaTaiKhoan&&Find<DataGridView>(account,"PermissionGrid").Rows.Count==5&&errors.Count==0);Snapshot("03-account-linked");
        });
        Test("Thai GUI refuses navigation when unsaved changes are kept",()=>{Find<TextBox>(account,"AccountUsername").Text="unsaved";Ui.TestConfirmation=_=>false;host.ShowPage(new InventoryPage(services,owner));Assert(!account.IsDisposed&&Walk(host).Contains(account));Ui.TestConfirmation=_=>true;Button(account,"Thêm mới").PerformClick();});
        var customer=new CustomerPage(services,owner);host.ShowPage(customer);Wait(()=>customer.Enabled&&Find<DataGridView>(customer,"CustomerGrid").DataSource is not null);
        string customerName="GUI Khách "+suffix,customerPhone="08"+Random.Shared.Next(10000000,99999999);
        Test("Thai GUI customer validates phone",()=>{Find<TextBox>(customer,"CustomerName").Text=customerName;Find<TextBox>(customer,"CustomerPhone").Text="abc";Button(customer,"Lưu").PerformClick();Wait(()=>customer.Enabled&&errors.Count>0);Assert(errors.Single() is BusinessException{Field:"Phone"});});
        Test("Thai GUI customer saves Unicode and leading-zero phone",()=>{Find<TextBox>(customer,"CustomerPhone").Text=customerPhone;Find<TextBox>(customer,"CustomerAddress").Text="TP. Hồ Chí Minh";Button(customer,"Lưu").PerformClick();Wait(()=>customer.Enabled&&services.Customers.Search(owner,customerPhone).Count==1);Assert(services.Customers.Search(owner,customerPhone).Single().HoTen==customerName&&errors.Count==0);Snapshot("04-customer-saved");});
        Test("Thai GUI cashier menu hides accounts/staff and warehouse menu hides sales/customers",()=>{
            var cashier=owner with{VaiTro=Roles.ThuNgan};using var cashierHost=new MainForm(services,cashier);
            var menu=Walk(cashierHost).OfType<Button>().Select(b=>b.AccessibleName).ToHashSet();Assert(!menu.Contains("Nhân viên")&&!menu.Contains("Tài khoản"));
            using var warehouseHost=new MainForm(services,owner with{VaiTro=Roles.Kho});menu=Walk(warehouseHost).OfType<Button>().Select(b=>b.AccessibleName).ToHashSet();Assert(!menu.Contains("Bán hàng")&&!menu.Contains("Khách hàng")&&menu.Contains("Phiếu nhập kho"));
        });
        host.Close();Ui.TestErrorSink=null;Ui.TestConfirmation=null;Console.WriteLine($"THAI GUI SQL RESULT: {passed} passed, {failed} failed. Offscreen form events with real SQL.");return failed==0?0:1;
    }
}
