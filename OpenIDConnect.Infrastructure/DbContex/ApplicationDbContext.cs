using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using Microsoft.EntityFrameworkCore;

namespace OpenIDConnect.Infrastructure.DbContex;

public class ApplicationDbContext: IdentityDbContext 
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options){}

    public DbSet<OpenIddictEntityFrameworkCoreApplication> Applications { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreAuthorization> Authorizations { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreScope> Scopes { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreToken> Tokens { get; set; }

}
