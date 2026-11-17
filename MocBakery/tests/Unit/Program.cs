using MocBakery.BLL.Services.Accounts;
using MocBakery.BLL.Services.Customers;
using MocBakery.BLL.Services.Sales;
using MocBakery.Models;
using MocBakery.Models.Entities;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.DTOs.Customers;

int passed=0,failed=0;
void Check(string title,Action action)
{
    try{action();Console.WriteLine("PASS "+title);passed++;}
    catch(Exception e){Console.WriteLine("FAIL "+title+": "+e.Message);failed++;}
}
void Assert(bool ok){if(!ok)throw new Exception("Assertion failed");}
void Reject(Action action,string? field=null)
{
    try{action();}catch(BusinessException e){if(field!=null)Assert(e.Field==field);return;}
    throw new Exception("Expected BusinessException");
}
LoginPreferencesChecks.Run(Check, Assert);
var repo=new MemoryAccounts();var customers=new MemoryCustomers();
var auth=new AuthService(repo);var service=new AccountService(repo,auth);var cs=new CustomerService(customers,auth);
var owner=auth.Login("admin","Owner@123");
var staff=auth.Login("staff","Staff@123");
Check("Hash uses random salt",()=>Assert(PasswordHasher.Hash("same")!=PasswordHasher.Hash("same")));
Check("Hash accepts correct and rejects incorrect password",()=>{string h=PasswordHasher.Hash("abc");Assert(PasswordHasher.Verify("abc",h)&&!PasswordHasher.Verify("ABC",h));});
Check("Malformed hash rejected",()=>Assert(!PasswordHasher.Verify("abc","<HASH_ADMIN>")&&!PasswordHasher.Verify("abc","PBKDF2-SHA256$999999999$x$x")));
Check("Login blank username",()=>Reject(()=>auth.Login(" ","x"),"Username"));
Check("Login blank password",()=>Reject(()=>auth.Login("admin",""),"Password"));
Check("Login wrong password",()=>Reject(()=>auth.Login("admin","bad")));
Check("Login nonexistent username",()=>Reject(()=>auth.Login("unknown","bad")));
Check("Locked account cannot login",()=>Reject(()=>auth.Login("locked","Staff@123")));
Check("Login trims username",()=>Assert(auth.Login(" admin ","Owner@123").MaTaiKhoan==1));
Check("Staff cannot list accounts",()=>Reject(()=>service.List(staff)));
Check("Forged owner data does not elevate staff",()=>Reject(()=>service.List(staff with {MaTaiKhoan=1,VaiTro=Roles.ChuTiem})));
Check("Unissued token rejected",()=>Reject(()=>service.List(owner with {Token="fake"})));
Check("Owner can create staff",()=>{int id=service.Save(owner,0,new("newstaff","Người dùng mới",Roles.NhanVien,true,"Valid@123"));Assert(id>0&&auth.Login("newstaff","Valid@123").VaiTro==Roles.NhanVien);});
Check("Duplicate username case insensitive",()=>Reject(()=>service.Save(owner,0,new("STAFF","Trùng",Roles.NhanVien,true,"Valid@123")),"Username"));
Check("Reject invalid role",()=>Reject(()=>service.Save(owner,0,new("other","Tên","KhongHopLe",true,"Valid@123")),"Role"));
Check("Reject blank account full name",()=>Reject(()=>service.Save(owner,0,new("other"," ",Roles.NhanVien,true,"Valid@123")),"FullName"));
Check("Reject short password",()=>Reject(()=>service.Save(owner,0,new("other","Tên",Roles.NhanVien,true,"short")),"Password"));
Check("Staff cannot create owner",()=>Reject(()=>service.Save(staff,0,new("evil","Tên",Roles.ChuTiem,true,"Valid@123"))));
Check("Owner cannot self-lock",()=>Reject(()=>service.Save(owner,1,new("admin","Chủ",Roles.ChuTiem,false,"")),"Active"));
Check("Owner cannot self-demote",()=>Reject(()=>service.Save(owner,1,new("admin","Chủ",Roles.NhanVien,true,"")),"Active"));
Check("Update staff name",()=>{service.Save(owner,2,new("staff","Tên đã sửa",Roles.NhanVien,true,""));Assert(repo.FindById(2)!.HoTen=="Tên đã sửa");});
Check("Username immutable after creation",()=>Reject(()=>service.Save(owner,2,new("renamed","Tên",Roles.NhanVien,true,"")),"Username"));
Check("Staff cannot reset password",()=>Reject(()=>service.ResetPassword(staff,1,"Valid@123")));
Check("Wrong old password leaves existing hash",()=>{string before=repo.FindById(2)!.MatKhauHash;Reject(()=>auth.ChangePassword(staff,"bad","NewPass@123","NewPass@123"),"OldPassword");Assert(before==repo.FindById(2)!.MatKhauHash);});
Check("Password confirmation mismatch",()=>Reject(()=>auth.ChangePassword(staff,"Staff@123","NewPass@123","mismatch"),"Confirmation"));
Check("Same password rejected",()=>Reject(()=>auth.ChangePassword(staff,"Staff@123","Staff@123","Staff@123"),"Password"));
Check("Change password requires signing in again",()=>{auth.ChangePassword(staff,"Staff@123","NewPass@123","NewPass@123");Reject(()=>auth.Require(staff));Reject(()=>auth.Login("staff","Staff@123"));staff=auth.Login("staff","NewPass@123");Assert(staff.MaTaiKhoan==2);});
Check("Owner reset invalidates existing staff sessions",()=>{service.ResetPassword(owner,2,"ResetPass@123");Reject(()=>cs.Search(staff,""));staff=auth.Login("staff","ResetPass@123");});
Check("Customer add normalizes name and preserves leading zero",()=>{int id=cs.Save(staff,new(0,"  Nguyễn   Văn An ","0901112233","  TP.HCM  "," Ghi chú "));var c=customers.Rows.Single(x=>x.MaKhachHang==id);Assert(c.HoTen=="Nguyễn Văn An"&&c.SoDienThoai=="0901112233"&&c.DiaChi=="TP.HCM");});
Check("Customer duplicate phone rejected",()=>Reject(()=>cs.Save(staff,new(0,"Trùng","0901112233",null,null)),"Phone"));
Check("Customer edit can keep same phone",()=>{var c=customers.Rows.Single();cs.Save(staff,c with {HoTen="Đã sửa"});Assert(customers.Rows.Single().HoTen=="Đã sửa");});
Check("Customer blank name rejected",()=>Reject(()=>cs.Save(staff,new(0," ","0909999999",null,null)),"FullName"));
Check("Customer short phone rejected",()=>Reject(()=>cs.Save(staff,new(0,"Tên","123",null,null)),"Phone"));
Check("Customer non-digit phone rejected",()=>Reject(()=>cs.Save(staff,new(0,"Tên","09011122ab",null,null)),"Phone"));
Check("Customer long address rejected",()=>Reject(()=>cs.Save(staff,new(0,"Tên","0909999999",new string('a',256),null)),"Address"));
Check("Customer long note rejected",()=>Reject(()=>cs.Save(staff,new(0,"Tên","0909999999",null,new string('a',256))),"Note"));
Check("Customer search finds by phone",()=>Assert(cs.Search(staff,"090111").Count==1));
Check("History requires selection",()=>Reject(()=>cs.History(staff,0)));
Check("Empty invoice history is empty",()=>Assert(cs.History(staff,1).Count==0));
Check("Locking an account rejects its next operation",()=>{service.Save(owner,2,new("staff","Tên",Roles.NhanVien,false,""));Reject(()=>cs.Search(staff,""));});
Check("Revoked role is checked again by BLL",()=>{int id=service.Save(owner,0,new("owner2","Chủ 2",Roles.ChuTiem,true,"OwnerTwo@123"));var s=auth.Login("owner2","OwnerTwo@123");service.Save(owner,id,new("owner2","Chủ 2",Roles.NhanVien,true,""));Reject(()=>service.List(s));});
Check("Owner can reset locked account without unlocking it",()=>{service.ResetPassword(owner,3,"ResetLocked@123");Assert(PasswordHasher.Verify("ResetLocked@123",repo.FindById(3)!.MatKhauHash));Reject(()=>auth.Login("locked","ResetLocked@123"));});
Check("Logout invalidates session",()=>{var s=auth.Login("admin","Owner@123");auth.Logout(s);Reject(()=>cs.Search(s,""));});
var near = new LoBanh(101, 1, new DateTime(2026,11,15), null, new(2026,11,15), new(2026,11,16), 4, 4, LotStatus.Selling);
var later = near with { MaLo = 102, HanSuDung = new(2026,11,18), SoLuongNhap = 5, SoLuongCon = 5 };
Check("KT06: FEFO allocates 4 + 2", () =>
{
    var lines = SalesService.Allocate([later, near], 6);
    Assert(lines.Select(line => (line.LotId, line.Quantity)).SequenceEqual(new[] { (101, 4), (102, 2) }));
});
Check("KT07: FEFO rejects insufficient stock", () => Reject(() => SalesService.Allocate([near, later], 10)));
Console.WriteLine($"RESULT: {passed} passed, {failed} failed");
Environment.ExitCode=failed==0?0:1;

