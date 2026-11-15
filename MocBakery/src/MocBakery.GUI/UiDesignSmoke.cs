using MocBakery.GUI.Forms.Sales;
using MocBakery.GUI.Forms.Accounts;
using MocBakery.GUI.Forms.Customers;
using MocBakery.GUI.Forms.Catalog;
using MocBakery.GUI.Forms.Inventory;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;
namespace MocBakery.GUI;
internal static class UiSmoke
{
    internal static bool Active{get;private set;}
    internal static void Run(AppServices services)
    {
        Active=true;var owner=new UserSession("preview-not-authenticated",0,"preview","Trần Thanh Nam",Roles.ChuTiem);
        var salesPage = new SalesPage(services, owner, previewProducts:
        [
            new(1, "Bánh thử", 15000, 9) { Category = "Bánh mì" },
            new(2, "Tiramisu mini", 45000, 18) { Category = "Bánh lạnh" },
            new(3, "Bánh kem dâu", 320000, 5) { Category = "Bánh kem" },
            new(4, "Cookie chocolate", 18000, 20) { Category = "Bánh quy" },
            new(5, "Muffin việt quất", 22000, 12) { Category = "Bánh kem" },
            new(6, "Bánh mì sữa", 22000, 24) { Category = "Bánh mì" }
        ]);
        var pages=new List<(string Name,Control Page)>();
        pages.Add(("BanHang",salesPage));
        pages.Add(("HoaDon",new SalesPage(services,owner,showInvoices:true)));
        var products=new ProductPage(services,owner);products.Preview();pages.Add(("05-QuanLyBanh",products));
        pages.Add(("06-ChiTietBanh",new ProductDetailPage(services,owner,new(1,1,"Croissant bơ","Cái",28000,true,null,2))));
        var categories=new CategoryPage(services,owner);categories.Preview();pages.Add(("07-DanhMucBanh",categories));
        var stock=new InventoryPage(services,owner);stock.Preview();pages.Add(("12-TonKho",stock));
        var receipt=new ReceiptPage(services,owner);pages.Add(("13-PhieuNhap",receipt));
        var supplier=new SupplierPage(services,owner);supplier.Preview();pages.Add(("14-NhaCungCap",supplier));
        var materials=new MaterialPage(services,owner);materials.Preview();pages.Add(("NguyenLieu",materials));
        var list=new ReceiptListPage(services,owner);list.Preview();pages.Add(("DanhSachPhieuNhap",list));
        var cashier=owner with{VaiTro=Roles.ThuNgan};pages.Add(("ThuNgan-QuanLyBanh",new ProductPage(services,cashier)));pages.Add(("ThuNgan-TonKho",new InventoryPage(services,cashier)));
        var storekeeper=owner with{VaiTro=Roles.Kho};var warehouse=new InventoryPage(services,storekeeper);warehouse.Preview();pages.Add(("Kho-TonKho",warehouse));
        string folder=Environment.GetEnvironmentVariable("MOCBAKERY_UI_OUTPUT")??Path.Combine(AppContext.BaseDirectory,"ui-check");Directory.CreateDirectory(folder);
        foreach(var (name,page) in pages)
        {
            using var host=new MainForm(services,name.StartsWith("ThuNgan")?cashier:name.StartsWith("Kho-")?storekeeper:owner);host.ShowPage(page);
            host.StartPosition=FormStartPosition.Manual;host.Location=new Point(-20000,-20000);host.Show();host.PerformLayout();Application.DoEvents();
            if(page==salesPage)CheckSalesQuantity(salesPage);
            using var bitmap=new Bitmap(host.Width,host.Height);host.DrawToBitmap(bitmap,new Rectangle(Point.Empty,host.Size));bitmap.Save(Path.Combine(folder,name+".png"));host.Close();
        }
        Console.WriteLine($"UI DESIGN CHECK: {pages.Count} layouts rendered. Preview values only; SQL workflows checked separately.");
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
    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach(Control child in parent.Controls){yield return child;foreach(var nested in Descendants(child))yield return nested;}
    }
}
