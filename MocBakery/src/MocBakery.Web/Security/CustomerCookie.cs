using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MocBakery.BLL.Services.CustomerAccounts;
using MocBakery.Models;
using MocBakery.Models.CustomerAccounts;

namespace MocBakery.Web.Security;

public static class CustomerCookie
{
    public static ClaimsPrincipal Principal(CustomerIdentity identity, string username) => new(new ClaimsIdentity(
        [new(ClaimTypes.NameIdentifier, identity.AccountId.ToString(CultureInfo.InvariantCulture)),
         new(ClaimTypes.Name, username), new("customer_id", identity.CustomerId.ToString(CultureInfo.InvariantCulture)),
         new("customer_stamp", identity.SecurityStamp.ToString("D"))], CookieAuthenticationDefaults.AuthenticationScheme));

    public static CustomerIdentity? Identity(ClaimsPrincipal principal)
    {
        return principal.Identity?.IsAuthenticated == true &&
            int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out int id) && id > 0 &&
            int.TryParse(principal.FindFirstValue("customer_id"), out int customerId) && customerId > 0 &&
            Guid.TryParse(principal.FindFirstValue("customer_stamp"), out var stamp) && stamp != Guid.Empty
                ? new(id, customerId, stamp) : null;
    }

    public static async Task Validate(CookieValidatePrincipalContext context)
    {
        var identity = Identity(context.Principal!);
        try
        {
            if (identity is null) throw new BusinessException("Invalid customer identity.");
            context.HttpContext.RequestServices.GetRequiredService<CustomerAccountService>().Require(identity);
        }
        catch (BusinessException)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
