namespace EmberFramework.Abstraction;

public interface IInfrastructureInitializer : IDisposable, IAsyncDisposable
{
    ValueTask InitializeAsync(CancellationToken cancellationToken = default);
}