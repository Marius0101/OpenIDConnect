using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIDConnect.GrantHandlers;

public class ClientCredentialsGrantHandler : IGrantTypeHandler
{
    private readonly OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> _applicationManager;
    private const string ScopePrefix = "scp:";
    public ClientCredentialsGrantHandler(OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication> applicationManager)
    {
        _applicationManager = applicationManager;
    }
    public bool CanHandle(OpenIddictRequest request) => request.IsClientCredentialsGrantType();

    public async Task<IActionResult> HandleAsync(OpenIddictRequest request)
    {
        OpenIddictEntityFrameworkCoreApplication? application = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("The application cannot be found.");
        var (validScopes, invalidScopes) = await ValidateScope(request, application);
        if (invalidScopes.Any() )
        {
            return new JsonResult(new OpenIddictResponse
            {
                Error = Errors.InvalidScope,
                ErrorDescription = "The specified 'scope' is invalid."
            })
            {
                StatusCode = 400 
            };
        }
        if (validScopes.Count == 0)
        {
            return new JsonResult(new OpenIddictResponse
            {
                Error = Errors.InvalidScope,
                ErrorDescription = "The 'scope' parameter must not be empty."
            })
            {
                StatusCode = 400
            };
        }
        
        ClaimsPrincipal claimsPrincipal = await CreateClaimsPrincipal(request, application, validScopes);
        
        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }

    private async Task<ClaimsPrincipal> CreateClaimsPrincipal(OpenIddictRequest request, OpenIddictEntityFrameworkCoreApplication application, List<String> grantedScopes)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);
        claimsPrincipal.SetClaim(Claims.Subject, request.ClientId);
        claimsPrincipal.SetClaim(Claims.Name, await _applicationManager.GetDisplayNameAsync(application));
        claimsPrincipal.SetScopes(grantedScopes);
        claimsPrincipal.SetDestinations(static claim => claim.Type switch
        {
            Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });
        return claimsPrincipal;
    }

    public async Task<(List<string> ValidScopes, List<string> InvalidScopes)> ValidateScope(OpenIddictRequest request, OpenIddictEntityFrameworkCoreApplication application)
    {
        var requestedScopes = request.GetScopes();
        var permissions  =  await _applicationManager.GetPermissionsAsync(application);
        var allowedScopes = permissions
            .Where(p => p.StartsWith(ScopePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(p => p[ScopePrefix.Length..])
            .ToList();
        var invalidScopes = requestedScopes.Except(allowedScopes).ToList();
        var validScopes = requestedScopes.Intersect(allowedScopes).ToList();
        return (validScopes, invalidScopes);
    }
}
