using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Catalog;
using MocBakery.BLL.Services.Inventory;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Inventory;

if(args.Contains("--excel")) { Environment.ExitCode=ExcelDesignChecks.Run(args); return; }
if(args.Contains("--integration")) { Environment.ExitCode=IntegrationChecks.Run(args); return; }
var checks=new Checks();var data=new MemoryCatalog();var accounts=new TestAccounts();var auth=new AuthService(accounts);
var owner=auth.Login("owner","Password123");var staff=auth.Login("staff","Password123");
var catalog=new CatalogService(data,data,auth);
var day=new DateOnly(2026,11,14);var inventory=new InventoryService(data,data,auth,()=>day);
int category=catalog.SaveCategory(owner,new(0,"  Bánh   ngọt ",null));
int product=catalog.SaveProduct(owner,new(0,category,"Bánh kem","Cái",25000,true,null));
checks.Test("Category normalizes whitespace",()=>Checks.Assert(catalog.Categories(staff).Single().TenLoai=="Bánh ngọt"));
checks.Test("Blank category rejected",()=>Checks.Reject(()=>catalog.SaveCategory(owner,new(0,"  ",null)),"Name"));
checks.Test("Duplicate category rejected",()=>Checks.Reject(()=>catalog.SaveCategory(owner,new(0,"BÁNH NGỌT",null)),"Name"));
checks.Test("Long category description rejected",()=>Checks.Reject(()=>catalog.SaveCategory(owner,new(0,"Khác",new string('x',256))),"Note"));
checks.Test("Used category cannot be deleted",()=>Checks.Reject(()=>catalog.DeleteCategory(owner,category)));
checks.Test("Unused category can be deleted",()=>{int id=catalog.SaveCategory(owner,new(0,"Tạm",null));catalog.DeleteCategory(owner,id);Checks.Assert(catalog.Categories(owner).Count==1);});
checks.Test("Staff cannot add category",()=>Checks.Reject(()=>catalog.SaveCategory(staff,new(0,"Khác",null))));
checks.Test("Staff cannot delete category",()=>Checks.Reject(()=>catalog.DeleteCategory(staff,category)));
checks.Test("Staff cannot edit product",()=>Checks.Reject(()=>catalog.SaveProduct(staff,data.Products[0])));
checks.Test("Negative price rejected KT04",()=>Checks.Reject(()=>catalog.SaveProduct(owner,data.Products[0] with{GiaBan=-1}),"Price"));
checks.Test("Price with excess decimals rejected",()=>Checks.Reject(()=>catalog.SaveProduct(owner,data.Products[0] with{GiaBan=1.001m}),"Price"));
checks.Test("Oversize price rejected",()=>Checks.Reject(()=>catalog.SaveProduct(owner,data.Products[0] with{GiaBan=10000000000000000m}),"Price"));
checks.Test("Zero price rejected by Excel screen rule",()=>Checks.Reject(()=>catalog.SaveProduct(owner,data.Products[0] with{GiaBan=0}),"Price"));
checks.Test("Missing category rejected",()=>Checks.Reject(()=>catalog.SaveProduct(owner,data.Products[0] with{MaLoai=999}),"Category"));
checks.Test("Duplicate product rejected",()=>Checks.Reject(()=>catalog.SaveProduct(owner,data.Products[0] with{MaSanPham=0}),"Name"));
checks.Test("Empty unit rejected",()=>Checks.Reject(()=>catalog.SaveProduct(owner,data.Products[0] with{DonViTinh=" "}),"Unit"));
checks.Test("Product filter works",()=>Checks.Assert(catalog.Products(staff,"kem",category,true).Count==1&&catalog.Products(staff,active:false).Count==0));
checks.Test("Staff cannot receive lot",()=>Checks.Reject(()=>inventory.Receive(staff,new(product,null,day,day,4))));
checks.Test("Zero quantity rejected",()=>Checks.Reject(()=>inventory.Receive(owner,new(product,null,day,day,0)),"Quantity"));
checks.Test("Negative quantity rejected",()=>Checks.Reject(()=>inventory.Receive(owner,new(product,null,day,day,-1)),"Quantity"));
checks.Test("Expiry before manufacture rejected KT04",()=>Checks.Reject(()=>inventory.Receive(owner,new(product,null,day,day.AddDays(-1),4)),"Expiry"));
checks.Test("Future manufacture rejected",()=>Checks.Reject(()=>inventory.Receive(owner,new(product,null,day.AddDays(1),day.AddDays(2),4)),"Produced"));
checks.Test("Missing product rejected",()=>Checks.Reject(()=>inventory.Receive(owner,new(999,null,day,day,4)),"Product"));
checks.Test("Long source rejected",()=>Checks.Reject(()=>inventory.Receive(owner,new(product,new string('x',101),day,day,4)),"Source"));
int later=inventory.Receive(owner,new(product,"Bếp",day.AddDays(-1),day.AddDays(2),5));
int near=inventory.Receive(owner,new(product,"Bếp",day.AddDays(-1),day,4));
int expired=inventory.Receive(owner,new(product,null,day.AddDays(-3),day.AddDays(-1),3));
int stopped=inventory.Receive(owner,new(product,null,day,day.AddDays(1),2));inventory.SetStatus(owner,stopped,LotStatus.Stopped);
checks.Test("Received lot starts with full remaining quantity",()=>Checks.Assert(data.Lots.All(l=>l.SoLuongCon==l.SoLuongNhap)));
checks.Test("FEFO sorts earliest expiry before creation ID",()=>Checks.Assert(inventory.SellableLots(staff,product).Select(l=>l.MaLo).SequenceEqual(new[]{near,later})));
checks.Test("Expiry date remains sellable KT05 boundary",()=>Checks.Assert(inventory.SellableLots(staff,product).Any(l=>l.MaLo==near)));
checks.Test("Expired and stopped lots excluded KT05",()=>Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==9));
checks.Test("Stock reconciles physical 14 / expired 3 / stopped 2",()=>{var s=inventory.Stock(staff).Single();Checks.Assert(s.TongTon==14&&s.TonHetHan==3&&s.TonNgungBan==2);});
checks.Test("Lot filters return expired and stopped",()=>Checks.Assert(inventory.Lots(staff,filter:"Expired").Single().MaLo==expired&&inventory.Lots(staff,filter:"Stopped").Single().MaLo==stopped));
checks.Test("Staff cannot change lot state",()=>Checks.Reject(()=>inventory.SetStatus(staff,near,LotStatus.Stopped)));
checks.Test("Unknown lot state rejected",()=>Checks.Reject(()=>inventory.SetStatus(owner,near,"Other")));
checks.Test("Empty lot selection rejected",()=>Checks.Reject(()=>inventory.SetStatus(owner,0,LotStatus.Stopped)));
checks.Test("Stopping lot excludes stock immediately",()=>{inventory.SetStatus(owner,near,LotStatus.Stopped);Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==5);inventory.SetStatus(owner,near,LotStatus.Selling);});
checks.Test("Re-enabling expired lot cannot make it sellable",()=>{inventory.SetStatus(owner,expired,LotStatus.Selling);Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==9);});
checks.Test("Deactivation preserves history and excludes all lots",()=>{catalog.SaveProduct(owner,data.Products[0] with{DangKinhDoanh=false});Checks.Assert(data.Lots.Count==4&&inventory.Stock(staff).Single().TonCoTheBan==0&&inventory.SellableLots(staff,product).Count==0);});
checks.Test("Inactive product cannot receive lots",()=>Checks.Reject(()=>inventory.Receive(owner,new(product,null,day,day,1)),"Product"));
catalog.SaveProduct(owner,data.Products[0] with{DangKinhDoanh=true});
checks.Test("Next business day changes expiry calculation",()=>{day=day.AddDays(1);Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==5);});
checks.Test("Zero remaining stock excluded",()=>{int i=data.Lots.FindIndex(l=>l.MaLo==later);data.Lots[i]=data.Lots[i] with{SoLuongCon=0};Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==0);});
checks.Test("Total stock uses 64-bit sum",()=>{data.Lots.Add(new(100,product,DateTime.Now,null,day,day, int.MaxValue,int.MaxValue,LotStatus.Selling));data.Lots.Add(new(101,product,DateTime.Now,null,day,day,int.MaxValue,int.MaxValue,LotStatus.Selling));Checks.Assert(inventory.Stock(staff).Single().TonCoTheBan==2L*int.MaxValue);});
checks.Test("Forged role does not grant management rights",()=>Checks.Reject(()=>catalog.SaveCategory(staff with{VaiTro=Roles.ChuTiem},new(0,"Khác",null))));
checks.Test("Revoked owner role checked in BLL",()=>{accounts.Rows[0]=accounts.Rows[0] with{VaiTro=Roles.NhanVien};Checks.Reject(()=>inventory.SetStatus(owner,near,LotStatus.Stopped));});
checks.Test("Revoked session cannot query inventory",()=>{auth.Logout(staff);Checks.Reject(()=>inventory.Stock(staff));});
Environment.ExitCode=checks.Finish();

