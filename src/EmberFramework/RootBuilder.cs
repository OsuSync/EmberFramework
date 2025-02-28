using Autofac;
using Autofac.Extensions.DependencyInjection;
using EmberFramework.Abstraction.Layer;
using EmberFramework.Abstraction.Layer.Plugin;
using EmberFramework.Layer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmberFramework;

public class RootBuilder
{
    private readonly IConfigurationRoot _configurationRoot;
    private readonly ContainerBuilder _containerBuilder = new();
    private List<Type> _pluginLoaderTypes = [];
    public IEnumerable<Type> PluginLoaderTypes => _pluginLoaderTypes;
    
    public RootBuilder(IConfigurationRoot configurationRoot)
    {
        _configurationRoot = configurationRoot;
        _containerBuilder
            .Register(ctx => new AutofacServiceProvider(ctx.Resolve<ILifetimeScope>()))
            .As<IServiceProvider>()
            .SingleInstance();
        _containerBuilder.RegisterType<PluginRoot>().As<IPluginRoot>().SingleInstance();
        _containerBuilder.RegisterInstance(this);
    }

    public RootBuilder Infrastructures(Action<IServiceCollection, IConfiguration> registrar)
    {
        var serviceCollection = new ServiceCollection();
        registrar(serviceCollection, _configurationRoot);
        
        _containerBuilder.Populate(serviceCollection);
        return this;
    }

    public RootBuilder UseLoader<TLoader>() where TLoader : PluginLoader
    {
        _containerBuilder.RegisterType<TLoader>().As<IPluginLoader>().SingleInstance();
        return this;
    }

    public IPluginRoot Build()
    {
        return _containerBuilder.Build().Resolve<IPluginRoot>();
    }
    
    public static RootBuilder Boot(Action<IConfigurationBuilder> builder)
    {
        var configurationBuilder = new ConfigurationBuilder();
        builder(configurationBuilder);
        var configurationRoot = configurationBuilder.Build();
        
        return new RootBuilder(configurationRoot);
    }

    public static RootBuilder Boot(string configFile = "appsettings.json")
    {
        return Boot(builder =>
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddCommandLine(Environment.GetCommandLineArgs())
                .AddEnvironmentVariables()
                .AddJsonFile(configFile, optional: true, reloadOnChange: true);
        });
    }
}