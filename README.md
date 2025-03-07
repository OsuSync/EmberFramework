Ember Framework
----
A lightweight plugin framework.


## Usage
**Setup infrastructures and plugin-loader in host application**
```csharp
// project: host application
var root = await RootBuilder
    .Boot()
    .Infrastructures((registry, config) => registry.AddSingleton(...))
    .UseLoader<PluginLoader>() // load plugins from $cwd/plugins in default
    .UseLoader<CustomLoader>() // support multiple loader
    .Build();

await root.RunAsync(cancellationToken);
```

**Setup plugins**
```csharp
// project: plugin

// Register your services into denpendency inject container
class MyPlugin : IPlugin
{
    public ValueTask<IServiceCollection> BuildComponents(CancellationToken cancellationToken = default)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<MyService>();
        serviceCollection.AddSingleton<MyServiceConfigurer, IComponentInitializer>();
        
        return serviceCollection;
    }
}

// Initialize your services
class MyServiceInitializer(MyService myService, ...) : IComponentInitializer
{
    public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        await myService.Connect(...);
    }
    ...
}

// Run your service!
class RunningJob(MyService myService) : IExecutable
{
    public async ValueTask RunAsync(CancellationToken cancellationToken = default)
    {
        await myService.ListenAsync(8080, cancenllationToken);
    }
}
```
