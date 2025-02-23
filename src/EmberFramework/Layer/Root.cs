using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using EmberFramework.Abstraction;
using EmberFramework.Abstraction.Layer;
using EmberFramework.Abstraction.Layer.Plugin;

namespace EmberFramework.Layer;

public class Root(
    ILifetimeScope parent,
    IEnumerable<IGracefulExitHandler> gracefulExitHandlers,
    IEnumerable<IPluginLoader> pluginLoaders)
    : IRoot
{
    private async ValueTask ExitAsync(CancellationToken cancellationToken = default)
    {
        foreach (var exitHandler in gracefulExitHandlers)
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

    private IAsyncEnumerable<IExecutable> GetExecutablesAsync(CancellationToken cancellationToken = default)
    {
        return pluginLoaders.ToAsyncEnumerable()
            .SelectMany(loader => loader.ResolveServiceAsync<IExecutable>(cancellationToken));
    }
    
    public async ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
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
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : notnull
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
