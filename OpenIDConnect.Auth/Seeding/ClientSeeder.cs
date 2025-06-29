using OpenIddict.Abstractions;

namespace OpenIDConnect.Auth.Seeding;

public class ClientSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var applicationManager  = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        if (await applicationManager.FindByClientIdAsync("my-client") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "my-client",
                ClientSecret = "secret",
                RedirectUris = { new Uri("https://localhost:4200/callback") },
                DisplayName = "My Client App",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    "api"
                }
            });
        }

    }
}
