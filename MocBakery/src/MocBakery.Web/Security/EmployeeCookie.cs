using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MocBakery.Models;
using MocBakery.Models.DTOs.Accounts;
using MocBakery.Web.Admin;
namespace MocBakery.Web.Security;

public static class EmployeeCookie
{
    public const string Scheme="MocBakery.Employee";
    public static string? Token(ClaimsPrincipal user)=>user.FindFirstValue("employee_session");
    public static ClaimsPrincipal Principal(UserSession session)=>new(new ClaimsIdentity(
        [new Claim("employee_session",session.Token),new Claim(ClaimTypes.Name,session.HoTen),new Claim(ClaimTypes.Role,session.VaiTro)],Scheme));
    public static async Task Validate(CookieValidatePrincipalContext context)
    {
        try
        {
            var session=context.HttpContext.RequestServices.GetRequiredService<AdminServices>().Require(Token(context.Principal!));
            // Roles/password/account state are reloaded for every request, never trusted from a stale cookie.
            context.ReplacePrincipal(Principal(session));
        }
        catch(BusinessException)
        {context.RejectPrincipal();await context.HttpContext.SignOutAsync(Scheme);}
    }
}