sealed class MemoryAccounts:IAccountRepository
{
    public readonly List<TaiKhoan> Rows=[
        new(1,"admin",PasswordHasher.Hash("Owner@123"),"Chủ tiệm",Roles.ChuTiem,true,DateTime.Now),
        new(2,"staff",PasswordHasher.Hash("Staff@123"),"Nhân viên",Roles.NhanVien,true,DateTime.Now),
        new(3,"locked",PasswordHasher.Hash("Staff@123"),"Khóa",Roles.NhanVien,false,DateTime.Now)];
    public TaiKhoan? FindByUsername(string s)=>Rows.Find(a=>a.TenDangNhap.Equals(s,StringComparison.OrdinalIgnoreCase));
    public TaiKhoan? FindById(int id)=>Rows.Find(a=>a.MaTaiKhoan==id);
    public IReadOnlyList<TaiKhoan> List()=>Rows.ToList();
    public int Create(AccountData a){int id=Rows.Max(x=>x.MaTaiKhoan)+1;Rows.Add(new(id,a.Username,a.Hash,a.FullName,a.Role,a.Active,DateTime.Now));return id;}
    public void Update(int actor,int id,AccountData a){int i=Rows.FindIndex(x=>x.MaTaiKhoan==id);Rows[i]=Rows[i] with{HoTen=a.FullName,VaiTro=a.Role,TrangThai=a.Active};}
    public bool ChangePassword(int id,string old,string next,bool requireActive=true){int i=Rows.FindIndex(x=>x.MaTaiKhoan==id&&x.MatKhauHash==old&&(!requireActive||x.TrangThai));if(i<0)return false;Rows[i]=Rows[i] with{MatKhauHash=next};return true;}
}
sealed class MemoryCustomers:ICustomerRepository
{
    public readonly List<KhachHang> Rows=[];
    public IReadOnlyList<KhachHang> Search(string key)=>Rows.Where(c=>c.HoTen.Contains(key,StringComparison.OrdinalIgnoreCase)||c.SoDienThoai.Contains(key)).ToList();
    public int Save(KhachHang c){if(c.MaKhachHang==0){c=c with{MaKhachHang=Rows.Count+1};Rows.Add(c);}else{int i=Rows.FindIndex(x=>x.MaKhachHang==c.MaKhachHang);Rows[i]=c;}return c.MaKhachHang;}
    public IReadOnlyList<InvoiceSummary> History(int id)=>[];
}
