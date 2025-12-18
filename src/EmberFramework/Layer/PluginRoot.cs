using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using EmberFramework.Abstraction;
using EmberFramework.Abstraction.Layer;
using EmberFramework.Abstraction.Layer.Plugin;

namespace EmberFramework.Layer;

public class PluginRoot(
    ILifetimeScope parent,
    IEnumerable<IPluginLoader> pluginLoaders)
    : IPluginRoot
{
    private async ValueTask ExitAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var exitHandler in GetExitHandlersAsync(cancellationToken))
        {
            try
            {
                await exitHandler.ExitAsync(cancellationToken);
            }
            catch
            {
                // ignored
            }
        }
    }

    private IAsyncEnumerable<IGracefulExitHandler> GetExitHandlersAsync(CancellationToken cancellationToken = default)
    {
        return pluginLoaders.ToAsyncEnumerable()
            .SelectMany(loader => loader.ResolveServiceAsync<IGracefulExitHandler>(cancellationToken))
            .Concat(parent.Resolve<IEnumerable<IGracefulExitHandler>>().ToAsyncEnumerable());
    }
    
    private IAsyncEnumerable<IExecutable> GetExecutablesAsync(CancellationToken cancellationToken = default)
    {
        return pluginLoaders.ToAsyncEnumerable()
            .SelectMany(loader => loader.ResolveServiceAsync<IExecutable>(cancellationToken));
    }

    private async ValueTask InitializeInfrastructureAsync(CancellationToken cancellationToken = default)
    {
        var initializers = parent.Resolve<IEnumerable<IInfrastructureInitializer>>();

        foreach (var infrastructureInitializer in initializers)
        {
            await infrastructureInitializer.InitializeAsync(cancellationToken);
        }
    }
    
    public async ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
        await InitializeInfrastructureAsync(cancellationToken);
        await BuildScopeAsync(cancellationToken);
        
        var allExecutables = await GetExecutablesAsync(cancellationToken)
            .ToListAsync(cancellationToken);
        
        var allTasks = allExecutables.Select(e => e.RunAsync(cancellationToken).AsTask());
        
        await Task.WhenAll(allTasks);
    }

    public async ValueTask BuildScopeAsync(CancellationToken cancellationToken = default)
    {
        foreach (var loader in pluginLoaders)
        {
            await loader.BuildScopeAsync(cancellationToken);
        }
    }

    public async IAsyncEnumerable<T> ResolveServiceAsync<T>(
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class
    {
        foreach (var pluginManager in pluginLoaders)
        {
            await foreach (var services in pluginManager.ResolveServiceAsync<T>(cancellationToken))
            {
                yield return services;
            }
        }
    }
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await ExitAsync();
        foreach (var loader in pluginLoaders)
            await using (loader) {}
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        DisposeAsync().AsTask().Wait();
    }

}
