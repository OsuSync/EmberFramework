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

    private static async ValueTask<PluginMetadata> GetMetadataAsync(string pluginFolder,
        CancellationToken cancellationToken = default)
    {
        var pluginJsonPath = Path.Combine(pluginFolder, "plugin.json");
        if (!File.Exists(pluginJsonPath)) return new PluginMetadata(pluginFolder);
        
        await using var sr = File.OpenRead(pluginJsonPath);
        var metadataJson = await JsonSerializer.DeserializeAsync<PluginMetadataJson>(sr, cancellationToken: cancellationToken);
        
        if (metadataJson == null) return new PluginMetadata(pluginFolder);

        return new PluginMetadata(pluginFolder)
        {
            Id = metadataJson.Id,
            Name = metadataJson.Name,
            Description = metadataJson.Description,
            RepositoryUrl = metadataJson.RepositoryUrl,
            Version = metadataJson.Version,
        };
    }
    
    public async ValueTask BuildScopeAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var metadata in GetPlugins(cancellationToken))
        {
            await LoadAsync(metadata, cancellationToken);
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
        GC.SuppressFinalize(this);
        foreach (var container in _pluginContainers.Values) using (container) {}

        _pluginContainers.Clear();
    }

    public async IAsyncEnumerable<PluginMetadata> GetPlugins(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pluginDir = config.GetSection(nameof(PluginLoader))["Path"] ?? "plugins";
        
        if (!Directory.Exists(pluginDir)) Directory.CreateDirectory(pluginDir);

        foreach (var directory in Directory.EnumerateDirectories(pluginDir))
        {
            var pluginFolder = Path.GetFullPath(Path.Combine(pluginDir, directory));
            yield return await GetMetadataAsync(pluginFolder, cancellationToken);
        }
    }

    public bool IsLoaded(PluginMetadata metadata)
    {
        return _pluginContainers.ContainsKey(metadata);
    }

    public ValueTask LoadAsync(PluginMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (IsLoaded(metadata)) return ValueTask.CompletedTask;
        
        var collection = new ServiceCollection();
        collection.AddSingleton(config);
        collection.AddSingleton(metadata);
        collection.AddSingleton(metadata.MakeLocalAssemblyLoadContext);
        collection.AddSingleton<PluginResolver>();

        _pluginContainers.Add(metadata, parent.BeginLifetimeScope((builder) =>
        {
            builder.Populate(collection);
        }));
        
        return ValueTask.CompletedTask;
    }

    public async ValueTask UnloadAsync(PluginMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (!_pluginContainers.Remove(metadata, out var container)) return;

        await using var _ = container;
    }
}