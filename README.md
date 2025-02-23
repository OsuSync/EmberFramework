Ember Framework
----
A lightweight plugin framework.

```csharp
// project: application
var root = await RootBuilder
    .Boot()
    .Infrastructures((registry, config) => registry.AddSingleton(...))
    .UseLoader<PluginLoader>() // defualt load location is $cwd/plugins
    .UseLoader<CustomLoader>() // support multiple loader
    .Build();

await root.RunAsync(cancellationToken);

// project: plugin
class MyPlugin : IPlugin // or use IPlugin.IWithInitializer to initialize service in plugin class
{
    public ValueTask<IServiceCollection> BuildComponents(CancellationToken cancellationToken = default)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<MyService>();
        ...
        return serviceCollection;
    }

}

// if MyPlugin implemented IWithIntiializer, this class can be simplifed
class ServiceInitializer(MyService myService, ...) : IComponentInitializer
{
    async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        await myService.Connect(...);
    }
    ...
}
```
