using System.Text.Json;
using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Customers;
using MocBakery.BLL.Services.Sales;
using MocBakery.BLL.Services.Orders;
using MocBakery.BLL.Services.Reports;
using MocBakery.DAL.Repositories.Sales;
using MocBakery.DAL.Repositories.Accounts;
using MocBakery.DAL.Repositories.Customers;
using MocBakery.DAL.Repositories.Orders;
using MocBakery.DAL.Repositories.Reports;
using MocBakery.DAL;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.BLL.Services.Catalog;
using MocBakery.BLL.Services.Inventory;
using MocBakery.DAL.Repositories.Catalog;
using MocBakery.DAL.Repositories.Inventory;
namespace MocBakery.GUI;
internal sealed class AppServices
{
    internal AuthService Auth { get; }
    internal AccountService Accounts { get; }
    internal StaffService Staff { get; }
    internal CustomerService Customers { get; }
    internal SalesService Sales { get; }
    internal OrderService Orders { get; }
    internal ReportService Reports { get; }
    internal CatalogService Catalog { get; }
    internal InventoryService Inventory { get; }
    internal WarehouseService Warehouse { get; }
    private readonly TaiKhoanRepository repository;
    internal AppServices() : this(ResolveConnection()) { }
    internal AppServices(string connection)
    {
        var db = new DbConnectionFactory(connection); repository = new(db);
        Auth = new(repository);
        var staff=new StaffRepository(db);Staff=new(staff,Auth);Accounts=new(repository,Auth,staff);
        Customers = new(new KhachHangRepository(db),Auth);
        Sales = new(new SalesRepository(db),Auth);
        Orders = new(new OrderRepository(db),Auth);
        Reports = new(new ReportRepository(db),Auth);
        var categories = new LoaiBanhRepository(db);
        var products = new SanPhamRepository(db);
        Catalog = new(categories,products,Auth);
        Inventory = new(new LoBanhRepository(db),products,Auth);
        Warehouse = new(new WarehouseRepository(db),Auth);
    }
    private static string ResolveConnection()
    {
        string? connection = Environment.GetEnvironmentVariable("MOCBAKERY_CONNECTION_STRING");
        string config = Path.Combine(AppContext.BaseDirectory,"appsettings.local.json");
        if(string.IsNullOrWhiteSpace(connection) && File.Exists(config))
        {
            using var json = JsonDocument.Parse(File.ReadAllText(config));
            connection = json.RootElement.GetProperty("ConnectionString").GetString();
        }
        connection ??= @"Server=.\SQLEXPRESS;Database=QuanLyTiemBanh;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5";
        return connection;
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
