using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Strawberry.Models;
using Strawberry.Services;

namespace Strawberry.Data;

public static class MigrationExtensions
{
    public static async Task<WebApplication> EnsureMigrationsAppliedAsync<TContext>(this WebApplication app)
        where TContext : DbContext
    {
        using IServiceScope scope = app.Services.CreateScope();
        IDbContextFactory<TContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TContext>>();
        await using TContext context = factory.CreateDbContext();
        await context.Database.MigrateAsync();
        return app;
    }

    public static async Task<WebApplication> EnsureRolesExistAsync(this WebApplication app, params string[] roles)
    {
        using IServiceScope scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
        return app;
    }

    public static async Task<WebApplication> EnsureAdminExistsAsync(this WebApplication app, string username, string password)
    {
        using IServiceScope scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        if (await userManager.FindByNameAsync(username) != null) return app;

        var admin = new AppUser
        {
            UserName = username,
            Email = "admin@strawberry.nl",
            FirstName = "Admin",
            LastName = "System",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, password);
        await userManager.AddToRoleAsync(admin, "Admin");

        var audit = scope.ServiceProvider.GetRequiredService<AuditService>();
        await audit.LogAsync("User.Created", $"Username={username}, Role=Admin (seed)");
        return app;
    }
}
