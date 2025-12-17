using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using EmberFramework.Abstraction.Layer.Plugin;
using Microsoft.Extensions.Configuration;

namespace EmberFramework.Layer;

public class TypedPluginLoader(ILifetimeScope parent, IConfiguration config) : IPluginLoader
{
    private static readonly Dictionary<PluginMetadata, Type> _registeredTypes = [];

    public static void Register<T>() where T : IPlugin =>
        _registeredTypes.Add(new PluginMetadata(typeof(T).Name), typeof(T));
    
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

    public IAsyncEnumerable<PluginMetadata> GetPlugins(CancellationToken cancellationToken = new())
    {
        return _registeredTypes.Keys.ToAsyncEnumerable();
    }

    public bool IsLoaded(PluginMetadata metadata)
    {
        return _pluginContainers.ContainsKey(metadata);
    }

    public ValueTask LoadAsync(PluginMetadata metadata, CancellationToken cancellationToken = new())
    {
        if (IsLoaded(metadata)) return ValueTask.CompletedTask;
        _pluginContainers.Add(metadata, parent.BeginLifetimeScope((builder) =>
        {
            builder.RegisterInstance(_registeredTypes[metadata]);
            builder.RegisterType<TypedPluginResolver>().As<IPluginResolver>().SingleInstance();
        }));
        
        return ValueTask.CompletedTask;
    }

    public async ValueTask UnloadAsync(PluginMetadata metadata, CancellationToken cancellationToken = new())
    {
        if (!_pluginContainers.Remove(metadata, out var container)) return;

        await using var _ = container;
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

}