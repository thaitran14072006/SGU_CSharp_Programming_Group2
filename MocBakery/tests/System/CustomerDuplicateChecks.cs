using MocBakery.GUI;
using MocBakery.GUI.Forms.Customers;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;

namespace MocBakery.GuiChecks;

// Exercises duplicate-result binding without opening a database or showing dialogs.
internal static class CustomerDuplicateChecks
{
    internal static int Run()
    {
        var services=new AppServices("Server=(localdb)\\MSSQLLocalDB;Database=NotOpened;Integrated Security=True");
        var session=new UserSession("layout-only",0,"layout","Layout",Roles.ChuTiem);
        using var page=new CustomerPage(services,session){BindingContext=new BindingContext()};
        var grid=(DataGridView)page.Controls.Find("CustomerGrid",true).Single();
        var name=(TextBox)page.Controls.Find("CustomerName",true).Single();
        var phone=(TextBox)page.Controls.Find("CustomerPhone",true).Single();
        var address=(TextBox)page.Controls.Find("CustomerAddress",true).Single();
        name.Text="Khách đang nhập";phone.Text="0901234567";address.Text="Địa chỉ đang nhập";
        KhachHang[] matches=[new(7,"Khách đã có","0901234567","Địa chỉ đã lưu",null)];
        int passed=0,failed=0,confirmations=0;
        var previousConfirmation=Ui.TestConfirmation;
        Ui.TestConfirmation=_=>{confirmations++;return false;};
        static void Assert(bool condition){if(!condition)throw new InvalidOperationException("Assertion failed.");}
        void Check(string title,Action action)
        {
            try{action();Console.WriteLine("PASS "+title);passed++;}
            catch(Exception ex){Console.WriteLine("FAIL "+title+": "+ex.Message);failed++;}
        }
        static IEnumerable<Control> Descendants(Control parent)=>parent.Controls.Cast<Control>()
            .SelectMany(child=>new[]{child}.Concat(Descendants(child)));
        try
        {
            Check("Duplicate results bind without a discard confirmation",()=>
            {
                page.ShowDuplicateCustomers(matches);
                Assert(grid.Rows.Count==1&&confirmations==0);
            });
            Check("Duplicate results preserve unsaved customer fields",()=>
                Assert(name.Text=="Khách đang nhập"&&phone.Text=="0901234567"&&address.Text=="Địa chỉ đang nhập"));
            Check("Duplicate results do not select an existing customer automatically",()=>
                Assert(grid.SelectedRows.Count==0));
            Check("Duplicate result count appears in the page footer",()=>
                Assert(Descendants(page).OfType<Label>().Any(label=>label.Text=="Hiển thị 1 bản ghi")));
            Check("Empty duplicate results clear the table and update the footer",()=>
            {
                page.ShowDuplicateCustomers([]);
                Assert(grid.Rows.Count==0&&Descendants(page).OfType<Label>().Any(label=>label.Text=="Chưa có dữ liệu")
                    &&name.Text=="Khách đang nhập"&&confirmations==0);
            });
            Check("Leaving the page still protects the unsaved draft",()=>
            {
                Assert(!page.CanLeave()&&confirmations==1);
            });
        }
        finally{Ui.TestConfirmation=previousConfirmation;}
        Console.WriteLine($"CUSTOMER DUPLICATE RESULT: {passed} passed, {failed} failed; no database operations.");
        return failed==0?0:1;
    }
}
