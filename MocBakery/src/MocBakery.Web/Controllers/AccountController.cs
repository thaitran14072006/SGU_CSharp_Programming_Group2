using System.Data.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MocBakery.BLL.Services.CustomerAccounts;
using MocBakery.Models;
using MocBakery.Web.Models;
using MocBakery.Web.Security;

namespace MocBakery.Web.Controllers;

[Route("tai-khoan")]
[RequestSizeLimit(16384)]
[RequestFormLimits(ValueLengthLimit = 1024, ValueCountLimit = 16)]
public sealed class AccountController(CustomerAccountService accounts, ILogger<AccountController> logger) : Controller
{
    [HttpGet("dang-ky")]
    public IActionResult Register() => View(new RegisterForm());

    [HttpPost("dang-ky"), EnableRateLimiting("customer-register")]
    public IActionResult Register(RegisterForm input)
    {
        if (!ModelState.IsValid) return View(input);
        try
        {
            accounts.Register(new(input.Username, input.Password, input.Confirmation, input.FullName, input.Phone, input.Address));
            return RedirectToAction(nameof(Login), new { registered = true });
        }
        catch (BusinessException ex) { ModelState.AddModelError(ex.Field ?? "", ex.Message); }
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        { Unavailable(ex); }
        return View(input);
    }

    [HttpGet("dang-nhap")]
    public IActionResult Login(bool registered = false)
    {
        ViewData["Registered"] = registered;
        return View(new LoginForm());
    }

    [HttpPost("dang-nhap"), EnableRateLimiting("customer-login")]
    public async Task<IActionResult> Login(LoginForm input)
    {
        if (!ModelState.IsValid) return View(input);
        try
        {
            var identity = accounts.Login(input.Username, input.Password);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                CustomerCookie.Principal(identity, input.Username.Trim().ToLowerInvariant()),
                new AuthenticationProperties { IsPersistent = false, AllowRefresh = false });
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex) { ModelState.AddModelError("", ex.Message); }
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        { Unavailable(ex); }
        return View(input);
    }

    [Authorize, HttpGet("")]
    public IActionResult Index() => View(accounts.Profile(CustomerCookie.Identity(User)!));

    [Authorize, HttpGet("ho-so")]
    public IActionResult EditProfile()
    {
        var profile = accounts.Profile(CustomerCookie.Identity(User)!);
        return View(new ProfileForm { FullName = profile.FullName, Phone = profile.Phone, Address = profile.Address });
    }

    [Authorize, HttpPost("ho-so")]
    public async Task<IActionResult> EditProfile(ProfileForm input)
    {
        if (!ModelState.IsValid) return View(input);
        try
        {
            // No customer id or employee fields are accepted from the submitted form.
            accounts.UpdateProfile(CustomerCookie.Identity(User)!, new(input.FullName, input.Phone, input.Address));
            TempData["ProfileSaved"] = true;
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex) when (ex.Field is not null)
        { ModelState.AddModelError(ex.Field, ex.Message); }
        catch (BusinessException)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex) when (ex is DbException or InvalidOperationException)
        { Unavailable(ex); }
        return View(input);
    }

    [Authorize, HttpPost("dang-xuat")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private void Unavailable(Exception ex)
    {
        // Do not expose a connection string, SQL details or entered credentials in the response.
        logger.LogWarning("Customer account database unavailable ({ExceptionType}).", ex.GetType().Name);
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        ModelState.AddModelError("", "Dịch vụ tài khoản tạm thời chưa sẵn sàng. Vui lòng thử lại sau.");
    }
}
