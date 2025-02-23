using System.Reflection;
using System.Runtime.Loader;
using Autofac;
using EmberFramework.Abstraction.Layer.Plugin;
using EmberFramework.Layer;
using EmberFramework.Test.SeparatedDummies;

namespace EmberFramework.Test;

public class PluginResolverExtensionsTest
{
    private static ILifetimeScope MakeSingleAnyScope<T, TAlias>()
        where T : TAlias
        where TAlias : notnull
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<T>().AsSelf().As<TAlias>().SingleInstance();
        return builder.Build();
    }
    private static ILifetimeScope MakeSingleAnyScope<T>() where T : notnull => MakeSingleAnyScope<T, T>() ;
    private static ILifetimeScope MakeAnyScope<T1, T2, TAlias>()
        where T2 : TAlias
        where T1 : TAlias
        where TAlias : notnull
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<T1>().AsSelf().As<TAlias>().SingleInstance();
        builder.RegisterType<T2>().AsSelf().As<TAlias>().SingleInstance();
        return builder.Build();
    }
    
    private static ILifetimeScope MakeSinglePluginScope<T>() where T : IPlugin
    {
        return MakeSingleAnyScope<T, IPlugin>();
    }
    private static ILifetimeScope MakeManyPluginScope<T1, T2>() where T1 : IPlugin where T2 : IPlugin
    {
        return MakeAnyScope<T1, T2, IPlugin>();
    }
    
    [Fact]
    public async Task TestShouldCallPluginInitializerWhenInitialize()
    {
        await using var scope = MakeSinglePluginScope<PluginWithInitializer>();
        await scope.InitializePluginInitializer(TestContext.Current.CancellationToken);

        Assert.True(scope.Resolve<PluginWithInitializer>().IsInitializeCalled);
    }

    [Fact]
    public async Task TestInitializePluginScope()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<PluginWithInitializer>().AsSelf().As<IPlugin>().SingleInstance();
        builder.RegisterType<ComponentInitializer>().AsSelf().As<IComponentInitializer>().SingleInstance();
        
        await using var scope = builder.Build();
        await scope.InitializePluginScope(TestContext.Current.CancellationToken);
        
        Assert.True(scope.Resolve<PluginWithInitializer>().IsInitializeCalled);
        Assert.True(scope.Resolve<ComponentInitializer>().IsInitializeCalled);
    }
    
    [Fact]
    public async Task TestShouldCallComponentInitializerWhenInitialize()
    {
        await using var scope = MakeSingleAnyScope<ComponentInitializer, IComponentInitializer>();
        await scope.InitializeComponentInitializer(TestContext.Current.CancellationToken);
        
        Assert.True(scope.Resolve<ComponentInitializer>().IsInitializeCalled);
    }

    [Fact]
    public void TestWillLoadPluginTypes()
    {
        var metadata = new PluginMetadata(Environment.CurrentDirectory);
        var ctx = metadata.MakeLocalAssemblyLoadContext();
        
        var current = Assembly.GetExecutingAssembly();
        var types = metadata.LoadPluginTypes(ctx, Path.GetFileName(current.Location));

        Assert.Contains(ctx.Assemblies, a => a.FullName == current.FullName);
        Assert.Contains(types, t => t.FullName == typeof(PluginWithInitializer).FullName);
        
        ctx.Unload();
    }

    [Fact]
    public async Task TestShouldMakeExecuteScopeWillRegisterComponents()
    {
        await using var scope = MakeManyPluginScope<PluginWithoutInitializer, PluginWithoutInitializer2>();
        var execScope = await scope.MakePluginExecuteScopeAsync(TestContext.Current.CancellationToken);
        
        Assert.True(scope.TryResolve<PluginWithoutInitializer>(out _));
        Assert.True(scope.TryResolve<PluginWithoutInitializer2>(out _));
        Assert.True(execScope.TryResolve<PluginWithoutInitializer>(out _));
        Assert.True(execScope.TryResolve<PluginWithoutInitializer2>(out _));
        Assert.True(execScope.IsRegistered<PluginDummyService>());
        Assert.True(execScope.IsRegistered<PluginDummyService2>());
    }

    [Fact]
    public async Task TestShouldMakePluginRegisterScopeLoadAllPluginAndInitialize()
    {
        var metadata = new PluginMetadata(Environment.CurrentDirectory);
        var ctx = metadata.MakeLocalAssemblyLoadContext();

        var parentBuilder = new ContainerBuilder();
        parentBuilder.RegisterType<PluginDummyServiceInParent>().AsSelf().SingleInstance();
        parentBuilder.RegisterInstance(metadata).AsSelf().SingleInstance();
        parentBuilder.RegisterInstance(ctx).AsSelf().SingleInstance();
        var parent = parentBuilder.Build();
        
        var current = Assembly.GetExecutingAssembly();
        var registerScope = parent.MakePluginRegisterScope(Path.GetFileName(current.Location));
        var executeScope = await registerScope.MakePluginExecuteScopeAsync(TestContext.Current.CancellationToken);
        
        Assert.Contains(ctx.Assemblies, a => a.FullName == current.FullName);
        Assert.Contains(registerScope.ComponentRegistry.Registrations,
            r => r.Activator.LimitType.Name == nameof(PluginWithoutInitializer));
        Assert.Contains(registerScope.ComponentRegistry.Registrations,
            r => r.Activator.LimitType.Name == nameof(PluginWithoutInitializer2));
        Assert.Contains(registerScope.ComponentRegistry.Registrations,
            r => r.Activator.LimitType.Name == nameof(PluginWithoutInitializerButReferenceServiceInParent));

        var pluginObj = executeScope.Resolve(ctx.LoadPluginTypes()
            .First(t => t.Name == nameof(PluginWithoutInitializerButReferenceServiceInParent)))!;
        
        var result = pluginObj.GetType().GetProperty("ParentService")!.GetMethod?.Invoke(pluginObj, []);
        
        Assert.Equivalent(parent.Resolve<PluginDummyServiceInParent>(), result);
    }

    [Fact]
    public void TestWillDebugPrintIfLoadBadImage()
    {
        
        var metadata = new PluginMetadata(Environment.CurrentDirectory);
        var ctx = metadata.MakeLocalAssemblyLoadContext();
        
        var types = metadata.LoadPluginTypes(ctx, "xunit.runner.json");
        
        Assert.Empty(types);
        Assert.Empty(ctx.Assemblies);
    }
}