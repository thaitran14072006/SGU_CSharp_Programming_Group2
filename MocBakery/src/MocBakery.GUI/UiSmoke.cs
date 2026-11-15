using MocBakery.GUI.Forms.Accounts;
using MocBakery.GUI.Forms.Customers;
using MocBakery.GUI.Forms.Sales;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;
using MocBakery.GUI.Forms.Catalog;
using MocBakery.GUI.Forms.Inventory;
namespace MocBakery.GUI;
internal static class UiSmoke
{
    internal static bool Active {get;private set;}
    internal static void Run(AppServices services)
    {
        Active=true;
        var session=new UserSession("UI-check-no-login",0,"layout","Kiểm tra bố cục",Roles.ChuTiem);
        var salesPage = new SalesPage(services, session, previewProducts:
        [
            new(1, "Bánh thử", 15000, 9) { Category = "Bánh mì" },
            new(2, "Tiramisu mini", 45000, 18) { Category = "Bánh lạnh" },
            new(3, "Bánh kem dâu", 320000, 5) { Category = "Bánh kem" },
            new(4, "Cookie chocolate", 18000, 20) { Category = "Bánh quy" },
            new(5, "Muffin việt quất", 22000, 12) { Category = "Bánh kem" },
            new(6, "Bánh mì sữa", 22000, 24) { Category = "Bánh mì" }
        ]);
        var items=new List<Form>{new LoginForm(services),new SetupForm(services),new ChangePasswordForm(services,session),new MainForm(services,session)};
        foreach(var page in new UserControl[]{new AccountPage(services,session),new CustomerPage(services,session),
            salesPage,new SalesPage(services,session,showInvoices:true)})
        {
            var host=new MainForm(services,session);host.ShowPage(page);host.Text=page.GetType().Name;items.Add(host);
        }
        var category=new CategoryPage(services,session);category.Preview();
        var product=new ProductPage(services,session);product.Preview();
        var inventory=new InventoryPage(services,session);inventory.Preview();
        foreach(var page in new UserControl[]{category,product,inventory})
        {
            var host=new MainForm(services,session);host.ShowPage(page);host.Text=page.GetType().Name;items.Add(host);
        }
        var staff=session with{VaiTro=Roles.NhanVien};
        foreach(var page in new UserControl[]{new CategoryPage(services,staff),new ProductPage(services,staff),new InventoryPage(services,staff)})
        {
            var buttons=Descendants(page).OfType<Button>().Select(b=>b.Text).ToHashSet();
            if(buttons.Overlaps(new[]{"Thêm loại","Sửa loại","Xóa loại","Thêm bánh","Sửa / Ngừng bán","Nhập lô","Ngừng bán lô","Cho bán lại"}))
                throw new InvalidOperationException("Staff sees management actions");
            var host=new MainForm(services,staff);host.ShowPage(page);host.Text=page.GetType().Name+"-staff";items.Add(host);
        }
        items.Add(CategoryPage.Editor(null,_=>Task.CompletedTask));
        items.Add(ProductPage.Editor(null,[new(1,"Bánh ngọt",null)],_=>Task.CompletedTask));
        items.Add(InventoryPage.Editor([new(1,1,"Bánh mì bơ tỏi","Cái",25000,true,null)],_=>Task.CompletedTask));
        var lotPreview=new InventoryPage(services,session);lotPreview.Preview();lotPreview.ShowLotsPreview();
        var lotHost=new MainForm(services,session);lotHost.ShowPage(lotPreview);lotHost.Text="LotDetailsPage";items.Add(lotHost);
        string folder=Environment.GetEnvironmentVariable("MOCBAKERY_UI_OUTPUT") ?? Path.Combine(AppContext.BaseDirectory,"ui-check");
        Directory.CreateDirectory(folder);
        int index=0;
        foreach(var form in items)
        {
            using(form)
            {
                form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);
                form.Show();form.PerformLayout();Application.DoEvents();
                if(form.Controls.Count==0)throw new InvalidOperationException("Empty form");
                if (form.Contains(salesPage)) CheckSalesQuantity(salesPage);
                using var bitmap=new Bitmap(form.Width,form.Height);
                form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));
                bitmap.Save(Path.Combine(folder,$"{++index:00}-{form.Text.Split('•')[0].Trim()}.png"));
                form.Close();
            }
        }
        CheckEditorBindings();
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"ui-check.local.json"),$"{{\"forms\":{items.Count},\"editorBindings\":3,\"result\":\"preview data and fake callbacks; no live SQL GUI workflow\"}}");
        Console.WriteLine($"UI CHECK: {items.Count} layouts; 3 staff permission layouts; 3 editor input bindings passed. Preview/fake callbacks only, no live SQL GUI workflow.");
    }
    private static void CheckSalesQuantity(Control page)
    {
        var controls = Descendants(page).ToArray();
        controls.OfType<Button>().Single(button => button.Text == "Bánh thử").PerformClick();
        var quantity = controls.OfType<NumericUpDown>().Single(input => input.AccessibleName == "Số lượng");
        quantity.Value = 3;
        var cart = controls.OfType<DataGridView>().Single(grid => grid.Columns.Contains("Quantity"));
        if (Convert.ToInt32(cart.Rows[0].Cells["Quantity"].Value) != 3 ||
            Convert.ToDecimal(cart.Rows[0].Cells["Total"].Value) != 45000 ||
            !controls.OfType<Label>().Any(label => label.Text == $"Phải trả   {45000:N0} đ"))
            throw new InvalidOperationException("Cart quantity did not update the row and total.");
    }
    private static void CheckEditorBindings()
    {
        LoaiBanh? category=null;
        using(var form=CategoryPage.Editor(null,input=>{category=input;return Task.CompletedTask;}))
        {
            var inputs=Descendants(form).OfType<TextBox>().ToArray();inputs[0].Text="Loại thử";inputs[1].Text="Mô tả";Submit(form);
        }
        if(category is not{TenLoai:"Loại thử",MoTa:"Mô tả"})throw new InvalidOperationException("Category input binding");
        SanPham? product=null;
        using(var form=ProductPage.Editor(null,[new(7,"Loại thử",null)],input=>{product=input;return Task.CompletedTask;}))
        {
            var inputs=Descendants(form).OfType<TextBox>().Where(x=>x.Parent is not NumericUpDown).ToArray();
            inputs[0].Text="Bánh thử";inputs[1].Text="Hộp";inputs[2].Text="Mô tả bánh";
            Descendants(form).OfType<NumericUpDown>().Single().Value=12345.50m;
            Descendants(form).OfType<CheckBox>().Single().Checked=false;Submit(form);
        }
        if(product is not{MaLoai:7,TenSanPham:"Bánh thử",DonViTinh:"Hộp",GiaBan:12345.50m,DangKinhDoanh:false,MoTa:"Mô tả bánh"})
            throw new InvalidOperationException("Product input binding");
        Models.DTOs.Inventory.LotInput? lot=null;
        using(var form=InventoryPage.Editor([new(5,7,"Bánh thử","Cái",100,true,null)],input=>{lot=input;return Task.CompletedTask;}))
        {
            Descendants(form).OfType<TextBox>().First(x=>x.Parent is not NumericUpDown).Text="Bếp thử";
            Descendants(form).OfType<NumericUpDown>().Single().Value=6;
            var dates=Descendants(form).OfType<DateTimePicker>().ToArray();dates[0].Value=DateTime.Today.AddDays(-1);dates[1].Value=DateTime.Today.AddDays(2);Submit(form);
        }
        if(lot is not{MaSanPham:5,NguonNhap:"Bếp thử",SoLuongNhap:6}||lot.NgaySanXuat!=DateOnly.FromDateTime(DateTime.Today.AddDays(-1))||lot.HanSuDung!=DateOnly.FromDateTime(DateTime.Today.AddDays(2)))
            throw new InvalidOperationException("Lot input binding");
    }
    private static void Submit(Form form)
    {
        form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();
        Descendants(form).OfType<Button>().Single(b=>b.Text=="Lưu").PerformClick();Application.DoEvents();
        if(form.DialogResult!=DialogResult.OK)throw new InvalidOperationException("Editor did not save");
    }
    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach(Control child in parent.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}
    }
}
