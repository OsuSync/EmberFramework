namespace EmberFramework.Abstraction.Layer.Plugin;

public interface IPluginLoader : ILayerBuilder
{
    public IAsyncEnumerable<PluginMetadata> GetPlugins(CancellationToken cancellationToken = default);
    
    public bool IsLoaded(PluginMetadata metadata);
    
    public ValueTask LoadAsync(PluginMetadata metadata, CancellationToken cancellationToken = default);
    
    public ValueTask UnloadAsync(PluginMetadata metadata, CancellationToken cancellationToken = default);
}
