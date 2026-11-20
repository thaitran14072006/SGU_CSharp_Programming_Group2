using System.Collections.Concurrent;
using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Catalog;
using MocBakery.BLL.Services.Customers;
using MocBakery.BLL.Services.Inventory;
using MocBakery.BLL.Services.Orders;
using MocBakery.BLL.Services.Reports;
using MocBakery.BLL.Services.Sales;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.Accounts;
using MocBakery.DAL.Repositories.Catalog;
using MocBakery.DAL.Repositories.Customers;
using MocBakery.DAL.Repositories.Inventory;
using MocBakery.DAL.Repositories.Orders;
using MocBakery.DAL.Repositories.Reports;
using MocBakery.DAL.Repositories.Sales;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;

namespace MocBakery.Web.Admin;

// One employee session registry per Web host. Repositories open independent SQL connections.
public sealed class AdminServices
{
    private readonly ConcurrentDictionary<string,(UserSession Session,DateTimeOffset Expires)> sessions=new();
    public AuthService Auth {get;}
    public CatalogService Catalog {get;}
    public InventoryService Inventory {get;}
    public WarehouseService Warehouse {get;}
    public OrderService Orders {get;}
    public CustomerService Customers {get;}
    public StaffService Staff {get;}
    public AccountService Accounts {get;}
    public ReportService Reports {get;}
    public SalesService Sales {get;}
    public AdminServices(DbConnectionFactory db)
    {
        var accounts=new TaiKhoanRepository(db);var staff=new StaffRepository(db);
        Auth=new(accounts);Accounts=new(accounts,Auth,staff);Staff=new(staff,Auth);
        Catalog=new(new LoaiBanhRepository(db),new SanPhamRepository(db),Auth);
        Inventory=new(new LoBanhRepository(db),new SanPhamRepository(db),Auth);
        Warehouse=new(new WarehouseRepository(db),Auth);
        Orders=new(new OrderRepository(db),Auth);Customers=new(new KhachHangRepository(db),Auth);
        Reports=new(new ReportRepository(db),Auth);Sales=new(new SalesRepository(db),Auth);
    }
    public UserSession Login(string username,string password)
    {
        foreach(var expired in sessions.Where(x=>x.Value.Expires<=DateTimeOffset.UtcNow))Logout(expired.Key);
        var session=Auth.Login(username,password);
        sessions[session.Token]=(session,DateTimeOffset.UtcNow.AddMinutes(30));return session;
    }
    public UserSession Require(string? token)
    {
        if(token is null||!sessions.TryGetValue(token,out var entry))throw new BusinessException("Phiên quản trị đã kết thúc.");
        if(entry.Expires<=DateTimeOffset.UtcNow){Logout(token);throw new BusinessException("Phiên quản trị đã hết hạn.");}
        var current=Auth.Require(entry.Session,allowPasswordChange:true);
        return entry.Session with{HoTen=current.HoTen,VaiTro=current.VaiTro};
    }
    public void Logout(string? token){if(token is not null&&sessions.TryRemove(token,out var entry))Auth.Logout(entry.Session);}
}
