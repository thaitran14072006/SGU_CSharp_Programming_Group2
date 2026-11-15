using System.Text.RegularExpressions;
using System.Text.Json;
using MocBakery.GUI;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.Accounts;
using MocBakery.BLL.Services.Accounts;
using MocBakery.Models.Entities;
namespace MocBakery.GuiChecks;
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string? config=File.Exists(Path.Combine(AppContext.BaseDirectory,"fixture.local.json"))
            ? File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"fixture.local.json")) : null;
        using var json=JsonDocument.Parse(config??"{}");
        string database=args.FirstOrDefault()??(json.RootElement.TryGetProperty("Database",out var value)?value.GetString():null)??"";
        if(!Regex.IsMatch(database,@"^MocBakery_NhuGuiTest_[a-zA-Z0-9_]+$"))
            throw new ArgumentException("Provide an isolated MocBakery_NhuGuiTest_ database. This test host never opens business databases.");
        string connection=$@"Server=(localdb)\MSSQLLocalDB;Database={database};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=3";
        var services=new AppServices(connection);
        var repository=new TaiKhoanRepository(new DbConnectionFactory(connection));
        // Fixture credentials apply only to a local database with the mandatory test prefix.
        const string password="GuiFixtureOnly2026!";
        if(repository.FindByUsername("gui-owner") is null)
            repository.Bootstrap(new("gui-owner","Chủ tiệm thử giao diện",Roles.ChuTiem,true,PasswordHasher.Hash(password)));
        bool staff=args.Contains("--staff")||(json.RootElement.TryGetProperty("Staff",out var role)&&role.GetBoolean());
        if(staff&&repository.FindByUsername("gui-staff") is null)
            repository.Create(new("gui-staff","Nhân viên thử giao diện",Roles.NhanVien,true,PasswordHasher.Hash(password)));
        var session=services.Auth.Login(staff?"gui-staff":"gui-owner",password);
        using var main=new MainForm(services,session);
        main.Text="Mộc Bakery • Kiểm thử SQL GUI • "+(staff?"Nhân viên":"Chủ tiệm");
        Application.Run(main);
        services.Auth.Logout(session);
    }
}
