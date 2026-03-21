using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Strawberry.Components;
using Strawberry.Data;
using Strawberry.Extensions;
using Strawberry.Models;
using Strawberry.Services;

var builder = WebApplication.CreateBuilder(args);

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("nl-NL");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("nl-NL");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<IdentityUserAccessor>();

builder.Services.AddDbContextWithPooledFactory<AppDbContext>(options =>
{
    options.UseMariaDb(builder.Configuration.GetConnectionStringOrThrow("ConnectionString", "Connection string not found!"));

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddAntiforgery();
builder.Services.AddScoped<QrCodeService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<WeighingService>();
builder.Services.AddScoped<BalanceService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddSingleton<TranslationService>();
builder.Services.AddScoped<UserLanguageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}


app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();
await app.EnsureMigrationsAppliedAsync<AppDbContext>();
await app.EnsureRolesExistAsync("Admin", "DropOff", "Harvester");

//To create the first account for the db in case there is none lol
await app.EnsureAdminExistsAsync("admin", "Strawberry!");

app.Run();
