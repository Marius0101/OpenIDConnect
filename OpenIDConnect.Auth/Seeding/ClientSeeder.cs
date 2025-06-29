using OpenIddict.Abstractions;

namespace OpenIDConnect.Auth.Seeding;

public class ClientSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var applicationManager  = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        if (await applicationManager.FindByClientIdAsync("api") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "my-client",
                ClientSecret = "secret",
                DisplayName = "My Client App",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    "api"
                }
            });
        }

    }
}
