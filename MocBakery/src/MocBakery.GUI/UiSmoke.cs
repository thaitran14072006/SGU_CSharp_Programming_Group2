using MocBakery.GUI.Forms.Accounts;
using MocBakery.GUI.Forms.Customers;
using MocBakery.GUI.Forms.Sales;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;
namespace MocBakery.GUI;
internal static class UiSmoke
{
    internal static bool Active {get;private set;}
    internal static void Run(AppServices services)
    {
        Active=true;
        var session=new UserSession("UI-check-no-login",0,"layout","Kiểm tra bố cục",Roles.ChuTiem);
        var items=new List<Form>{new LoginForm(services),new SetupForm(services),new ChangePasswordForm(services,session),new MainForm(services,session)};
        foreach(var page in new UserControl[]{new AccountPage(services,session),new CustomerPage(services,session),
            new SalesPage(services,session),new SalesPage(services,session,showInvoices:true)})
        {
            var host=new MainForm(services,session);host.ShowPage(page);host.Text=page.GetType().Name;items.Add(host);
        }
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
                using var bitmap=new Bitmap(form.Width,form.Height);
                form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));
                bitmap.Save(Path.Combine(folder,$"{++index:00}-{form.Text.Split('•')[0].Trim()}.png"));
                form.Close();
            }
        }
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"ui-check.local.json"),$"{{\"result\":\"{index} forms constructed and laid out\"}}");
    }
}
