using OpenIddict.Abstractions;

namespace OpenIDConnect.Seeding;

public class ScopeSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var scopeManager = serviceProvider.GetRequiredService<IOpenIddictScopeManager>();
            if (await scopeManager.FindByNameAsync("api") == null)
            {
                await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
                {
                    Name = "api",
                    DisplayName = "Access to My API",
                    Resources = { "resource_server" } 
                });
            }

        }
}
