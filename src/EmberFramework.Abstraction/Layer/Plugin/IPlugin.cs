namespace EmberFramework.Abstraction.Layer.Plugin;

public interface IPlugin : IComponentBuilder
{
    public interface IWithInitializer : IPlugin
    {
        ValueTask InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);
    }
}
