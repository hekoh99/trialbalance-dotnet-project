using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TriBalance.Application.Common.Messaging;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers dispatchers + every closed generic implementation of
    /// ICommandHandler&lt;,&gt; and IQueryHandler&lt;,&gt; found in the Application assembly.
    /// Call from Program.cs: <c>builder.Services.AddApplicationMessaging();</c>
    /// </summary>
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services)
    {
        // Scoped so dispatchers receive the per-request IServiceProvider and
        // can resolve scoped handlers (DbContext, EF-backed repositories).
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;
        RegisterHandlers(services, assembly, typeof(ICommandHandler<,>));
        RegisterHandlers(services, assembly, typeof(IQueryHandler<,>));

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly, Type openHandlerType)
    {
        var concreteHandlers = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openHandlerType)
                .Select(i => new { Implementation = t, Service = i }));

        foreach (var pair in concreteHandlers)
            services.AddScoped(pair.Service, pair.Implementation);
    }

    /// <summary>Registers a single pipeline behavior (open generic).</summary>
    public static IServiceCollection AddPipelineBehavior(this IServiceCollection services, Type openBehaviorType)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), openBehaviorType);
        return services;
    }
}
