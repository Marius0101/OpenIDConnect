
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using SignInResult = Microsoft.AspNetCore.Mvc.SignInResult;

namespace OpenIDConnect.Auth.GrantHandlers;

public class AuthorizationCodeGrantHandler : IGrantTypeHandler
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly UserManager<IdentityUser> _userManager;
    private const string ScopePrefix = "scp:";
    public AuthorizationCodeGrantHandler(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictTokenManager tokenManager,
        UserManager<IdentityUser> userManager)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _tokenManager = tokenManager;
        _userManager = userManager;
    }
    public bool CanHandle(OpenIddictRequest request) => request.IsAuthorizationCodeGrantType();

    public async Task<IActionResult> HandleAsync(OpenIddictRequest request)
    {
        OpenIddictEntityFrameworkCoreToken? authorizationCodeToken = await _tokenManager.FindByReferenceIdAsync(request.Code)
            as OpenIddictEntityFrameworkCoreToken;
        if (authorizationCodeToken is null)
            return new BadRequestObjectResult(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "Invalid authorization code."
            });
        OpenIddictEntityFrameworkCoreAuthorization? authorization = await _authorizationManager.FindByIdAsync(authorizationCodeToken.Authorization.Id)
        as OpenIddictEntityFrameworkCoreAuthorization;
        if (authorization is null)
            return new BadRequestObjectResult(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "Invalid authorization."
            });
        var properties = authorization.Properties;
        var application = await _applicationManager.FindByClientIdAsync(request.ClientId) as OpenIddictEntityFrameworkCoreApplication;
        if (application is null)
            return new BadRequestObjectResult(new OpenIddictResponse
            {
                Error = Errors.InvalidClient,
                ErrorDescription = "Invalid client."
            });
        var creationDate = await _authorizationManager.GetCreationDateAsync(authorization);
        if (creationDate + TimeSpan.FromMinutes(10) < DateTimeOffset.UtcNow)
        {
            return new BadRequestObjectResult(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The authorization code has expired."
            });
        }
        var clientType = await _applicationManager.GetClientTypeAsync(application);
        if (string.Equals(clientType, OpenIddictConstants.ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase))
        {
            if (!await _applicationManager.ValidateClientSecretAsync(application, request.ClientSecret))
                return new ForbidResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        string? redirectUris = authorization.Application.RedirectUris;
        if (!redirectUris.Contains(request.RedirectUri))
            return new BadRequestObjectResult(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The redirect URI does not match."
            });
        var requestedScopes = await _authorizationManager.GetScopesAsync(authorization);
        var claimsPrincipal  = await CreateClaimsPrincipal(authorization.Subject, requestedScopes.ToList());
        if (claimsPrincipal == null)
        {
            return new BadRequestObjectResult(new OpenIddictResponse
            {
                Error = Errors.InvalidGrant,
                ErrorDescription = "The user associated with this authorization no longer exists."
            });
        }
        await _tokenManager.TryRevokeAsync(authorizationCodeToken);
        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }

    private async Task<ClaimsPrincipal?> CreateClaimsPrincipal(string subject, List<String> grantedScopes)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);
        claimsPrincipal.SetClaim(Claims.Subject, subject);
        var user = await _userManager.FindByIdAsync(subject);
        if (user is null) return null;
        claimsPrincipal.SetClaim(Claims.Name, user.UserName);
        claimsPrincipal.SetScopes(grantedScopes);
        claimsPrincipal.SetDestinations(static claim => claim.Type switch
        {
            Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        });
        return claimsPrincipal;
    } 
}
