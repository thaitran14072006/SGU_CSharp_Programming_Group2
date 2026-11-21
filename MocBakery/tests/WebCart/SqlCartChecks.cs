using System.Text.RegularExpressions;
using MocBakery.DAL;
using MocBakery.DAL.Repositories.Sales;
using MocBakery.DAL.Repositories.Orders;
using MocBakery.Models;
using MocBakery.Models.WebCart;

internal static class SqlCartChecks
{
    internal static async Task Run()
    {
        string name="MocBakery_CartTest_"+Guid.NewGuid().ToString("N");
        string root=Directory.GetCurrentDirectory();
        while(!File.Exists(Path.Combine(root,"database/schema/07_CustomerCart.sql")))
            root=Directory.GetParent(root)?.FullName??throw new Exception("Run from the MocBakery workspace.");
        string instance=Environment.GetEnvironmentVariable("MOCBAKERY_CART_TEST_SERVER")??@".\SQLEXPRESS";
        string server=$"Server={instance};Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=15;Database=";
        var master=new DbConnectionFactory(server+"master");var db=new DbConnectionFactory(server+name);
        void Sql(DbConnectionFactory factory,string sql){using var c=factory.Open();using var q=c.CreateCommand();q.CommandText=sql;q.ExecuteNonQuery();}
        long Value(string sql){using var c=db.Open();using var q=c.CreateCommand();q.CommandText=sql;return Convert.ToInt64(q.ExecuteScalar());}
        int passed=0;
        void Check(bool ok,string title){if(!ok)throw new Exception(title);passed++;Console.WriteLine("PASS SQL "+title);}
        bool created=false;
        try
        {
            Sql(master,$"CREATE DATABASE [{name}]");created=true;
            string schema=File.ReadAllText(Path.Combine(root,"database/schema/01_CreateDatabase.sql")).Replace("QuanLyTiemBanh",name,StringComparison.Ordinal);
            foreach(string batch in Regex.Split(schema,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
                if(!string.IsNullOrWhiteSpace(batch))Sql(db,batch);
            foreach(string file in new[]{"04_CustomerWebAccounts.sql","05_WebOrders.sql","06_WebOrderContacts.sql","07_CustomerCart.sql"})
                Sql(db,File.ReadAllText(Path.Combine(root,"database/schema",file)));
            Sql(db,File.ReadAllText(Path.Combine(root,"database/schema/07_CustomerCart.sql")));
            Sql(db,"""
                SET IDENTITY_INSERT dbo.KhachHang ON;
                INSERT dbo.KhachHang(MaKhachHang,HoTen,SoDienThoai) VALUES(7,N'Khách A','0901234567'),(8,N'Khách B','0901234568');
                SET IDENTITY_INSERT dbo.KhachHang OFF;
                INSERT dbo.TaiKhoanKhachHang(MaKhachHang,TenDangNhap,MatKhauHash) VALUES(7,'cart.a','fixture'),(8,'cart.b','fixture');
                INSERT dbo.LoaiBanh(TenLoai) VALUES(N'Bánh kiểm thử');
                INSERT dbo.SanPham(MaLoai,TenSanPham,DonViTinh,GiaBan) VALUES(1,N'Bánh kiểm thử',N'Cái',15000);
                INSERT dbo.LoBanh(MaSanPham,NgaySanXuat,HanSuDung,SoLuongNhap,SoLuongCon) VALUES(1,CONVERT(date,GETDATE()),DATEADD(day,2,GETDATE()),10,10);
                """);
            var a=new CustomerCartRepository(db);var b=new CustomerCartRepository(db);
            Check(a.Read(7).Items.Count==0,"new customer has empty cart");
            a.Update(7,_=>[new(1,2)]);
            Check(b.Read(7).Items.Single().Quantity==2,"another repository connection reads same stored cart");
            Check(b.Read(8).Items.Count==0,"different customer is isolated");
            Check(new CustomerCartRepository(new DbConnectionFactory(server+name)).Read(7).Items.Single().Quantity==2,"cart survives repository and connection recreation");
            var old=a.Read(7);
            await Task.WhenAll(Enumerable.Range(0,2).Select(_=>Task.Run(()=>b.Update(7,items=>[new(1,items.Single().Quantity+1)]))));
            Check(a.Read(7).Items.Single().Quantity==4,"parallel browser additions do not lose updates");
            Check(!a.ClearIfVersion(7,old.Version)&&a.Read(7).Items.Count==1,"stale clear preserves newer edits");
            try{a.Update(7,_=>[new(1,0)]);throw new Exception("invalid quantity accepted");}catch(BusinessException){}
            Check(a.Read(7).Items.Single().Quantity==4,"failed cart mutation rolls back");
            var orders=new WebOrderRepository(db);
            var ready=a.Update(7,_=>[new(1,1)]);
            WebPurchaseRequest Request(long version,Guid key)=>new(7,key,[new(1,1)],15000,DateTime.Now.AddDays(1),null,[new(1,"Bánh kiểm thử",1,15000,true)],new("Khách A","0901234567","test@example.com"),version);
            Guid key=Guid.NewGuid();int id=orders.Submit(Request(ready.Version,key));
            Check(id>0&&a.Read(7).Items.Count==0,"order and shared cart clearing commit together");
            a.Update(7,_=>[new(1,2)]);
            Check(orders.Submit(Request(ready.Version,key))==id&&a.Read(7).Items.Single().Quantity==2,"order replay preserves newly added cart");
            var before=a.Update(7,_=>[new(1,1)]);
            Sql(db,"CREATE TRIGGER CartTestFail ON dbo.ChiTietDonDat AFTER INSERT AS THROW 51001,'cart rollback fixture',1;");
            try{orders.Submit(Request(before.Version,Guid.NewGuid()));throw new Exception("trigger did not reject");}
            catch(System.Data.Common.DbException){}
            Sql(db,"DROP TRIGGER CartTestFail");
            Check(a.Read(7).Version==before.Version&&a.Read(7).Items.Count==1&&Value("SELECT COUNT(*) FROM dbo.DonDatBanh")==1,"failed order preserves cart and rolls back order");
            async Task<bool> Submit()=>await Task.Run(()=>{try{orders.Submit(Request(before.Version,Guid.NewGuid()));return true;}catch(BusinessException){return false;}});
            var results=await Task.WhenAll(Submit(),Submit());
            Check(results.Count(x=>x)==1,"two browsers submitting same cart create only one order");
            Check(Value("SELECT COUNT(*) FROM dbo.HoaDon")==0&&Value("SELECT SoLuongCon FROM dbo.LoBanh WHERE MaLo=1")==10,"saving cart and submitting order do not invoice or deduct stock");
            Sql(db,"UPDATE dbo.TaiKhoanKhachHang SET TrangThai=0 WHERE MaKhachHang=8");
            try{b.Read(8);throw new Exception("disabled account accepted");}catch(BusinessException){}
            Check(true,"disabled customer cannot access stored cart");
            Console.WriteLine($"{passed} SQL cart checks passed; disposable database only.");
        }
        finally
        {
            if(created)Sql(master,$"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");
        }
    }
}
