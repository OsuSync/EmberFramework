using System.Runtime.Loader;
using Autofac;
using EmberFramework.Abstraction.Layer.Plugin;
using EmberFramework.Layer;
using EmberFramework.Test.SeparatedDummies;

namespace EmberFramework.Test;

public class PluginResolverTest
{

    private static ILifetimeScope BuildEmptyRootScope()
    {
        var builder = new ContainerBuilder();
        var pluginMetadata = new PluginMetadata(Path.Combine(Environment.CurrentDirectory, PluginLoader.DefaultPluginFolder));
        builder.RegisterInstance(pluginMetadata);
        builder.Register(_ => pluginMetadata.MakeLocalAssemblyLoadContext());
        builder.RegisterType<PluginResolver>().SingleInstance();
        builder.RegisterType<PluginDummyServiceInParent>().SingleInstance();
        
        return builder.Build();
    }
    private static ILifetimeScope BuildRootScope()
    {
        var builder = new ContainerBuilder();
        var pluginMetadata = new PluginMetadata(Path.Combine(Environment.CurrentDirectory, PluginLoader.DefaultPluginFolder, "EmberTest"));
        builder.RegisterInstance(pluginMetadata);
        builder.Register(_ => pluginMetadata.MakeLocalAssemblyLoadContext());
        builder.RegisterType<PluginResolver>().SingleInstance();
        builder.RegisterType<PluginDummyServiceInParent>().SingleInstance();
        
        return builder.Build();
    }

    [Fact]
    public async Task IntegrationTestPluginResolverShouldBuildScopeCorrectly()
    {
        await using var scope = BuildRootScope();
        var resolver = scope.Resolve<PluginResolver>();

        await resolver.BuildScopeAsync(TestContext.Current.CancellationToken);
        
        var result = await resolver.ResolveServiceAsync<IPlugin>(TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);
        
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task TestResolverShouldReturnEmptyWhenNotBuildScope()
    {
        await using var scope = BuildRootScope();
        var resolver = scope.Resolve<PluginResolver>();
        var plugins = await resolver.ResolveServiceAsync<IPlugin>(TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);
        
        Assert.Empty(plugins);
    }
    
    [Fact]
    public async Task TestResolverShouldReturnEmptyWhenResolveNotRegisteredComponent()
    {
        await using var scope = BuildRootScope();
        var resolver = scope.Resolve<PluginResolver>();
        
        await resolver.BuildScopeAsync(TestContext.Current.CancellationToken);
        var plugins = await resolver.ResolveServiceAsync<Task>(TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);
        
        Assert.Empty(plugins);
    }

    [Fact]
    public async Task TestResolverShouldReturnEmptyWhenScopeIsEmpty()
    {
        await using var scope = BuildEmptyRootScope();
        var resolver = scope.Resolve<PluginResolver>();
        var plugins = await resolver.ResolveServiceAsync<IPlugin>(TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);
        
        Assert.Empty(plugins);
    }
    
    [Fact]
    public void TestDisposeWillCallDisposeAsyncJustForCoverage()
    {
        using var scope = BuildEmptyRootScope();
        var resolver = scope.Resolve<PluginResolver>();
    }

}