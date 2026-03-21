using Microsoft.EntityFrameworkCore;

namespace Strawberry.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDbContextWithPooledFactory<TContext>(this IServiceCollection collection, Action<DbContextOptionsBuilder> optionsAction) where TContext : DbContext
    {
        return collection.AddPooledDbContextFactory<TContext>(optionsAction)
            .AddScoped((serviceProvider) => serviceProvider.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());
    }
}