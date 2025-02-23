using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Util;
using EmberFramework.Abstraction.Layer.Plugin;

namespace EmberFramework.Layer;

public static class PluginResolverExtensions
{
    
    public static AssemblyLoadContext MakeLocalAssemblyLoadContext(this PluginMetadata metadata)
    {
        var ctx = new AssemblyLoadContext(metadata.PluginFolder, isCollectible: true);
        return ctx;
    }

    public static async ValueTask InitializePluginInitializer(this ILifetimeScope scope,
        CancellationToken cancellationToken = default)
    {
        var pluginServiceProvider = new AutofacServiceProvider(scope);
        foreach (var plugin in scope.Resolve<IEnumerable<IPlugin>>())
        {
            if (plugin is IPlugin.IWithInitializer pluginWithInitializer)
            {
                await pluginWithInitializer.InitializeAsync(pluginServiceProvider, cancellationToken);
            }
        }
    }

    public static async ValueTask InitializeComponentInitializer(this ILifetimeScope scope,
        CancellationToken cancellationToken = default)
    {
        foreach (var componentInitializer in scope.Resolve<IEnumerable<IComponentInitializer>>())
        {
            await componentInitializer.InitializeAsync(cancellationToken);
        }
    }
    
    public static async ValueTask InitializePluginScope(this ILifetimeScope scope,
        CancellationToken cancellationToken = default)
    {
        await scope.InitializePluginInitializer(cancellationToken);
        await scope.InitializeComponentInitializer(cancellationToken);
    }

    public static void ReadAssembliesFrom(this AssemblyLoadContext context, PluginMetadata metadata, string searchPattern = "*.dll")
    {
        foreach (var dllFile in Directory.EnumerateFiles(metadata.PluginFolder, searchPattern, SearchOption.TopDirectoryOnly))
        {
            try
            {
                var asm = context.LoadFromAssemblyPath(dllFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

    public static IEnumerable<Type> LoadPluginTypes(this AssemblyLoadContext context)
    {
        return from assembly in context.Assemblies
            from type in assembly.GetLoadableTypes()
            where typeof(IPlugin).IsAssignableFrom(type)
            select type;
    }

    public static IEnumerable<Type> LoadPluginTypes(this PluginMetadata metadata, AssemblyLoadContext context, string searchPattern = "*.dll")
    {
        context.ReadAssembliesFrom(metadata, searchPattern);
        return context.LoadPluginTypes();
    }
    
    public static async ValueTask<ILifetimeScope> MakePluginExecuteScopeAsync(this ILifetimeScope parent,
        CancellationToken cancellationToken = default)
    {
        var pluginServices = await parent.Resolve<IEnumerable<IPlugin>>()
            .ToAsyncEnumerable()
            .SelectAwait(p => p.BuildComponents(cancellationToken))
            .ToListAsync(cancellationToken);
        
        return parent.BeginLifetimeScope((builder) =>
        {
            foreach (var serviceCollection in pluginServices)
            {
                builder.Populate(serviceCollection);
            }
        });
    }
    
    public static ILifetimeScope MakePluginRegisterScope(this ILifetimeScope parent, string searchPattern = "*.dll")
    {
        return parent.BeginLifetimeScope((builder) =>
        {
            var metadata = parent.Resolve<PluginMetadata>();
            var context = parent.Resolve<AssemblyLoadContext>();
            foreach (var type in metadata.LoadPluginTypes(context, searchPattern))
            {
                builder.RegisterType(type).As<IPlugin>().AsSelf().SingleInstance();
            }
        });
    }
}