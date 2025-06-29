using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace OpenIDConnect.Auth.Pages.connect.authorize;
public class AuthorizeModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {

        if (!User.Identity.IsAuthenticated)
        {
            return Redirect($"/Account/Login{Request.QueryString}");
        }
        return await OnPostAcceptAsync();
    }
    
    public async Task<IActionResult> OnPostAcceptAsync()
    {
        var request = HttpContext.GetOpenIddictServerRequest();

        var claims = new List<Claim>
        {
            new Claim(OpenIddictConstants.Claims.Subject, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""),
        };

        var claimsIdentity = new ClaimsIdentity(claims,
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        claimsPrincipal.SetScopes(request.GetScopes());
        
        return SignIn(claimsPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}