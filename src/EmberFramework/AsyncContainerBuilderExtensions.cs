using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace EmberFramework;

public static class AsyncContainerBuilderExtensions
{
    public static async ValueTask<ILifetimeScope> BuildAsync(this ILifetimeScope parent,
        Func<ILifetimeScope, IServiceCollection, CancellationToken, ValueTask> builder,
        CancellationToken cancellationToken = default)
    {
        var serviceCollection = new ServiceCollection();
        await builder(parent, serviceCollection, cancellationToken);
        
        return parent.BeginLifetimeScope((scopeBuilder) => scopeBuilder.Populate(serviceCollection));
    }
}