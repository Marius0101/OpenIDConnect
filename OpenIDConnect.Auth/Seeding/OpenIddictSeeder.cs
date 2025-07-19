namespace OpenIDConnect.Seeding;

public class OpenIddictSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        await ClientSeeder.SeedAsync(scope.ServiceProvider);
        await ScopeSeeder.SeedAsync(scope.ServiceProvider);
        await UserSeeder.SeedAsync(scope.ServiceProvider);
    }
}
