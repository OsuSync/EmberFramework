using EmberFramework.Abstraction.Layer.Plugin;
using EmberFramework.Test.SeparatedDummies;
using Microsoft.Extensions.DependencyInjection;

namespace EmberFramework.Test;

public class PluginDummyService {}
public class PluginDummyService2 {}
public class PluginDummyService3 {}

public class PluginWithoutInitializer : IPlugin
{
    public ValueTask<IServiceCollection> BuildComponents(CancellationToken cancellationToken = default)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<PluginDummyService>();
        return ValueTask.FromResult<IServiceCollection>(serviceCollection);
    }
}
public class PluginWithoutInitializer2 : IPlugin
{
    public ValueTask<IServiceCollection> BuildComponents(CancellationToken cancellationToken = default)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<PluginDummyService2>();
        return ValueTask.FromResult<IServiceCollection>(serviceCollection);
    }
}
public class PluginWithoutInitializerButWithExternalInitializer : IPlugin
{
    public ValueTask<IServiceCollection> BuildComponents(CancellationToken cancellationToken = default)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IComponentInitializer, ComponentInitializer>();
        return ValueTask.FromResult<IServiceCollection>(serviceCollection);
    }
}
public class PluginWithoutInitializerButReferenceServiceInParent(PluginDummyServiceInParent service) : IPlugin
{
    public PluginDummyServiceInParent ParentService => service;
    
    public ValueTask<IServiceCollection> BuildComponents(CancellationToken cancellationToken = default)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<PluginDummyService2>();
        return ValueTask.FromResult<IServiceCollection>(serviceCollection);
    }
}

public class PluginWithInitializer : PluginWithoutInitializer, IPlugin.IWithInitializer
{
    public bool IsInitializeCalled { get; set; }
    public ValueTask InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        IsInitializeCalled = true;
        return ValueTask.CompletedTask;
    }
}

public class ComponentInitializer : IComponentInitializer
{
    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }

    public bool IsInitializeCalled { get; set; }
    public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsInitializeCalled = true;

        return default;
    }
}