internal sealed class Checks
{
    private int passed,failed;
    public void Test(string name,Action action){try{action();Console.WriteLine("PASS "+name);passed++;}catch(Exception ex){Console.WriteLine("FAIL "+name+": "+ex.Message);failed++;}}
    public static void Assert(bool value){if(!value)throw new Exception("Assertion failed");}
    public static void Reject(Action action,string? field=null){try{action();}catch(BusinessException ex){if(field is not null)Assert(ex.Field==field);return;}throw new Exception("Expected BusinessException");}
    public int Finish(){Console.WriteLine($"RESULT: {passed} passed, {failed} failed");return failed==0?0:1;}
}
internal sealed class TestAccounts:IAccountRepository
{
    public List<TaiKhoan> Rows=[new(1,"owner",PasswordHasher.Hash("Password123"),"Chủ thử",Roles.ChuTiem,true,DateTime.Now),new(2,"staff",PasswordHasher.Hash("Password123"),"Nhân viên thử",Roles.NhanVien,true,DateTime.Now)];
    public TaiKhoan? FindByUsername(string name)=>Rows.Find(x=>x.TenDangNhap==name);
    public TaiKhoan? FindById(int id)=>Rows.Find(x=>x.MaTaiKhoan==id);
    public IReadOnlyList<TaiKhoan> List()=>Rows;
    public int Create(AccountData input)=>throw new NotSupportedException();
    public void Update(int actorId,int id,AccountData input)=>throw new NotSupportedException();
    public bool ChangePassword(int id,string hash,string next,bool requireActive=true)=>throw new NotSupportedException();
}
internal sealed class MemoryCatalog:ILoaiBanhRepository,ISanPhamRepository,ILoBanhRepository
{
    public List<LoaiBanh> Categories=[]; public List<SanPham> Products=[]; public List<LoBanh> Lots=[];
    IReadOnlyList<LoaiBanh> ILoaiBanhRepository.List()=>Categories.ToList();
    IReadOnlyList<SanPham> ISanPhamRepository.List()=>Products.ToList();
    IReadOnlyList<LoBanh> ILoBanhRepository.List()=>Lots.ToList();
    public int Save(LoaiBanh input){if(input.MaLoai==0){input=input with{MaLoai=Categories.Count==0?1:Categories.Max(x=>x.MaLoai)+1};Categories.Add(input);}else{int index=Categories.FindIndex(x=>x.MaLoai==input.MaLoai);if(index<0)throw new BusinessException("Missing");Categories[index]=input;}return input.MaLoai;}
    public int Save(SanPham input){if(input.MaSanPham==0){input=input with{MaSanPham=Products.Count+1};Products.Add(input);}else{int i=Products.FindIndex(x=>x.MaSanPham==input.MaSanPham);if(i<0)throw new BusinessException("Missing");Products[i]=input;}return input.MaSanPham;}
    public void Delete(int id){if(Products.Any(p=>p.MaLoai==id))throw new BusinessException("Used category");if(Categories.RemoveAll(c=>c.MaLoai==id)!=1)throw new BusinessException("Missing");}
    public int Receive(LotInput input){int id=Lots.Count+1;Lots.Add(new(id,input.MaSanPham,DateTime.Now,input.NguonNhap,input.NgaySanXuat,input.HanSuDung,input.SoLuongNhap,input.SoLuongNhap,LotStatus.Selling));return id;}
    public void SetStatus(int id,string status){int i=Lots.FindIndex(l=>l.MaLo==id);if(i<0)throw new BusinessException("Missing");Lots[i]=Lots[i] with{TinhTrang=status};}
    public IReadOnlyList<LoBanh> GetSellableLots(int id,DateOnly date)=>Lots.Where(l=>l.MaSanPham==id&&InventoryRules.CanSell(l,Products.Single(p=>p.MaSanPham==id),date)).OrderBy(l=>l.HanSuDung).ThenBy(l=>l.MaLo).ToList();
}
