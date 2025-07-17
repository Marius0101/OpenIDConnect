using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace OpenIDConnect.Auth.GrantHandlers;

public interface IGrantTypeHandler
{
    Task<IActionResult> HandleAsync(OpenIddictRequest request);
    bool CanHandle(OpenIddictRequest request);
}

