using MocBakery.GUI;
using MocBakery.GUI.Forms.Accounts;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Models.Entities;

namespace MocBakery.GuiChecks;

// This mode checks local controls only; it never authenticates or opens a database.
internal static class PasswordVisibilityChecks
{
    internal static int Run()
    {
        var services=new AppServices("Server=(localdb)\\MSSQLLocalDB;Database=NotOpened;Integrated Security=True");
        var session=new UserSession("layout-only",0,"layout","Layout",Roles.ChuTiem);
        using var form=new ChangePasswordForm(services,session);
        var inputs=new[]{"CurrentPassword","NewPassword","ConfirmPassword"}
            .Select(name=>(TextBox)form.Controls.Find(name,true).Single()).ToArray();
        var show=(CheckBox)form.Controls.Find("ShowPasswords",true).Single();
        int passed=0,failed=0;
        static void Assert(bool condition){if(!condition)throw new InvalidOperationException("Assertion failed.");}
        void Check(string title,Action action)
        {
            try{action();Console.WriteLine("PASS "+title);passed++;}
            catch(Exception ex){Console.WriteLine("FAIL "+title+": "+ex.Message);failed++;}
        }

        Check("Change password masks all three inputs by default",()=>
            Assert(!show.Checked&&inputs.All(input=>input.UseSystemPasswordChar)));
        string[] values=["Current sample 123!","New sample 456!","New sample 456!"];
        for(int i=0;i<inputs.Length;i++)inputs[i].Text=values[i];
        Check("Show password reveals all three inputs",()=>
        {
            show.Checked=true;
            Assert(inputs.All(input=>!input.UseSystemPasswordChar));
        });
        Check("Showing passwords preserves current new and confirmation text",()=>
            Assert(inputs.Select(input=>input.Text).SequenceEqual(values)));
        Check("Hiding passwords preserves input values",()=>
        {
            show.Checked=false;
            Assert(inputs.All(input=>input.UseSystemPasswordChar)&&inputs.Select(input=>input.Text).SequenceEqual(values));
        });
        Check("Repeated visibility changes do not clear or modify text",()=>
        {
            for(int i=0;i<4;i++)show.Checked=!show.Checked;
            Assert(inputs.All(input=>input.UseSystemPasswordChar)&&inputs.Select(input=>input.Text).SequenceEqual(values));
        });
        Check("Visibility checkbox leaves password limits and default actions intact",()=>
            Assert(inputs.All(input=>input.MaxLength==64)&&form.AcceptButton is Button&&form.CancelButton is Button));
        Console.WriteLine($"PASSWORD VISIBILITY RESULT: {passed} passed, {failed} failed; no database operations.");
        return failed==0?0:1;
    }
}
