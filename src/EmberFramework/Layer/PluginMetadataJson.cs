namespace EmberFramework.Layer;

public class PluginMetadataJson
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? RepositoryUrl { get; init; }
}
