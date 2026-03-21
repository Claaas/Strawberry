using System.Diagnostics.CodeAnalysis;

namespace Strawberry.Extensions;

public static class IConfigurationExtensions
{
    public static T GetOrThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this IConfiguration configuration, string? exceptionMessage = null)
    {
        return configuration.Get<T>() ?? throw new InvalidOperationException(exceptionMessage);
    }

    public static string GetConnectionStringOrThrow(this IConfiguration configuration, string name, string? exceptionMessage = null)
    {
        return configuration.GetConnectionString(name)
               ?? Environment.GetEnvironmentVariable(name)
               ?? throw new InvalidOperationException(exceptionMessage);
    }
}