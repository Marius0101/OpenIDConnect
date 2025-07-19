using System;
using Microsoft.AspNetCore.Identity;

namespace OpenIDConnect.Seeding;

public class UserSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByNameAsync("testuser");
        if (user == null)
        {
            user = new IdentityUser
            {
                UserName = "testuser",
                Email = "testuser@example.com",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "P@ssw0rd!");
            if (!result.Succeeded)
            {
                throw new Exception("Failed to create test user: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
