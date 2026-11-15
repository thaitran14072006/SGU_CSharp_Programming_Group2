using System.Data.Common;
using System.Text.RegularExpressions;
using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Catalog;
using MocBakery.BLL.Services.Inventory;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.Catalog;
using MocBakery.DAL.Repositories.Inventory;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Inventory;
internal static class IntegrationChecks
{
    public static int Run(string[] args)
    {
        string schema=args.SkipWhile(a=>a!="--integration").Skip(1).FirstOrDefault()
            ?? "MocBakery/database/schema/01_CreateDatabase.sql";
        string name="MocBakery_NhuTest_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N")[..6];
        // Always create a new database, never use existing business data or delete a database.
        var builder=new DbConnectionStringBuilder{ConnectionString=Environment.GetEnvironmentVariable("MOCBAKERY_TEST_CONNECTION")
            ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10"};
        builder["Initial Catalog"]="master";
        var master=new DbConnectionFactory(builder.ConnectionString);
        using var cleanup=new TemporaryDatabases(master,args.Contains("--keep-database"));
        using(var c=master.Open())
        {
            using(var q=c.CreateCommand()) { q.CommandText=$"CREATE DATABASE [{name}]";q.ExecuteNonQuery(); }
            cleanup.Add(name);
            string script=File.ReadAllText(schema).Replace("QuanLyTiemBanh",name,StringComparison.Ordinal);
            foreach(string batch in Regex.Split(script,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
            {
                if(string.IsNullOrWhiteSpace(batch))continue;
                using var q=c.CreateCommand();q.CommandText=batch;q.ExecuteNonQuery();
            }
        }
        Console.WriteLine("TEST DATABASE: "+name);
        builder["Initial Catalog"]=name;var db=new DbConnectionFactory(builder.ConnectionString);
        var categories=new LoaiBanhRepository(db);var products=new SanPhamRepository(db);var lots=new LoBanhRepository(db);
        var accounts=new TestAccounts();var auth=new AuthService(accounts);var owner=auth.Login("owner","Password123");var staff=auth.Login("staff","Password123");
        var catalog=new CatalogService(categories,products,auth);var today=DateOnly.FromDateTime(DateTime.Today);
        var inventory=new InventoryService(lots,products,auth,()=>today);var checks=new Checks();
        int category=0,product=0,near=0,later=0,expired=0,stopped=0;
        checks.Test("SQL category insert and Unicode roundtrip",()=>{category=catalog.SaveCategory(owner,new(0,"Bánh thử ' % _ [", "Mô tả tiếng Việt"));Checks.Assert(categories.List().Single().MoTa=="Mô tả tiếng Việt");});
        checks.Test("SQL unique category handles database collation",()=>Checks.Reject(()=>categories.Save(new(0,"BÁNH THỬ ' % _ [",null)),"Name"));
        checks.Test("SQL category update",()=>{catalog.SaveCategory(owner,new(category,"Bánh ngọt","Đã sửa"));Checks.Assert(categories.List().Single().MoTa=="Đã sửa");});
        checks.Test("SQL product insert with decimal price",()=>{product=catalog.SaveProduct(owner,new(0,category,"Bánh ' đặc biệt","Cái",25000.50m,true,null));Checks.Assert(products.List().Single().GiaBan==25000.50m);});
        checks.Test("SQL duplicate product is friendly error",()=>Checks.Reject(()=>products.Save(new(0,category,"Bánh ' đặc biệt","Cái",0,true,null)),"Name"));
        checks.Test("SQL category FK prevents delete",()=>Checks.Reject(()=>catalog.DeleteCategory(owner,category)));
        checks.Test("SQL invalid category update is rejected",()=>Checks.Reject(()=>products.Save(products.List().Single() with{MaLoai=999}),"Category"));
        checks.Test("SQL price/date validation leaves database unchanged KT04",()=>{
            Checks.Reject(()=>catalog.SaveProduct(owner,products.List().Single() with{GiaBan=-10}),"Price");
            Checks.Reject(()=>inventory.Receive(owner,new(product,null,today,today.AddDays(-1),3)),"Expiry");
            Checks.Assert(lots.List().Count==0&&products.List().Single().GiaBan==25000.50m);
        });
        checks.Test("SQL receive initializes both quantities",()=>{
            later=inventory.Receive(owner,new(product,"Bếp thử",today,today.AddDays(3),5));
            near=inventory.Receive(owner,new(product,null,today,today,4));
            expired=inventory.Receive(owner,new(product,null,today.AddDays(-2),today.AddDays(-1),3));
            stopped=inventory.Receive(owner,new(product,null,today,today.AddDays(1),2));
            inventory.SetStatus(owner,stopped,LotStatus.Stopped);
            Checks.Assert(lots.List().All(l=>l.SoLuongNhap==l.SoLuongCon)&&lots.List().Count==4);
        });
        checks.Test("SQL FEFO / inclusive DATE / stopped / expired KT05",()=>Checks.Assert(lots.GetSellableLots(product,today).Select(l=>l.MaLo).SequenceEqual(new[]{near,later})));
        checks.Test("SQL transaction overload participates without committing",()=>{
            using var c=db.Open();using var tx=c.BeginTransaction();
            Checks.Assert(lots.GetSellableLots(c,tx,product,today).Count==2);
            using var q=c.CreateCommand();q.Transaction=tx;q.CommandText="SELECT 1";Checks.Assert(Convert.ToInt32(q.ExecuteScalar())==1);tx.Rollback();
        });
        checks.Test("SQL stock reconciles 14 physical / 9 sellable",()=>{var s=inventory.Stock(staff).Single();Checks.Assert(s.TongTon==14&&s.TonCoTheBan==9&&s.TonHetHan==3&&s.TonNgungBan==2);Console.WriteLine($"EVIDENCE product={product}; physical={s.TongTon}; sellable={s.TonCoTheBan}; expired={s.TonHetHan}; stopped={s.TonNgungBan}");});
        checks.Test("SQL staff direct write forbidden",()=>{Checks.Reject(()=>inventory.SetStatus(staff,near,LotStatus.Stopped));Checks.Reject(()=>catalog.SaveProduct(staff,products.List().Single()));Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==9);});
        checks.Test("SQL stop and resume valid lot",()=>{inventory.SetStatus(owner,near,LotStatus.Stopped);Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==5);inventory.SetStatus(owner,near,LotStatus.Selling);Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==9);});
        checks.Test("SQL resume expired lot cannot bypass expiry",()=>{inventory.SetStatus(owner,expired,LotStatus.Selling);Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==9);});
        checks.Test("SQL deactivate product preserves all lots",()=>{catalog.SaveProduct(owner,products.List().Single() with{DangKinhDoanh=false});Checks.Assert(lots.List().Count==4&&lots.GetSellableLots(product,today).Count==0&&inventory.Stock(staff).Single().TonCoTheBan==0);});
        checks.Test("SQL DAL guards inactive receive even without service",()=>Checks.Reject(()=>lots.Receive(new(product,null,today,today,1)),"Product"));
        checks.Test("SQL reactivation restores eligible stock",()=>{catalog.SaveProduct(owner,products.List().Single() with{DangKinhDoanh=true});Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==9);});
        checks.Test("SQL tomorrow excludes today's expiry",()=>Checks.Assert(lots.GetSellableLots(product,today.AddDays(1)).Single().MaLo==later));
        checks.Test("SQL depleted lot excluded",()=>{
            using var c=db.Open();using var q=c.CreateCommand();q.CommandText=$"UPDATE dbo.LoBanh SET SoLuongCon=0 WHERE MaLo={later}";q.ExecuteNonQuery();
            Checks.Assert(lots.GetSellableLots(product,today).Single().MaLo==near);
        });
        checks.Test("SQL deletion of unused category",()=>{int id=catalog.SaveCategory(owner,new(0,"Loại tạm",null));catalog.DeleteCategory(owner,id);Checks.Assert(categories.List().Count==1);});
        checks.Test("SQL missing records report friendly errors",()=>{
            Checks.Reject(()=>categories.Save(new(999,"Không tồn tại",null)));
            Checks.Reject(()=>products.Save(new(999,category,"Không tồn tại","Cái",0,true,null)));
            Checks.Reject(()=>lots.SetStatus(999,LotStatus.Stopped));
        });
        // Restore the documented 14 / 9 / 3 / 2 fixture for manual verification.
        using(var c=db.Open()) { using var q=c.CreateCommand();q.CommandText="UPDATE dbo.LoBanh SET SoLuongCon=SoLuongNhap";q.ExecuteNonQuery(); }
        checks.Test("SQL demo seed is repeatable without resetting stock",()=>{
            string seed=File.ReadAllText(Path.Combine(Path.GetDirectoryName(schema)!,"../seeds/03_CatalogInventory_Demo.sql")).Replace("QuanLyTiemBanh",name,StringComparison.Ordinal);
            using var c=db.Open();
            void Seed(){foreach(string batch in Regex.Split(seed,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase)){
                if(string.IsNullOrWhiteSpace(batch))continue;using var q=c.CreateCommand();q.CommandText=batch;q.ExecuteNonQuery();}}
            Seed();int count=lots.List().Count;
            using(var q=c.CreateCommand()){q.CommandText="UPDATE dbo.LoBanh SET SoLuongCon=3 WHERE MaLo IN (SELECT MIN(MaLo) FROM dbo.LoBanh WHERE NguonNhap=N'DEMO')";q.ExecuteNonQuery();}
            Seed();Checks.Assert(lots.List().Count==count);
            var demo=inventory.Stock(staff,"DEMO - Bánh mì").Single();Checks.Assert(demo.TongTon==13&&demo.TonCoTheBan==8);
        });
        checks.Test("SQL backup and restore independent test copy",()=>{
            string folder=Path.Combine(AppContext.BaseDirectory,"backup-check");Directory.CreateDirectory(folder);
            string backup=Path.Combine(folder,name+".bak"), restored="MocBakery_NhuRestore_"+Guid.NewGuid().ToString("N")[..10];
            string dataLogical,logLogical;
            using(var c=db.Open())
            {
                using(var q=c.CreateCommand()){q.CommandText="SELECT name FROM sys.database_files ORDER BY type";using var r=q.ExecuteReader();r.Read();dataLogical=r.GetString(0);r.Read();logLogical=r.GetString(0);}
                using(var q=c.CreateCommand()){q.CommandText=$"BACKUP DATABASE [{name}] TO DISK=@path";var p=q.CreateParameter();p.ParameterName="@path";p.Value=backup;q.Parameters.Add(p);q.ExecuteNonQuery();}
            }
            using(var c=master.Open())
            {
                using var q=c.CreateCommand();
                q.CommandText=$"RESTORE DATABASE [{restored}] FROM DISK=@backup WITH MOVE @data TO @mdf, MOVE @log TO @ldf";
                foreach(var (key,value) in new[]{("@backup",backup),("@data",dataLogical),("@log",logLogical),("@mdf",Path.Combine(folder,restored+".mdf")),("@ldf",Path.Combine(folder,restored+"_log.ldf"))}){
                    var p=q.CreateParameter();p.ParameterName=key;p.Value=value;q.Parameters.Add(p);}
                q.ExecuteNonQuery();
                cleanup.Add(restored);
            }
            var restoredBuilder=new DbConnectionStringBuilder{ConnectionString=builder.ConnectionString};restoredBuilder["Initial Catalog"]=restored;
            var restoredDb=new DbConnectionFactory(restoredBuilder.ConnectionString);
            Checks.Assert(new LoBanhRepository(restoredDb).List().SequenceEqual(lots.List()));
            Checks.Assert(new SanPhamRepository(restoredDb).List().SequenceEqual(products.List()));
            Console.WriteLine("RESTORED TEST DATABASE: "+restored);
        });
        Console.WriteLine("No business database was modified. Temporary test databases are cleaned unless --keep-database is specified.");
        return checks.Finish();
    }
    private sealed class TemporaryDatabases(DbConnectionFactory master,bool keep):IDisposable
    {
        private readonly List<string> names=[];
        public void Add(string name)=>names.Add(name);
        public void Dispose()
        {
            if(keep)return;
            using var c=master.Open();
            foreach(string name in names.AsEnumerable().Reverse())
            {
                if(!Regex.IsMatch(name,@"^MocBakery_Nhu(Test|Restore)_[a-zA-Z0-9_]+$"))throw new InvalidOperationException("Unexpected test database name");
                using var q=c.CreateCommand();
                q.CommandText=$"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]";q.ExecuteNonQuery();
                Console.WriteLine("CLEANED TEST DATABASE: "+name);
            }
        }
    }
}
