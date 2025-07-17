using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIDConnect.Auth.GrantHandlers;
using OpenIDConnect.Auth.Seeding;
using OpenIDConnect.Infrastructure.DbContex;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));

    options.UseOpenIddict();
});
builder.Services.AddScoped<IGrantTypeHandler, ClientCredentialsGrantHandler>();
builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);

    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token");
        options.AllowClientCredentialsFlow();

        options.SetAuthorizationEndpointUris("/connect/authorize");

        options.AllowPasswordFlow()
               .AllowAuthorizationCodeFlow()
               .AllowRefreshTokenFlow();

        options.AcceptAnonymousClients();

        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough();
    });
    
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Add seeding logic
await OpenIddictSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); 
    endpoints.MapRazorPages();  
});

app.Run();
