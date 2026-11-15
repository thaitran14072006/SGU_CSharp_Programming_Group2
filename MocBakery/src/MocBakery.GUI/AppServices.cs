using System.Text.Json;
using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Customers;
using MocBakery.BLL.Services.Sales;
using MocBakery.DAL.Repositories.Sales;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.Accounts;
using MocBakery.DAL.Repositories.Customers;
using MocBakery.Models;
using MocBakery.Models.Entities;
namespace MocBakery.GUI;
internal sealed class AppServices
{
    internal SalesService Sales { get; }
    internal AuthService Auth { get; }
    internal AccountService Accounts { get; }
    internal CustomerService Customers { get; }
    private readonly TaiKhoanRepository repository;
    internal AppServices()
    {
        string? connection = Environment.GetEnvironmentVariable("MOCBAKERY_CONNECTION_STRING");
        string config = Path.Combine(AppContext.BaseDirectory,"appsettings.local.json");
        if(string.IsNullOrWhiteSpace(connection) && File.Exists(config))
        {
            using var json = JsonDocument.Parse(File.ReadAllText(config));
            connection = json.RootElement.GetProperty("ConnectionString").GetString();
        }
        connection ??= @"Server=.\SQLEXPRESS;Database=QuanLyTiemBanh;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5";
        var db = new DbConnectionFactory(connection); repository = new(db);
        Auth = new(repository); Accounts = new(repository,Auth); Customers = new(new KhachHangRepository(db),Auth);
        Sales = new(new SalesRepository(db), Auth);
    }
    internal void Bootstrap(string username,string name,string password)
    {
        username = username.Trim(); name = name.Trim();
        if(username.Length is < 3 or > 50) throw new BusinessException("Tên đăng nhập phải có 3–50 ký tự.","Username");
        if(name.Length is < 1 or > 100) throw new BusinessException("Nhập họ tên, tối đa 100 ký tự.","FullName");
        AuthService.ValidatePassword(password);
        repository.Bootstrap(new(username,name,Roles.ChuTiem,true,PasswordHasher.Hash(password)));
    }
}
