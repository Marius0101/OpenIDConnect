using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenIDConnect.GrantHandlers;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
namespace OpenIDConnect.Controllers;

[ApiController]
public class TokenController : ControllerBase
{
    private readonly IEnumerable<IGrantTypeHandler> _grantHandlers;
    public TokenController(IEnumerable<IGrantTypeHandler> grantHandlers)
    {
        _grantHandlers = grantHandlers;
    }

    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        if(request is null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.InvalidRequest,
                ErrorDescription = "The OpenID Connect request cannot be retrieved."
            });
        }
        var handler = _grantHandlers.FirstOrDefault(h => h.CanHandle(request));
        if (handler is null)
        {
            return BadRequest(new OpenIddictResponse
            {
                Error = Errors.UnsupportedGrantType,
                ErrorDescription = "The specified grant type is not supported."
            });
        }

        return await handler.HandleAsync(request);
    }

}
