using System.Runtime.CompilerServices;
using System.Text.Json;
using EmberFramework.Abstraction.Layer.Plugin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmberFramework.Layer;

public static class PluginLoaderExtensions
{
    public static string GetPluginFolderPath(this IConfiguration configuration)
    {
        var dir = configuration.GetSection(nameof(PluginLoader))["Path"] ?? "plugins";

        return Path.GetFullPath(dir);
    }

    public static async ValueTask<PluginMetadata> GetMetadataByPathAsync(string pluginFolderPath,
        CancellationToken cancellationToken = default)
    {
        var pluginJsonPath = Path.Combine(pluginFolderPath, "plugin.json");
        if (!File.Exists(pluginJsonPath)) return new PluginMetadata(pluginFolderPath);
        
        await using var sr = File.OpenRead(pluginJsonPath);
        var metadataJson = await JsonSerializer.DeserializeAsync<PluginMetadataJson>(sr, cancellationToken: cancellationToken);
        
        return new PluginMetadata(pluginFolderPath)
        {
            Id = metadataJson!.Id,
            Name = metadataJson.Name,
            Description = metadataJson.Description,
            RepositoryUrl = metadataJson.RepositoryUrl,
            Version = metadataJson.Version,
        };
    }

    public static IEnumerable<string> EnumPluginFoldersAsync(string pluginDir)
    {
        if (!Directory.Exists(pluginDir)) Directory.CreateDirectory(pluginDir);

        foreach (var directory in Directory.EnumerateDirectories(pluginDir))
        {
            yield return directory;
        }
    }

    public static IServiceCollection BuildPluginScope(this PluginMetadata metadata, IConfiguration config)
    {        
        var collection = new ServiceCollection();
        collection.AddSingleton(config);
        collection.AddSingleton(metadata);
        collection.AddSingleton(_ => metadata.MakeLocalAssemblyLoadContext());
        collection.AddSingleton<IPluginResolver, PluginResolver>();

        return collection;
    }
}