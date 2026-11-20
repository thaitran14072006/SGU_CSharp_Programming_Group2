using System.Data.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MocBakery.Models;
using MocBakery.Web.Admin;
using MocBakery.Web.Models.Admin;
using MocBakery.Web.Security;
namespace MocBakery.Web.Controllers;

[Route("admin"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None),RequestSizeLimit(16384)]
public sealed class AdminAuthController(AdminServices services) : Controller
{
    [AllowAnonymous,HttpGet("dang-nhap")]
    public IActionResult Login(string? returnUrl=null)=>View(new AdminLoginForm{ReturnUrl=returnUrl});
    [AllowAnonymous,HttpPost("dang-nhap"),EnableRateLimiting("employee-login")]
    public async Task<IActionResult> Login(AdminLoginForm input)
    {
        if(ModelState.IsValid)
        try
        {
            var session=services.Login(input.Username,input.Password);
            await HttpContext.SignInAsync(EmployeeCookie.Scheme,EmployeeCookie.Principal(session),new AuthenticationProperties{IsPersistent=false,AllowRefresh=false,ExpiresUtc=DateTimeOffset.UtcNow.AddMinutes(30)});
            if(services.Auth.NeedsPasswordChange(session))return Redirect("/admin/doi-mat-khau");
            return LocalRedirect(Url.IsLocalUrl(input.ReturnUrl)&&input.ReturnUrl!.StartsWith("/admin",StringComparison.Ordinal)?input.ReturnUrl:"/admin");
        }
        catch(BusinessException ex){ModelState.AddModelError("",ex.Message);}
        catch(Exception ex) when(ex is DbException or InvalidOperationException){Response.StatusCode=503;ModelState.AddModelError("","Chưa kết nối được dữ liệu quản trị. Vui lòng thử lại.");}
        return View(input);
    }
    [Authorize(AuthenticationSchemes=EmployeeCookie.Scheme),HttpPost("dang-xuat")]
    public async Task<IActionResult> Logout()
    {services.Logout(EmployeeCookie.Token(User));await HttpContext.SignOutAsync(EmployeeCookie.Scheme);return RedirectToAction(nameof(Login));}
    [Authorize(AuthenticationSchemes=EmployeeCookie.Scheme),HttpGet("doi-mat-khau")]
    public IActionResult Password(){PasswordNavigation();return View(new AdminPasswordForm());}
    [Authorize(AuthenticationSchemes=EmployeeCookie.Scheme),HttpPost("doi-mat-khau"),EnableRateLimiting("employee-login")]
    public async Task<IActionResult> Password(AdminPasswordForm input)
    {
        if(ModelState.IsValid)
        try
        {
            var session=services.Require(EmployeeCookie.Token(User));services.Auth.ChangePassword(session,input.CurrentPassword,input.Password,input.Confirmation);
            services.Logout(session.Token);await HttpContext.SignOutAsync(EmployeeCookie.Scheme);TempData["AdminMessage"]="Đã đổi mật khẩu. Vui lòng đăng nhập lại.";return RedirectToAction(nameof(Login));
        }
        catch(BusinessException ex){ModelState.AddModelError("",ex.Message);}
        PasswordNavigation();return View(input);
    }
    private void PasswordNavigation()=>ViewData["PasswordRequired"]=services.Auth.NeedsPasswordChange(services.Require(EmployeeCookie.Token(User)));
    [Authorize(AuthenticationSchemes=EmployeeCookie.Scheme),HttpGet("khong-co-quyen")]
    public IActionResult Denied(){Response.StatusCode=403;return View();}
}
