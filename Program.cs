using Azure.Identity; // För autentisering med Managed Identity
using DataTrust.Data;
using DataTrust.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// **Steg 1**:
// spara URI för ditt Azure Key Vault i en variabel 
var keyVaultUri = new Uri("https://projekt-key-vault.vault.azure.net/");

// **Steg 2**:
// lägg till Azure Key Vault i konfigurationen. 
// builder.Configuration.AddAzureKeyVault gör att din applikation kan hämta konfigurationsdata från Azure Key Vault.
// Det innebär att du kan hämta secrets som lagras i Azure Key Vault och använda dem som en del av applikationens konfiguration.
// DefaultAzureCredential() hanterar autentiseringen och ser till att applikationen autentiserar sig mot key vault i azure. 
// I den här appen använder vi Managed Identity för autentisering, vilket innebär att när appen körs på Azure (t.ex.
// i Azure App Service), får den automatiskt en identitet som kan användas för att autentisera sig mot andra Azure-tjänster, som Azure Key Vault.
builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());

/* (Om din app inte är på Azure, kan du fortfarande få åtkomst till Azure Key Vault genom att använda en App Registration och autentisera
 * den via en client secret)! */

// **Steg 3**: Hämta secret (i detta fall anslutningssträngen) från Key Vault
// DefaultConnection är namnet på vår secret 
string connectionString = builder.Configuration["DefaultConnection"];

// **Steg 4**: Skapa en databaskoppling med strängen 
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// **Steg 5**: Konfigurera autentisering via Google (OpenID Connect)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Google";
})
.AddCookie(options =>
{
    options.Events.OnValidatePrincipal += async context =>
    {
        var serviceProvider = context.HttpContext.RequestServices;
        using var db = new AppDbContext(serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

        string subject = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string issuer = context.Principal.FindFirst(ClaimTypes.NameIdentifier).Issuer;
        string name = context.Principal.FindFirst(ClaimTypes.Name).Value;

        var account = db.Accounts
            .FirstOrDefault(p => p.OpenIDIssuer == issuer && p.OpenIDSubject == subject);

        if (account == null)
        {
            account = new Account
            {
                OpenIDIssuer = issuer,
                OpenIDSubject = subject,
                Name = name
            };
            db.Accounts.Add(account);
        }
        else
        {
            account.Name = name;
        }

        await db.SaveChangesAsync();
    };
})
.AddOpenIdConnect("Google", options =>
{
    options.Authority = "https://accounts.google.com";
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.CallbackPath = "/signin-oidc-google";
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AccessControl>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    SampleData.Create(context);
}

app.Run();
