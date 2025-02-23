namespace EmberFramework.Abstraction;

public interface IExecutable
{
    public ValueTask RunAsync(CancellationToken cancellationToken = default);
}