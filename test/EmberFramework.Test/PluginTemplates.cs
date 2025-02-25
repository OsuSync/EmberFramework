using EmberFramework.Abstraction;
using EmberFramework.Abstraction.Layer.Plugin;
using EmberFramework.Test.SeparatedDummies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        serviceCollection.AddSingleton<IGracefulExitHandler, ExitHandlerInPlugin>();
        serviceCollection.AddSingleton<IExecutable, ExecuteHandler>();
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

public class TestHandler : IGracefulExitHandler, ICallRecord
{
    public bool IsCalled { get; private set; }
    public ValueTask ExitAsync(CancellationToken cancellationToken = default)
    {
        IsCalled = true;
        return ValueTask.CompletedTask;
    }
}
    
public class ExecuteHandler : IExecutable, ICallRecord
{
    public bool IsCalled { get; private set; }
    public ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
        IsCalled = true;
        return ValueTask.CompletedTask;
    }
}


public class ExceptionThrownExitHandler : IGracefulExitHandler, ICallRecord
{
    public bool IsCalled { get; private set; }
    public ValueTask ExitAsync(CancellationToken cancellationToken = default)
    {
        IsCalled = true;
        throw new Exception();
    }
}

public class ExitHandlerInPlugin : IGracefulExitHandler, ICallRecord
{
    public bool IsCalled { get; private set; }
    public ValueTask ExitAsync(CancellationToken cancellationToken = default)
    {
        IsCalled = true;
        return ValueTask.CompletedTask;
    }
}
