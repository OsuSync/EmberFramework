using System.Runtime.CompilerServices;
using Autofac;
using System.Runtime.Loader;
using System.Text.Json;
using Autofac.Extensions.DependencyInjection;
using EmberFramework.Abstraction.Layer.Plugin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmberFramework.Layer;

public class PluginLoader(ILifetimeScope parent, IConfiguration config) : IPluginLoader
{
    private readonly Dictionary<PluginMetadata, ILifetimeScope> _pluginContainers = [];

    public async ValueTask BuildScopeAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var metadata in GetPlugins(cancellationToken))
        {
            await LoadAsync(metadata, cancellationToken);
            
            foreach (var pluginResolver in _pluginContainers[metadata].Resolve<IEnumerable<IPluginResolver>>())
            {
                await pluginResolver.BuildScopeAsync(cancellationToken);
            }
        }
    }

    public async IAsyncEnumerable<T> ResolveServiceAsync<T>(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : class
    {
        foreach (var (_, containers) in _pluginContainers)
        {
            foreach (var resolver in containers.Resolve<IEnumerable<IPluginResolver>>())
            {
                await foreach (var service in resolver.ResolveServiceAsync<T>(cancellationToken))
                {
                    yield return service;
                }
            }
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        foreach (var container in _pluginContainers.Values)
            await using (container) {}
        
        _pluginContainers.Clear();
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public IAsyncEnumerable<PluginMetadata> GetPlugins(CancellationToken cancellationToken = default)
    {
        var path = config.GetPluginFolderPath();
        return PluginLoaderExtensions.EnumPluginFoldersAsync(path).ToAsyncEnumerable()
            .SelectAwait(p => PluginLoaderExtensions.GetMetadataByPathAsync(p, cancellationToken));
    }

    public bool IsLoaded(PluginMetadata metadata)
    {
        return _pluginContainers.ContainsKey(metadata);
    }

    public ValueTask LoadAsync(PluginMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (IsLoaded(metadata)) return ValueTask.CompletedTask;
        
        _pluginContainers.Add(metadata, parent.BeginLifetimeScope((builder) =>
        {
            builder.Populate(metadata.BuildPluginScope(config));
        }));
        
        return ValueTask.CompletedTask;
    }

    public async ValueTask UnloadAsync(PluginMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (!_pluginContainers.Remove(metadata, out var container)) return;

        await using var _ = container;
    }
}