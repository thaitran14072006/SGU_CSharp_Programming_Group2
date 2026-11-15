using MocBakery.GUI.Forms.Accounts;
namespace MocBakery.GUI;
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_,e) => { string code = ErrorLog.Write(e.Exception); MessageBox.Show("Có lỗi ngoài dự kiến. Mã: " + code); };
        try
        {
            var services = new AppServices();
            if(args.Contains("--ui-check")) { UiSmoke.Run(services); return; }
            while(true)
            {
                using var login = new LoginForm(services);
                if(login.ShowDialog() != DialogResult.OK || login.Session is null) break;
                using var main = new MainForm(services,login.Session);
                main.ShowDialog();
                services.Auth.Logout(login.Session);
                if(!main.SignedOut) break;
            }
        }
        catch(Exception ex)
        {
            if(args.Contains("--ui-check")){Console.Error.WriteLine(ex);Environment.ExitCode=1;return;}
            string code = ErrorLog.Write(ex); MessageBox.Show("Không khởi động được ứng dụng. Kiểm tra appsettings.local.json. Mã: " + code);
        }
    }
}
