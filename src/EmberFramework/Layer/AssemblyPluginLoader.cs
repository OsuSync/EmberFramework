using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Autofac;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using EmberFramework.Abstraction.Layer.Plugin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmberFramework.Layer;

public class AssemblyPluginLoader(ILifetimeScope parent, IConfiguration config) : IPluginLoader
{
    
    public const string DefaultPluginFolder = "plugins";
    
    private readonly Dictionary<PluginMetadata, ILifetimeScope> _pluginContainers = [];

    public virtual async ValueTask BuildScopeAsync(CancellationToken cancellationToken = default)
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

    public virtual async IAsyncEnumerable<T> ResolveServiceAsync<T>(
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
    
    public virtual async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        foreach (var container in _pluginContainers.Values)
            await using (container) {}
        
        _pluginContainers.Clear();
    }

    public virtual void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public virtual IAsyncEnumerable<PluginMetadata> GetPlugins(CancellationToken cancellationToken = default)
    {
        var path = config.GetPluginFolderPath();
        return PluginLoaderExtensions.EnumPluginFoldersAsync(path).ToAsyncEnumerable()
            .Select(PluginLoaderExtensions.GetMetadataByPathAsync);
    }

    public virtual bool IsLoaded(PluginMetadata metadata)
    {
        return _pluginContainers.ContainsKey(metadata);
    }

    public virtual ValueTask LoadAsync(PluginMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (IsLoaded(metadata)) return ValueTask.CompletedTask;
        
        _pluginContainers.Add(metadata, parent.BeginLifetimeScope((builder) =>
        {
            builder.Populate(metadata.BuildPluginScope(config));
        }));
        
        return ValueTask.CompletedTask;
    }

    public virtual async ValueTask UnloadAsync(PluginMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (!_pluginContainers.Remove(metadata, out var container)) return;

        await using var _ = container;
    }
}