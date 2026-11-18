using MocBakery.BLL.Services.Accounts;
using MocBakery.Models;
using MocBakery.Models.Entities;

internal static class StaffSearchChecks
{
    internal static void Run(Action<string,Action> check,Action<bool> assert)
    {
        var auth=new AuthService(new MemoryAccounts());
        var owner=auth.Login("admin","Owner@123");
        var staff=auth.Login("staff","Staff@123");
        var repository=new SearchRepository();
        var service=new StaffService(repository,auth);
        int[] Ids(string keyword)=>service.Search(owner,keyword).Select(row=>row.MaNhanVien).ToArray();

        check("Staff search matches a displayed code prefix",()=>assert(Ids("NV01").SequenceEqual(new[]{10,11})));
        check("Staff code search trims whitespace and ignores case",()=>assert(Ids(" nv01 ").SequenceEqual(new[]{10,11})));
        check("Staff search still accepts a full displayed code",()=>assert(Ids("NV001").SequenceEqual(new[]{1})));
        check("Staff code search supports IDs with more than three digits",()=>assert(Ids("nv1000").SequenceEqual(new[]{1000})));
        check("Staff name search remains case insensitive",()=>assert(Ids("ANH").SequenceEqual(new[]{1})));
        check("Staff search still finds phone fragments",()=>assert(Ids("1234567").SequenceEqual(new[]{10})));
        check("Empty staff search returns every row including missing phones",()=>assert(Ids(" ").Length==4));
        check("Unmatched staff code returns no results",()=>assert(Ids("NV999").Length==0));
        check("Staff search rejects non-owner before reading repository",()=>
        {
            int reads=repository.Reads;
            try{service.Search(staff,"NV");}
            catch(BusinessException){assert(repository.Reads==reads);return;}
            throw new InvalidOperationException("Expected owner authorization failure.");
        });
    }

    private sealed class SearchRepository:IStaffRepository
    {
        public int Reads{get;private set;}
        public IReadOnlyList<NhanVien> List()
        {
            Reads++;
            return [
                new(1,"Anh","0900000001",Roles.ChuTiem,new(2026,1,1),true),
                new(10,"Bình","0901234567",Roles.NhanVien,new(2026,1,1),true),
                new(11,"Chi",null,Roles.NhanVien,new(2026,1,1),false),
                new(1000,"Dung","0900001000",Roles.NhanVien,new(2026,1,1),true)];
        }
        public int Save(int actorId,NhanVien input)=>throw new NotSupportedException();
        public int SaveAccount(int actorId,int employeeId,int accountId,AccountData input)=>throw new NotSupportedException();
    }
}
