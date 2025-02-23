namespace EmberFramework.Abstraction.Layer.Plugin;

public record PluginMetadata(string PluginFolder)
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? RepositoryUrl { get; init; }
    public string? Version { get; init; }
}
