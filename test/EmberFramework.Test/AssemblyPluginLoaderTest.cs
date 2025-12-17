using Autofac;
using EmberFramework.Abstraction.Layer.Plugin;
using EmberFramework.Layer;
using EmberFramework.Test.SeparatedDummies;
using Microsoft.Extensions.Configuration;

namespace EmberFramework.Test;

public class AssemblyPluginLoaderTest
{
    private ILifetimeScope BuildScope()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<PluginDummyServiceInParent>();
        containerBuilder.RegisterInstance<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection().Build());
        containerBuilder.RegisterType<AssemblyPluginLoader>().AsSelf().As<IPluginLoader>();
        return containerBuilder.Build();
    }
    
    [Fact]
    public async Task TestPluginLoaderCanBuildScopeAndResolve()
    {
        await using var scope = BuildScope();

        var pluginLoader = scope.Resolve<IPluginLoader>();
        await pluginLoader.BuildScopeAsync(TestContext.Current.CancellationToken);
        var config = scope.Resolve<IConfiguration>();
        var pluginMetadataList = await PluginLoaderExtensions.EnumPluginFoldersAsync(config.GetPluginFolderPath()).ToAsyncEnumerable()
            .Select(PluginLoaderExtensions.GetMetadataByPathAsync)
            .ToListAsync(TestContext.Current.CancellationToken);

        var plugins = await pluginLoader.ResolveServiceAsync<IPlugin>(TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.NotEmpty(plugins);
        Assert.All(pluginMetadataList, metadata => Assert.True(pluginLoader.IsLoaded(metadata)));
        
        foreach (var pluginMetadata in pluginMetadataList)
        {
            await pluginLoader.LoadAsync(pluginMetadata, TestContext.Current.CancellationToken);
        }
        
        var pluginMetadataList2 = await PluginLoaderExtensions.EnumPluginFoldersAsync(config.GetPluginFolderPath()).ToAsyncEnumerable()
            .Select(PluginLoaderExtensions.GetMetadataByPathAsync)
            .ToListAsync(TestContext.Current.CancellationToken);
        
        Assert.Equal(pluginMetadataList, pluginMetadataList2);
        
        foreach (var pluginMetadata in pluginMetadataList)
        {
            await pluginLoader.UnloadAsync(pluginMetadata, TestContext.Current.CancellationToken);
            await pluginLoader.UnloadAsync(pluginMetadata, TestContext.Current.CancellationToken);
        }
        Assert.All(pluginMetadataList, metadata => Assert.False(pluginLoader.IsLoaded(metadata)));
        
        foreach (var pluginMetadata in pluginMetadataList)
        {
            await pluginLoader.LoadAsync(pluginMetadata, TestContext.Current.CancellationToken);
        }
        var pluginMetadataList3 = await PluginLoaderExtensions.EnumPluginFoldersAsync(config.GetPluginFolderPath()).ToAsyncEnumerable()
            .Select(PluginLoaderExtensions.GetMetadataByPathAsync)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(pluginMetadataList, pluginMetadataList3);
        
        scope.Resolve<IPluginLoader>().Dispose();
    }
}