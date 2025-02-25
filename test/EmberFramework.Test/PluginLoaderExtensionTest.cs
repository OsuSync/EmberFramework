using System.Runtime.Loader;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using EmberFramework.Abstraction.Layer.Plugin;
using EmberFramework.Layer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmberFramework.Test;

public class PluginLoaderExtensionTest
{
    [Fact]
    public void TestGetPluginRootFromConfigurationReturnIfSetupInConfiguration()
    {
        var configRoot = (new ConfigurationBuilder()).AddInMemoryCollection().Build();
        
        var randStr = Guid.NewGuid().ToString();
        configRoot.GetSection(nameof(PluginLoader))["Path"] = randStr;
        var path = Path.Combine(Environment.CurrentDirectory, randStr);
        
        Assert.Equal(configRoot.GetPluginFolderPath(), path);
    }
    
    [Fact]
    public void TestGetPluginRootFromConfigurationReturnDefaultIfNotSetup()
    {
        var configRoot = new ConfigurationRoot([]);
        var pluginsPath = Path.Combine(Environment.CurrentDirectory, "plugins");
        
        Assert.Equal(pluginsPath, configRoot.GetPluginFolderPath());
    }

    [Fact]
    public async Task TestGetMetadataIfPluginJsonNotExists()
    {
        var path = "/a-folder-not-includes-plugin-json";
        var metadata = await PluginLoaderExtensions.GetMetadataByPathAsync(path,
            TestContext.Current.CancellationToken);
        
        Assert.Equal(path, metadata.PluginFolder);
        Assert.Null(metadata.Id);
        Assert.Null(metadata.Name);
    }
    
    [Fact]
    public async Task TestGetMetadataWillLoadMetadataFromJson()
    {
        var path = Path.Combine(Environment.CurrentDirectory);
        var metadata = await PluginLoaderExtensions.GetMetadataByPathAsync(path,
            TestContext.Current.CancellationToken);
        
        Assert.Equal(metadata.PluginFolder, path);
        Assert.Equal("id", metadata.Id);
        Assert.Equal("name", metadata.Name);
        Assert.Equal("1.0.0", metadata.Version);
        Assert.Equal("description", metadata.Description);
        Assert.Equal("repositoryUrl", metadata.RepositoryUrl);
    }

    [Fact]
    public void TestEnumAllPluginsFolder()
    {
        var path = Path.Combine(Environment.CurrentDirectory, "Plugins");
        var result = PluginLoaderExtensions.EnumPluginFoldersAsync(path).ToList();
        
        var target = Path.Combine(Environment.CurrentDirectory, "Plugins", "EmberTest");
        Assert.Contains(result, x => x == target);
        Assert.Single(result);
    }
    
    
    [Fact]
    public void TestEnumAllPluginsFolderWillCreatePluginFolder()
    {
        var uuid = Guid.NewGuid().ToString();
        var path = Path.Combine(Environment.CurrentDirectory, uuid);
        
        Assert.False(Directory.Exists(path));
        
        _ = PluginLoaderExtensions.EnumPluginFoldersAsync(path).ToList();
        
        Assert.True(Directory.Exists(path));
        
        Directory.Delete(path, false);
        Directory.CreateDirectory(path);
    }

    [Fact]
    public void TestBuildPluginScope()
    {
        IConfiguration config = new ConfigurationRoot([]);
        var metadata = new PluginMetadata(Environment.CurrentDirectory);

        var serviceCollection = metadata.BuildPluginScope(config);
        
        Assert.Contains(serviceCollection, x => x.Lifetime == ServiceLifetime.Singleton
                                                && x.ServiceType == typeof(IConfiguration));
        Assert.Contains(serviceCollection, x => x.Lifetime == ServiceLifetime.Singleton
                                                && x.ServiceType == typeof(PluginMetadata));
        Assert.Contains(serviceCollection, x => x.Lifetime == ServiceLifetime.Singleton
                                                && x.ServiceType == typeof(AssemblyLoadContext));
        Assert.Contains(serviceCollection, x => x.Lifetime == ServiceLifetime.Singleton
                                                && x.ServiceType == typeof(IPluginResolver));

        var b = new ContainerBuilder();
        b.Populate(serviceCollection);
        using var scope = b.Build();
        
        scope.Resolve<AssemblyLoadContext>();
    }
}