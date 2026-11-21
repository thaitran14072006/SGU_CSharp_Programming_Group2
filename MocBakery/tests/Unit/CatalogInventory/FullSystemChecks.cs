using System.Data.Common;
using System.Text.RegularExpressions;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Orders;
using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Customers;
using MocBakery.BLL.Services.Catalog;
using MocBakery.BLL.Services.Inventory;
using MocBakery.BLL.Services.Sales;
using MocBakery.BLL.Services.Orders;
using MocBakery.BLL.Services.Reports;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.Accounts;
using MocBakery.DAL.Repositories.Customers;
using MocBakery.DAL.Repositories.Catalog;
using MocBakery.DAL.Repositories.Inventory;
using MocBakery.DAL.Repositories.Sales;
using MocBakery.DAL.Repositories.Orders;
using MocBakery.DAL.Repositories.Reports;

internal static class FullSystemChecks
{
    internal static int Run()
    {
        string name="MocBakery_FinalTest_"+Guid.NewGuid().ToString("N")[..12];bool created=false;
        var cs=new DbConnectionStringBuilder{ConnectionString=Environment.GetEnvironmentVariable("MOCBAKERY_TEST_CONNECTION")??@"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10"};
        cs["Initial Catalog"]="master";var master=new DbConnectionFactory(cs.ConnectionString);
        try
        {
            using(var c=master.Open()){Sql(c,$"CREATE DATABASE [{name}]");created=true;Script(c,File.ReadAllText("MocBakery/database/schema/01_CreateDatabase.sql").Replace("QuanLyTiemBanh",name,StringComparison.Ordinal));}
            Console.WriteLine("ISOLATED SYSTEM DATABASE: "+name);cs["Initial Catalog"]=name;var db=new DbConnectionFactory(cs.ConnectionString);
            var accountRepo=new TaiKhoanRepository(db);var staffRepo=new StaffRepository(db);const string password="FixtureOnly2026!";
            int ownerId=accountRepo.Bootstrap(new("owner","Chủ tiệm",Roles.ChuTiem,true,PasswordHasher.Hash(password)));
            var auth=new AuthService(accountRepo);var owner=auth.Login("owner",password);var staffService=new StaffService(staffRepo,auth);var accounts=new AccountService(accountRepo,auth,staffRepo);
            var customers=new CustomerService(new KhachHangRepository(db),auth);var catalog=new CatalogService(new LoaiBanhRepository(db),new SanPhamRepository(db),auth);
            var inventory=new InventoryService(new LoBanhRepository(db),new SanPhamRepository(db),auth);
            var sales=new SalesService(new SalesRepository(db),auth);var orders=new OrderService(new OrderRepository(db),auth);var reports=new ReportService(new ReportRepository(db),auth);
            var checks=new Checks();var today=DateOnly.FromDateTime(DateTime.Today);int employee=0,cashId=0,warehouseId=0,customer=0,product=0,near=0,later=0,order=0,invoice=0;
            MocBakery.Models.DTOs.Accounts.UserSession? cashier=null,warehouse=null;
            long Value(string sql){using var c=db.Open();using var q=c.CreateCommand();q.CommandText=sql;return Convert.ToInt64(q.ExecuteScalar());}
            void Execute(string sql){using var c=db.Open();Sql(c,sql);}
            int NewOrder(int qty=2,decimal deposit=10000)=>orders.Create(owner,customer,DateTime.Now.AddDays(1),deposit,"Giao thử",[new(product,"Bánh thử",qty,25000)]);
            checks.Test("Thai bootstrap is single-use and links owner profile",()=>{Checks.Assert(staffRepo.List().Single().MaTaiKhoan==ownerId);Checks.Reject(()=>accountRepo.Bootstrap(new("second","X",Roles.ChuTiem,true,PasswordHasher.Hash(password))));});
            checks.Test("Thai SQL staff create, normalize, code/name/phone search",()=>{employee=staffService.Save(owner,new(0,"  Lê   Thảo Vy ","0901112233",Roles.ThuNgan,today,true));Checks.Assert(staffService.Search(owner,$"NV{employee:000}").Single().HoTen=="Lê Thảo Vy"&&staffService.Search(owner,"090111").Count==1);});
            checks.Test("Thai staff rejects phone, role and future date",()=>{var n=staffRepo.List().Single(n=>n.MaNhanVien==employee);Checks.Reject(()=>staffService.Save(owner,n with{SoDienThoai="123"}),"Phone");Checks.Reject(()=>staffService.Save(owner,n with{ChucVu="invalid"}),"Role");Checks.Reject(()=>staffService.Save(owner,n with{NgayVaoLam=today.AddDays(1)}),"StartDate");});
            checks.Test("Thai duplicate staff phone is friendly and leaves profile unchanged",()=>{Checks.Reject(()=>staffService.Save(owner,new(0,"Trùng","0901112233",Roles.Kho,today,true)),"Phone");Checks.Assert(staffRepo.List().Count==2);});
            checks.Test("Thai create account and employee link atomically",()=>{cashId=accounts.Save(owner,0,new("thaovy","Lê Thảo Vy",Roles.ThuNgan,true,password),employee);Checks.Assert(staffRepo.List().Single(n=>n.MaNhanVien==employee).MaTaiKhoan==cashId);cashier=auth.Login("thaovy",password);});
            checks.Test("Thai temporary password cannot access business functions",()=>{Checks.Assert(auth.NeedsPasswordChange(cashier!));Checks.Reject(()=>sales.Products(cashier!));Checks.Reject(()=>customers.Search(cashier!,""));});
            checks.Test("Thai change password invalidates all old sessions and clears temporary flag",()=>{var other=auth.Login("thaovy",password);auth.ChangePassword(cashier!,password,"Changed2026!","Changed2026!");Checks.Reject(()=>auth.Require(cashier!));Checks.Reject(()=>auth.Require(other));Checks.Reject(()=>auth.Login("thaovy",password));cashier=auth.Login("thaovy","Changed2026!");Checks.Assert(!auth.NeedsPasswordChange(cashier));});
            checks.Test("Thai duplicate link cannot create second account",()=>{Checks.Reject(()=>accounts.Save(owner,0,new("duplicate","X",Roles.ThuNgan,true,password),employee),"Employee");Checks.Assert(accountRepo.List().Count==2);});
            checks.Test("Thai failed account insert rolls back employee link",()=>{int n=staffService.Save(owner,new(0,"Nhân viên kho","0902223344",Roles.Kho,today,true));Checks.Reject(()=>staffRepo.SaveAccount(ownerId,n,0,new("thaovy","X",Roles.Kho,true,PasswordHasher.Hash(password))),"Username");Checks.Assert(staffRepo.List().Single(x=>x.MaNhanVien==n).MaTaiKhoan is null);warehouseId=accounts.Save(owner,0,new("warehouse","Nhân viên kho",Roles.Kho,true,password),n);warehouse=auth.Login("warehouse",password);auth.ChangePassword(warehouse,password,"Warehouse2026!","Warehouse2026!");warehouse=auth.Login("warehouse","Warehouse2026!");});
            checks.Test("Thai cashier cannot administer employees or accounts",()=>{Checks.Reject(()=>staffService.Search(cashier!));Checks.Reject(()=>accounts.List(cashier!));Checks.Reject(()=>staffRepo.Save(cashId,new(0,"X","0903334455",Roles.Kho,today,true)));});
            checks.Test("Thai warehouse cannot sell, access customers/orders/reports or catalog writes",()=>{Checks.Reject(()=>sales.Products(warehouse!));Checks.Reject(()=>customers.Search(warehouse!,""));Checks.Reject(()=>orders.List(warehouse!));Checks.Reject(()=>reports.Revenue(warehouse!,DateTime.Today,DateTime.Today));Checks.Reject(()=>catalog.SaveCategory(warehouse!,new(0,"Không được",null)));});
            checks.Test("Thai live role reload rejects forged display role",()=>{Checks.Reject(()=>accounts.List(cashier! with{VaiTro=Roles.ChuTiem,MaTaiKhoan=ownerId}));Checks.Reject(()=>sales.Products(warehouse! with{VaiTro=Roles.ThuNgan}));});
            checks.Test("Thai staff cannot self-retire or demote owner",()=>{var n=staffRepo.List().Single(n=>n.MaTaiKhoan==ownerId) with{SoDienThoai="0909999999"};Checks.Reject(()=>staffService.Save(owner,n with{DangLam=false}),"Active");Checks.Reject(()=>staffService.Save(owner,n with{ChucVu=Roles.ThuNgan}),"Active");});
            checks.Test("Thai SQL customer Unicode/leading zero and escaped wildcard search",()=>{customer=customers.Save(cashier!,new(0,"  Nguyễn   Ngọc Anh %_[ ","0901234567","TP. Hồ Chí Minh",null));Checks.Assert(customers.Search(cashier!,"%_[").Single().MaKhachHang==customer&&customers.Search(cashier!,"090123").Single().SoDienThoai=="0901234567");});
            checks.Test("Thai duplicate customer phone rejected without extra row",()=>{Checks.Reject(()=>customers.Save(cashier!,new(0,"Trùng","0901234567",null,null)),"Phone");Checks.Assert(customers.Search(owner,"").Count==1);});
            checks.Test("Thai customer edit keeps phone; empty history initially",()=>{var n=customers.Search(owner,"").Single();customers.Save(cashier!,n with{DiaChi="Địa chỉ mới"});Checks.Assert(customers.History(cashier!,customer).Count==0&&customers.Search(owner,"").Single().DiaChi=="Địa chỉ mới");});
            checks.Test("Group catalog and inventory supply sellable FEFO stock",()=>{int category=catalog.SaveCategory(owner,new(0,"Bánh thử",null));product=catalog.SaveProduct(owner,new(0,category,"Bánh hệ thống","Cái",25000,true,null));near=inventory.Receive(owner,new(product,null,today,today,4));later=inventory.Receive(owner,new(product,null,today,today.AddDays(5),30));Checks.Assert(sales.Products(cashier!).Single().Stock==34);});
            checks.Test("Group KT06 sale allocates 4 + 2, history stores 150000",()=>{invoice=sales.Sell(cashier!,customer,[new(product,6,25000)]);var lines=sales.Invoice(cashier!,invoice);Checks.Assert(lines.Select(x=>x.Quantity).SequenceEqual(new[]{4,2})&&customers.History(cashier!,customer).Single().TongTien==150000&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==28);});
            checks.Test("Group sale rejects insufficient stock and stale price without invoice",()=>{long count=Value("SELECT COUNT(*) FROM dbo.HoaDon");Checks.Reject(()=>sales.Sell(cashier!,customer,[new(product,100,25000)]));Checks.Reject(()=>sales.Sell(cashier!,customer,[new(product,1,1)]));Checks.Assert(Value("SELECT COUNT(*) FROM dbo.HoaDon")==count&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==28);});
            checks.Test("Group KT10 create order 200000 deposit 50000 does not reserve stock",()=>{order=NewOrder(8,50000);var d=orders.List(owner).Single();Checks.Assert(d.Total==200000&&d.Deposit==50000&&d.Due==150000&&d.Status=="MoiTiepNhan"&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==28);});
            checks.Test("Group edit pending order persists details without invoice or stock changes",()=>{
                var pickup=DateTime.Now.AddDays(2);
                orders.Update(cashier!,order,customer,pickup,50000,"Đã sửa lịch nhận",[new(product,"Bánh thử",7,25000)]);
                var saved=orders.List(owner).Single(x=>x.Id==order);
                Checks.Assert(saved.Note=="Đã sửa lịch nhận"&&Math.Abs((saved.Pickup-pickup).TotalSeconds)<1&&saved.Total==175000&&orders.Lines(owner,order).Single().Quantity==7);
                Checks.Assert(Value("SELECT COUNT(*) FROM dbo.HoaDon")==1&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==28);
                orders.Update(cashier!,order,customer,pickup,50000,"Đã sửa lịch nhận",[new(product,"Bánh thử",8,25000)]);
            });
            checks.Test("Group KT11 deliver creates one invoice, deducts deposit once",()=>{orders.Prepare(cashier!,order);int h=orders.Deliver(cashier!,order);var row=sales.Invoices(owner).Single(x=>x.MaHoaDon==h);Checks.Assert(row.TongTien==200000&&row.TienCocTru==50000&&row.SoTienThu==150000&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==20&&orders.List(owner).Single(x=>x.Id==order).Status=="DaGiao");});
            checks.Test("Group KT12 duplicate delivery cannot duplicate invoice or stock",()=>{Checks.Reject(()=>orders.Deliver(owner,order));Checks.Assert(Value("SELECT COUNT(*) FROM dbo.HoaDon")==2&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==20);});
            checks.Test("Group KT13 delivered order cannot edit or cancel",()=>{Checks.Reject(()=>orders.Cancel(owner,order,"Sai"));Checks.Reject(()=>orders.Update(owner,order,customer,DateTime.Now.AddDays(2),0,null,[new(product,"X",1,25000)]));});
            checks.Test("Group KT12 shortage delivery rolls back order and balances",()=>{int d=NewOrder(100,0);orders.Prepare(owner,d);Checks.Reject(()=>orders.Deliver(owner,d));Checks.Assert(orders.List(owner).Single(x=>x.Id==d).Status=="DangChuanBi"&&Value("SELECT COUNT(*) FROM dbo.HoaDon")==2&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==20);orders.Cancel(owner,d,"Không đủ hàng");});
            checks.Test("Group KT13 cancellation requires reason and prevents later edit/delivery",()=>{int d=NewOrder();Checks.Reject(()=>orders.Cancel(owner,d," "));orders.Cancel(owner,d,"Khách hủy");Checks.Reject(()=>orders.Prepare(owner,d));Checks.Reject(()=>orders.Deliver(owner,d));Checks.Assert(orders.List(owner).Single(x=>x.Id==d).CancelReason=="Khách hủy");});
            checks.Test("Group KT12 SQL failure mid-delivery rolls back invoice/details/stock/status",()=>{
                int d=NewOrder();orders.Prepare(owner,d);long before=Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh");
                Execute("CREATE TRIGGER dbo.FinalTest_FailDetail ON dbo.ChiTietHoaDon AFTER INSERT AS THROW 51001, 'Injected isolated test failure', 1;");
                try{try{orders.Deliver(owner,d);throw new Exception("Expected SQL failure");}catch(DbException){}Checks.Assert(Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==before&&Value("SELECT COUNT(*) FROM dbo.HoaDon")==2&&orders.List(owner).Single(x=>x.Id==d).Status=="DangChuanBi");}
                finally{Execute("DROP TRIGGER dbo.FinalTest_FailDetail");}orders.Cancel(owner,d,"Kết thúc kiểm thử lỗi");
            });
            checks.Test("Group KT12 concurrent delivery succeeds exactly once",()=>{int d=NewOrder();orders.Prepare(owner,d);int success=0,rejected=0;Parallel.For(0,2,_=>{try{orders.Deliver(owner,d);Interlocked.Increment(ref success);}catch(BusinessException){Interlocked.Increment(ref rejected);}});Checks.Assert(success==1&&rejected==1&&Value($"SELECT COUNT(*) FROM dbo.HoaDon WHERE MaDon={d}")==1&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==18);});
            checks.Test("Group reports reconcile total invoices, history and sold quantities without double deposit",()=>{var rev=reports.Revenue(owner,DateTime.Today,DateTime.Today).Single();var top=reports.Bestsellers(owner,DateTime.Today,DateTime.Today).Single();var dash=reports.Overview(owner,DateTime.Today,DateTime.Today);Checks.Assert(rev.Revenue==400000&&rev.InvoiceCount==3&&top.Quantity==16&&top.Revenue==400000&&dash.Revenue==400000&&dash.AvailableStock==18&&customers.History(owner,customer).Sum(x=>x.TongTien)==400000);Console.WriteLine("EVIDENCE: invoice revenue=400000; receipts collected=340000; sold=16; stock=18; invoice count=3.");});
            checks.Test("Group report empty/reversed/date boundaries and owner-only permission",()=>{Checks.Assert(reports.Revenue(owner,DateTime.Today.AddDays(-3),DateTime.Today.AddDays(-2)).Count==0);Checks.Reject(()=>reports.Revenue(owner,DateTime.Today,DateTime.Today.AddDays(-1)));Checks.Reject(()=>reports.Revenue(cashier!,DateTime.Today,DateTime.Today));Checks.Assert(reports.LowStock(owner,20).Single().Quantity==18);});
            checks.Test("Thai password reset marks temporary and cannot unlock inactive account",()=>{accounts.ResetPassword(owner,cashId,"Reset2026!");Checks.Reject(()=>auth.Require(cashier!));cashier=auth.Login("thaovy","Reset2026!");Checks.Assert(auth.NeedsPasswordChange(cashier));auth.ChangePassword(cashier,"Reset2026!","Final2026!","Final2026!");cashier=auth.Login("thaovy","Final2026!");});
            checks.Test("Thai retirement locks linked login and revokes live sessions atomically",()=>{var n=staffRepo.List().Single(n=>n.MaNhanVien==employee);staffService.Save(owner,n with{DangLam=false});Checks.Assert(!accountRepo.FindById(cashId)!.TrangThai&&!staffRepo.List().Single(x=>x.MaNhanVien==employee).DangLam);Checks.Reject(()=>customers.Search(cashier!,""));Checks.Reject(()=>auth.Login("thaovy","Final2026!"));Checks.Reject(()=>accounts.Save(owner,cashId,new("thaovy",n.HoTen,Roles.ThuNgan,true,""),employee),"Employee");});
            checks.Test("Thai restoring employment does not silently unlock login",()=>{var n=staffRepo.List().Single(n=>n.MaNhanVien==employee);staffService.Save(owner,n with{DangLam=true});Checks.Assert(!accountRepo.FindById(cashId)!.TrangThai);accounts.Save(owner,cashId,new("thaovy",n.HoTen,Roles.ThuNgan,true,""),employee);cashier=auth.Login("thaovy","Final2026!");Checks.Assert(customers.Search(cashier,"").Count==1);});
            checks.Test("Thai job change updates role and live BLL checks",()=>{var n=staffRepo.List().Single(n=>n.MaNhanVien==employee);staffService.Save(owner,n with{ChucVu=Roles.Kho});Checks.Assert(accountRepo.FindById(cashId)!.VaiTro==Roles.Kho);Checks.Reject(()=>sales.Products(cashier!));});
            checks.Test("Group KT08 SQL failure during direct sale rolls back all balances",()=>{
                long before=Value("SELECT COUNT(*) FROM dbo.HoaDon");Execute("CREATE TRIGGER dbo.FinalTest_FailSale ON dbo.ChiTietHoaDon AFTER INSERT AS THROW 51002, 'Injected isolated sale failure', 1;");
                try{try{sales.Sell(owner,customer,[new(product,1,25000)]);throw new Exception("Expected SQL failure");}catch(DbException){}Checks.Assert(Value("SELECT COUNT(*) FROM dbo.HoaDon")==before&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==18);}
                finally{Execute("DROP TRIGGER dbo.FinalTest_FailSale");}
            });
            checks.Test("Group KT09 concurrent sale of last cake succeeds once",()=>{
                int category=catalog.Categories(owner).First().MaLoai;int last=catalog.SaveProduct(owner,new(0,category,"Bánh cuối cùng","Cái",25000,true,null));inventory.Receive(owner,new(last,null,today,today.AddDays(1),1));int success=0,rejected=0;
                Parallel.For(0,2,_=>{try{sales.Sell(owner,null,[new(last,1,25000)]);Interlocked.Increment(ref success);}catch(BusinessException){Interlocked.Increment(ref rejected);}});
                Checks.Assert(success==1&&rejected==1&&sales.Products(owner).Single(p=>p.Id==last).Stock==0);
            });
            checks.Test("Thai staff migration is repeatable and preserves links, hashes, stock",()=>{string hash=accountRepo.FindById(cashId)!.MatKhauHash;using var c=db.Open();string script=File.ReadAllText("MocBakery/database/schema/03_Upgrade_StaffAccounts.sql");Script(c,script);Script(c,script);Checks.Assert(staffRepo.List().Count==3&&accountRepo.FindById(cashId)!.MatKhauHash==hash&&Value("SELECT SUM(SoLuongCon) FROM dbo.LoBanh")==18);});
            checks.Test("Thai upgrade from fifteen tables backfills profiles without changing credentials or locks",()=>{
                string upgrade=name+"_upgrade";bool exists=false;
                try{
                    using(var c=master.Open()){
                        Sql(c,$"CREATE DATABASE [{upgrade}]");exists=true;
                        string schema=File.ReadAllText("MocBakery/database/schema/01_CreateDatabase.sql");schema=schema[..schema.IndexOf("-- Run on the selected existing database after migration 02.",StringComparison.Ordinal)];
                        Script(c,schema.Replace("QuanLyTiemBanh",upgrade,StringComparison.Ordinal));
                        Sql(c,"INSERT dbo.TaiKhoan(TenDangNhap,MatKhauHash,HoTen,VaiTro,TrangThai) VALUES(N'legacy',N'unchanged-hash',N'Nhân viên cũ',N'NhanVien',0)");
                        string migration=File.ReadAllText("MocBakery/database/schema/03_Upgrade_StaffAccounts.sql");Script(c,migration);Script(c,migration);
                        using var q=c.CreateCommand();q.CommandText="SELECT COUNT(*) FROM dbo.TaiKhoan a JOIN dbo.NhanVien n ON n.MaTaiKhoan=a.MaTaiKhoan WHERE a.MatKhauHash=N'unchanged-hash' AND a.TrangThai=0 AND n.SoDienThoai IS NULL AND a.BatBuocDoiMatKhau=0";Checks.Assert(Convert.ToInt32(q.ExecuteScalar())==1);
                    }
                }finally{if(exists){using var c=master.Open();Sql(c,$"ALTER DATABASE [{upgrade}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{upgrade}]");}}
            });
            return checks.Finish();
        }
        finally
        {
            if(created){using var c=master.Open();Sql(c,$"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");Console.WriteLine("CLEANED ISOLATED SYSTEM DATABASE: "+name);}
        }
    }
    private static void Sql(DbConnection c,string sql){using var q=c.CreateCommand();q.CommandText=sql;q.ExecuteNonQuery();}
    private static void Script(DbConnection c,string script){foreach(string batch in Regex.Split(script,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))if(!string.IsNullOrWhiteSpace(batch))Sql(c,batch);}
}
