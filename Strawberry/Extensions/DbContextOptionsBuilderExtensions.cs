using Microsoft.EntityFrameworkCore;

namespace Strawberry.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseMariaDb(this DbContextOptionsBuilder builder, string connectionString)
    {
        return builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
}