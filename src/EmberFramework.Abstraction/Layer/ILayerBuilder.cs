namespace EmberFramework.Abstraction.Layer;

public interface ILayerBuilder : IDisposable, IAsyncDisposable
{
    ValueTask BuildScopeAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> ResolveServiceAsync<T>(CancellationToken cancellationToken = default)
        where T : notnull;
}