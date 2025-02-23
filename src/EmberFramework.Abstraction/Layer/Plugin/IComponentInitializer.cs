namespace EmberFramework.Abstraction.Layer.Plugin;

public interface IComponentInitializer : IDisposable, IAsyncDisposable
{
    ValueTask InitializeAsync(CancellationToken cancellationToken = default);
}
