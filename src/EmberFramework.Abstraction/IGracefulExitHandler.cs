namespace EmberFramework.Abstraction;

public interface IGracefulExitHandler
{
    public ValueTask ExitAsync(CancellationToken cancellationToken = default);
}
