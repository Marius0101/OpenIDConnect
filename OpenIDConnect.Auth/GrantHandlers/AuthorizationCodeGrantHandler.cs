
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

namespace OpenIDConnect.GrantHandlers;

public class AuthorizationCodeGrantHandler : IGrantTypeHandler
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly UserManager<IdentityUser> _userManager;
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
    #region public methods
    public bool CanHandle(OpenIddictRequest request) => request.IsAuthorizationCodeGrantType();

    public async Task<IActionResult> HandleAsync(OpenIddictRequest request)
    {
        var token = await GetAuthorizationCodeTokenAsync(request);
        if (token is null)
            return CreateErrorResponse(Errors.InvalidGrant, "Invalid authorization code.");

        var authorization = await GetAuthorizationAsync(token);
        if (authorization is null)
            return CreateErrorResponse(Errors.InvalidGrant, "Invalid authorization.");

        var application = await GetApplicationAsync(request);
        if (application is null)
            return CreateErrorResponse(Errors.InvalidClient, "Invalid client.");

        if (!await IsClientSecretValid(application, request))
        {
            return new ForbidResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        if (await IsAuthorizationCodeExpired(authorization))
            return CreateErrorResponse(Errors.InvalidGrant, "The authorization code has expired.");

        if (!IsRedirectUriValid(authorization, request))
            return CreateErrorResponse(Errors.InvalidGrant, "The redirect URI does not match.");

        var requestedScopes = await _authorizationManager.GetScopesAsync(authorization);
        var claimsPrincipal = await CreateClaimsPrincipal(authorization, requestedScopes.ToList());
        if (claimsPrincipal == null)
            return CreateErrorResponse(Errors.InvalidGrant, "The user associated with this authorization no longer exists.");
        await _tokenManager.TryRevokeAsync(token);
        return new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, claimsPrincipal);
    }
    #endregion
    #region private methods
    private async Task<OpenIddictEntityFrameworkCoreToken?> GetAuthorizationCodeTokenAsync(OpenIddictRequest request)
    {
        if (string.IsNullOrEmpty(request.Code))
            return null;
        return await _tokenManager.FindByReferenceIdAsync(request.Code) as OpenIddictEntityFrameworkCoreToken;
    }
    private async Task<OpenIddictEntityFrameworkCoreAuthorization?> GetAuthorizationAsync(OpenIddictEntityFrameworkCoreToken token)
    {
        if (token?.Authorization?.Id is null)
            return null;
        return await _authorizationManager.FindByIdAsync(token.Authorization.Id) as OpenIddictEntityFrameworkCoreAuthorization;
    }
    private async Task<OpenIddictEntityFrameworkCoreApplication?> GetApplicationAsync(OpenIddictRequest request)
    {
        if (request.ClientId is null)
            return null;
        return await _applicationManager.FindByClientIdAsync(request.ClientId) as OpenIddictEntityFrameworkCoreApplication;
    }
    private async Task<bool> IsClientSecretValid(OpenIddictEntityFrameworkCoreApplication application, OpenIddictRequest request)
    {
        if (request.ClientSecret is null)
            return false;
        var clientType = await _applicationManager.GetClientTypeAsync(application);
        if (string.Equals(clientType, ClientTypes.Confidential, StringComparison.OrdinalIgnoreCase))
            return await _applicationManager.ValidateClientSecretAsync(application, request.ClientSecret);
        return true;
    }
    private async Task<bool> IsAuthorizationCodeExpired(OpenIddictEntityFrameworkCoreAuthorization authorization)
    {
        var creationDate = await _authorizationManager.GetCreationDateAsync(authorization);
        return creationDate + TimeSpan.FromMinutes(10) < DateTimeOffset.UtcNow;
    }
    private bool IsRedirectUriValid(OpenIddictEntityFrameworkCoreAuthorization authorization, OpenIddictRequest request)
    {
        if (authorization.Application?.RedirectUris is null || request.RedirectUri is null)
            return false;
        return authorization.Application.RedirectUris.Contains(request.RedirectUri);
    }
    private async Task<ClaimsPrincipal?> CreateClaimsPrincipal(OpenIddictEntityFrameworkCoreAuthorization authorization, List<String> grantedScopes)
    {
        if (authorization.Subject is null)
            return null;
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

        claimsPrincipal.SetClaim(Claims.Subject, authorization.Subject);
        var user = await _userManager.FindByIdAsync(authorization.Subject);
        if (user is null)
            return null;
        claimsPrincipal.SetClaim(Claims.Name, user.UserName);
        claimsPrincipal.SetScopes(grantedScopes);
        claimsPrincipal.SetDestinations(static claim =>
        {
            var subject = claim.Subject;
            bool hasProfileScope = subject is not null && subject.HasScope(Scopes.Profile);
            return claim.Type switch
            {
                Claims.Name when hasProfileScope => new[] { Destinations.AccessToken, Destinations.IdentityToken },
                _ => new[] { Destinations.AccessToken }
            };
        });
        return claimsPrincipal;
    }
    private BadRequestObjectResult CreateErrorResponse(string error, string description)
    {
        return new BadRequestObjectResult(new OpenIddictResponse
        {
            Error = error,
            ErrorDescription = description
        });
    }
    #endregion
}
        
