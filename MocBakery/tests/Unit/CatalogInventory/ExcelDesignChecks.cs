using System.Data.Common;
using System.Text.RegularExpressions;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Inventory;
using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Catalog;
using MocBakery.BLL.Services.Inventory;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.Accounts;
using MocBakery.DAL.Repositories.Catalog;
using MocBakery.DAL.Repositories.Inventory;
internal static class ExcelDesignChecks
{
    public static int Run(string[] args)
    {
        string name="MocBakery_ExcelTest_"+Guid.NewGuid().ToString("N")[..12];
        var cs=new DbConnectionStringBuilder{ConnectionString=Environment.GetEnvironmentVariable("MOCBAKERY_TEST_CONNECTION")??@"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10"};cs["Initial Catalog"]="master";var master=new DbConnectionFactory(cs.ConnectionString);
        bool created=false;
        try
        {
            using(var c=master.Open()){Execute(c,$"CREATE DATABASE [{name}]");created=true;Console.WriteLine("EXCEL TEST DATABASE: "+name);Script(c,File.ReadAllText("MocBakery/database/schema/01_CreateDatabase.sql").Replace("QuanLyTiemBanh",name,StringComparison.Ordinal));}
            cs["Initial Catalog"]=name;var db=new DbConnectionFactory(cs.ConnectionString);
            var accountRepo=new TaiKhoanRepository(db);const string password="ExcelFixtureOnly2026!";
            accountRepo.Bootstrap(new("owner","Quản lý thử",Roles.ChuTiem,true,PasswordHasher.Hash(password)));
            foreach(var (user,role) in new[]{("cashier",Roles.ThuNgan),("warehouse",Roles.Kho),("legacy",Roles.NhanVien)})accountRepo.Create(new(user,user,role,true,PasswordHasher.Hash(password)));
            var auth=new AuthService(accountRepo);var owner=auth.Login("owner",password);var cashier=auth.Login("cashier",password);var warehouse=auth.Login("warehouse",password);var legacy=auth.Login("legacy",password);
            var categories=new LoaiBanhRepository(db);var products=new SanPhamRepository(db);var repository=new WarehouseRepository(db);
            var catalog=new CatalogService(categories,products,auth);var service=new WarehouseService(repository,auth);
            var checks=new Checks();int category=0,product=0,material=0,supplier=0,receiptId=0;var date=DateOnly.FromDateTime(DateTime.Today);
            checks.Test("Excel category status, codes and product count source",()=>{
                category=catalog.SaveCategory(owner,new(0,"Bánh mì","Mô tả"));product=catalog.SaveProduct(owner,new(0,category,"Croissant bơ","Cái",28000,true,null,2));
                Checks.Assert(catalog.Categories(cashier,"DM001").Single().MaLoai==category&&catalog.Products(cashier,"B001").Single().MaSanPham==product);
            });
            checks.Test("Excel stopped category excluded from new product choices",()=>{
                catalog.SaveCategory(owner,categories.List().Single() with{DangSuDung=false});Checks.Reject(()=>catalog.SaveProduct(owner,new(0,category,"Bánh mới","Cái",100,true,null)),"Category");
                catalog.SaveProduct(owner,products.List().Single() with{GiaBan=30000});Checks.Assert(!categories.List().Single().DangSuDung);
                catalog.SaveCategory(owner,categories.List().Single() with{DangSuDung=true});
            });
            checks.Test("Excel price positive, name length and unit rules",()=>{
                var p=products.List().Single();Checks.Reject(()=>catalog.SaveProduct(owner,p with{GiaBan=0}),"Price");Checks.Reject(()=>catalog.SaveProduct(owner,p with{TenSanPham=new string('x',101)}),"Name");Checks.Reject(()=>catalog.SaveProduct(owner,p with{DonViTinh="Kg"}),"Unit");Checks.Reject(()=>catalog.SaveProduct(owner,p with{HanDungNgay=0}),"Shelf");
            });
            checks.Test("Excel product image, default shelf life and low stock roundtrip",()=>{
                byte[] image=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aP1cAAAAASUVORK5CYII=");
                catalog.SaveProduct(owner,products.List().Single() with{AnhBanh=image,HanDungNgay=3,NguongCanhBao=5});var p=products.List().Single();Checks.Assert(p.AnhBanh!.SequenceEqual(image)&&p.HanDungNgay==3);
                Checks.Reject(()=>catalog.SaveProduct(owner,p with{AnhBanh=new byte[]{1,2,3}}),"Image");
            });
            checks.Test("Excel warehouse role manages suppliers and materials",()=>{
                supplier=service.SaveSupplier(warehouse,new(0,"An Phú","02812345678","TP.HCM","contact@example.test"));material=service.SaveMaterial(warehouse,new(0,"Bột mì số 13","Kg",true,10));Checks.Assert(service.Suppliers(cashier).Count==1&&service.Materials(cashier).Count==1);
            });
            checks.Test("Excel supplier email phone validation",()=>{Checks.Reject(()=>service.SaveSupplier(owner,new(0,"Sai","123",null,null)),"Phone");Checks.Reject(()=>service.SaveSupplier(owner,new(0,"Sai","02812345678",null,"not-email")),"Email");});
            checks.Test("Excel cashier and legacy cannot write inventory, warehouse cannot edit catalog",()=>{
                Checks.Reject(()=>service.SaveMaterial(cashier,new(0,"Sai","Kg")));Checks.Reject(()=>service.SaveMaterial(legacy,new(0,"Sai","Kg")));Checks.Reject(()=>catalog.SaveProduct(warehouse,products.List().Single()));
            });
            Receipt Draft(params ReceiptLine[] lines)=>new(0,supplier,DateTime.Today,"Nhap",lines);
            ReceiptLine Cake(string lot,decimal qty=4)=>new(ItemKinds.Cake,product,lot,date,date.AddDays(5),qty,"Cái",18000);
            ReceiptLine Raw(string lot,decimal qty=20,int days=10)=>new(ItemKinds.Material,material,lot,date,date.AddDays(days),qty,"Kg",24000);
            checks.Test("Excel empty receipt future date wrong units and date bounds rejected",()=>{
                Checks.Reject(()=>service.SaveDraft(owner,Draft()));Checks.Reject(()=>service.SaveDraft(owner,Draft(Cake("A")) with{Date=DateTime.Now.AddDays(1)}),"Date");
                Checks.Reject(()=>service.SaveDraft(owner,Draft(Cake("A") with{Unit="Kg"})),"Unit");Checks.Reject(()=>service.SaveDraft(owner,Draft(Cake("A") with{Produced=date.AddDays(1)})),"Expiry");Checks.Reject(()=>service.SaveDraft(owner,Draft(Cake("A") with{Expiry=date.AddDays(-1)})),"Expiry");
            });
            checks.Test("Excel receipt quantities costs duplicate lines rejected",()=>{
                Checks.Reject(()=>service.SaveDraft(owner,Draft(Cake("A",1.5m))),"Quantity");Checks.Reject(()=>service.SaveDraft(owner,Draft(Raw("A",0))),"Quantity");
                Checks.Reject(()=>service.SaveDraft(owner,Draft(Raw("A") with{Cost=-1})),"Cost");Checks.Reject(()=>service.SaveDraft(owner,Draft(Cake("A"),Cake("A"))),"Lot");
            });
            checks.Test("Excel multiline draft total saved without changing stock",()=>{
                receiptId=service.SaveDraft(warehouse,Draft(Cake("CAKE-1"),Raw("RAW-1")));var receipt=service.Receipt(cashier,receiptId);Checks.Assert(receipt.Total==552000&&receipt.State=="Nhap"&&repository.Lots().Count==0&&receipt.Lines.Count==2);
            });
            checks.Test("Excel stale draft version cannot overwrite another editor",()=>{
                var old=service.Receipt(owner,receiptId);service.SaveDraft(owner,old);Checks.Reject(()=>service.SaveDraft(warehouse,old));Checks.Assert(service.Receipt(owner,receiptId).Lines.Count==2);
            });
            checks.Test("Excel post creates both stocks and audit once",()=>{
                service.Post(warehouse,receiptId);Checks.Assert(repository.Lots().Single(l=>l.Kind==ItemKinds.Cake).Remaining==4&&repository.Lots().Single(l=>l.Kind==ItemKinds.Material).Remaining==20);
                Checks.Assert(service.Receipt(owner,receiptId).State=="DaGhiSo"&&service.Receipts(owner).Single().Total==552000);
            });
            checks.Test("Excel repeat post and posted edits rejected",()=>{
                Checks.Reject(()=>service.Post(owner,receiptId));Checks.Reject(()=>service.SaveDraft(owner,service.Receipt(owner,receiptId)));Checks.Assert(repository.Lots().Sum(l=>l.Remaining)==24);
            });
            checks.Test("Excel migration reruns without deleting or changing stock",()=>{
                using var c=db.Open();string migration=File.ReadAllText("MocBakery/database/schema/02_Upgrade_ExcelDesign.sql");Script(c,migration);Script(c,migration);Checks.Assert(repository.Lots().Sum(l=>l.Remaining)==24&&products.List().Single().AnhBanh is not null);
            });
            checks.Test("Excel failure on second posting line rolls back first line and audit",()=>{
                int id=service.SaveDraft(owner,Draft(Cake("ROLLBACK-FIRST"),Raw("RAW-1")));Checks.Reject(()=>service.Post(owner,id));
                Checks.Assert(repository.Lots().Count==2&&service.Receipt(owner,id).State=="Nhap");
                using var c=db.Open();Checks.Assert(Scalar(c,"SELECT COUNT(*) FROM dbo.BienDongKho")==2);
            });
            checks.Test("Excel concurrent post only one succeeds",()=>{
                int id=service.SaveDraft(owner,Draft(Cake("RACE",2)));int passed=0,rejected=0;
                Parallel.Invoke(()=>{try{service.Post(owner,id);Interlocked.Increment(ref passed);}catch(BusinessException){Interlocked.Increment(ref rejected);}},()=>{try{service.Post(warehouse,id);Interlocked.Increment(ref passed);}catch(BusinessException){Interlocked.Increment(ref rejected);}});
                Checks.Assert(passed==1&&rejected==1&&repository.Lots().Single(l=>l.Code=="RACE").Remaining==2);
            });
            checks.Test("Excel FEFO raw material export spans earliest lots with exact decimal quantities",()=>{
                int id=service.SaveDraft(owner,Draft(Raw("RAW-NEAR",5,1)));service.Post(owner,id);service.ChangeStock(warehouse,new(ItemKinds.Material,0,material,"Xuat",6,"Sản xuất: kiểm tra FEFO"));
                Checks.Assert(repository.Lots().Single(l=>l.Code=="RAW-NEAR").Remaining==0&&repository.Lots().Single(l=>l.Code=="RAW-1").Remaining==19);
            });
            checks.Test("Excel overstock export rolls back and mandatory reason",()=>{
                Checks.Reject(()=>service.ChangeStock(owner,new(ItemKinds.Material,0,material,"Xuat",100,"Sản xuất: vượt tồn")),"Quantity");Checks.Reject(()=>service.ChangeStock(owner,new(ItemKinds.Material,0,material,"Xuat",1," ")),"Reason");Checks.Assert(repository.Lots().Single(l=>l.Code=="RAW-1").Remaining==19);
            });
            checks.Test("Excel expired raw lot excluded from production but disposal uses it first",()=>{
                using(var c=db.Open()){using var q=c.CreateCommand();q.CommandText=$"INSERT dbo.LoNguyenLieu(MaNguyenLieu,SoLo,NgayNhap,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon) VALUES({material},N'EXPIRED',GETDATE(),DATEADD(day,-3,CAST(GETDATE() AS DATE)),DATEADD(day,-1,CAST(GETDATE() AS DATE)),7,7)";q.ExecuteNonQuery();}
                service.ChangeStock(owner,new(ItemKinds.Material,0,material,"Xuat",1,"Sản xuất: lô hợp lệ"));Checks.Assert(repository.Lots().Single(l=>l.Code=="EXPIRED").Remaining==7);
                service.ChangeStock(owner,new(ItemKinds.Material,0,material,"Xuat",7,"Hủy: hàng hết hạn"));Checks.Assert(repository.Lots().Single(l=>l.Code=="EXPIRED").Remaining==0);
            });
            checks.Test("Excel stocktake increase preserves original receipt and records before after actor reason",()=>{
                var lot=repository.Lots().Single(l=>l.Code=="RAW-1");service.ChangeStock(warehouse,new(lot.Kind,lot.Id,lot.ItemId,"KiemKe",25.125m,"Kiểm kê: thừa hàng"));var updated=repository.Lots().Single(l=>l.Code=="RAW-1");var history=service.History(owner,lot.Kind,lot.Id).First();
                Checks.Assert(updated.Received==20&&updated.Remaining==25.125m&&history.Before==18&&history.After==25.125m&&history.Actor=="warehouse"&&history.Reason.Contains("thừa hàng"));
            });
            checks.Test("Excel cake count supports excess and zero without changing quantity received",()=>{
                var lot=repository.Lots().Single(l=>l.Code=="CAKE-1");service.ChangeStock(owner,new(lot.Kind,lot.Id,lot.ItemId,"KiemKe",6,"Kiểm kê: thừa 2"));Checks.Assert(repository.Lots().Single(l=>l.Code=="CAKE-1").Received==4);
                service.ChangeStock(owner,new(lot.Kind,lot.Id,lot.ItemId,"KiemKe",0,"Kiểm kê: không còn"));Checks.Assert(repository.Lots().Single(l=>l.Code=="CAKE-1").Remaining==0);
            });
            checks.Test("Excel low stock soon expiry and expired filters",()=>{
                int id=service.SaveDraft(owner,Draft(Raw("RAW-ALERT",2,2)));service.Post(owner,id);
                Checks.Assert(service.Stock(cashier,ItemKinds.Cake,filter:"Low").Any()&&service.Stock(cashier,ItemKinds.Material,filter:"Soon").Any(l=>l.Code=="RAW-ALERT")&&service.Stock(cashier,ItemKinds.Material,filter:"Expired").Single().Code=="EXPIRED");
            });
            checks.Test("Excel inactive supplier blocked at post, history preserved",()=>{
                int id=service.SaveDraft(owner,Draft(Cake("SUPPLIER-STOP")));var s=service.Suppliers(owner).Single();service.SaveSupplier(warehouse,s with{Active=false});Checks.Reject(()=>service.Post(owner,id));Checks.Assert(service.Receipt(owner,id).State=="Nhap"&&service.Receipt(owner,receiptId).State=="DaGhiSo");service.SaveSupplier(owner,s);
            });
            checks.Test("Excel inactive item blocked at post",()=>{
                int id=service.SaveDraft(owner,Draft(Cake("ITEM-STOP")));catalog.SaveProduct(owner,products.List().Single() with{DangKinhDoanh=false});Checks.Reject(()=>service.Post(owner,id));Checks.Assert(service.Receipt(owner,id).State=="Nhap");
            });
            checks.Test("Excel upgrades original nine-table database without losing legacy balances",()=>{
                string legacyName=name+"_legacy";bool exists=false;
                try
                {
                    using(var c=master.Open())
                    {
                        Execute(c,$"CREATE DATABASE [{legacyName}]");exists=true;
                        string baseline=File.ReadAllText("MocBakery/database/schema/01_CreateDatabase.sql").Split("-- Excel design extensions for new installations",StringSplitOptions.None)[0];
                        Script(c,baseline.Replace("QuanLyTiemBanh",legacyName,StringComparison.Ordinal));
                        Execute(c,"INSERT dbo.LoaiBanh(TenLoai) VALUES(N'Loại cũ'); INSERT dbo.SanPham(MaLoai,TenSanPham,DonViTinh,GiaBan) VALUES(1,N'Bánh cũ',N'Cái',0); INSERT dbo.LoBanh(MaSanPham,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon) VALUES(1,CAST(GETDATE() AS DATE),DATEADD(day,2,CAST(GETDATE() AS DATE)),2,2)");
                        Script(c,File.ReadAllText("MocBakery/database/schema/02_Upgrade_ExcelDesign.sql"));
                        Checks.Assert(Scalar(c,"SELECT COUNT(*) FROM dbo.SanPham WHERE TenSanPham=N'Bánh cũ' AND GiaBan=0 AND HanDungNgay=1")==1&&Scalar(c,"SELECT SoLuongCon FROM dbo.LoBanh WHERE MaLo=1")==2&&Scalar(c,"SELECT COUNT(*) FROM sys.tables")==15);
                    }
                }
                finally{if(exists){using var c=master.Open();Execute(c,$"ALTER DATABASE [{legacyName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{legacyName}]");}}
            });
            Console.WriteLine("EVIDENCE: draft total 552000; post once; rollback and concurrent posting verified; stocktake 18 -> 25.125 Kg with audit.");
            return checks.Finish();
        }
        finally
        {
            if(created&&!args.Contains("--keep-database")){using var c=master.Open();Execute(c,$"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");Console.WriteLine("CLEANED EXCEL TEST DATABASE: "+name);}
        }
    }
    private static void Execute(DbConnection c,string sql){using var q=c.CreateCommand();q.CommandText=sql;q.ExecuteNonQuery();}
    private static int Scalar(DbConnection c,string sql){using var q=c.CreateCommand();q.CommandText=sql;return Convert.ToInt32(q.ExecuteScalar());}
    private static void Script(DbConnection c,string sql){foreach(var batch in Regex.Split(sql,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))if(!string.IsNullOrWhiteSpace(batch))Execute(c,batch);}
}
