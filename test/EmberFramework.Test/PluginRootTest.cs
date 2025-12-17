using Autofac;
using EmberFramework.Abstraction;
using EmberFramework.Abstraction.Layer;
using EmberFramework.Abstraction.Layer.Plugin;
using EmberFramework.Layer;
using EmberFramework.Test.SeparatedDummies;
using Microsoft.Extensions.Configuration;

namespace EmberFramework.Test;

public class PluginRootTest
{

    [Fact]
    public async Task TestRootShouldCallAllExitHandler()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<PluginDummyServiceInParent>().SingleInstance();
        containerBuilder.RegisterType<TestHandler>().AsSelf().As<IGracefulExitHandler>().SingleInstance();
        containerBuilder.RegisterType<ExceptionThrownExitHandler>().AsSelf().As<IGracefulExitHandler>().SingleInstance();
        containerBuilder.RegisterType<PluginRoot>().As<IPluginRoot>().SingleInstance();
        containerBuilder.RegisterInstance(new ConfigurationBuilder().AddInMemoryCollection().Build()).As<IConfiguration>().SingleInstance();
        containerBuilder.RegisterType<AssemblyPluginLoader>().As<IPluginLoader>().SingleInstance();
        var scope = containerBuilder.Build();
        
        var handlerObj = scope.Resolve<TestHandler>();
        var handlerObj2 = scope.Resolve<ExceptionThrownExitHandler>();
        var root = scope.Resolve<IPluginRoot>();
        await root.BuildScopeAsync(TestContext.Current.CancellationToken);
        var exitHandlers = await root.ResolveServiceAsync<IGracefulExitHandler>(TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);
        {
            await using var rootDisposer = root;
            
            await rootDisposer.RunAsync(TestContext.Current.CancellationToken);
            
            var executables = await root.ResolveServiceAsync<IExecutable>(TestContext.Current.CancellationToken)
                .Select(s => s as ICallRecord ?? throw new NullReferenceException())
                .ToListAsync(TestContext.Current.CancellationToken);
            
            Assert.NotEmpty(executables);
            Assert.All(executables, (e) => Assert.True(e.IsCalled));
        }
        Assert.True(handlerObj.IsCalled);
        Assert.True(handlerObj2.IsCalled);
        Assert.Contains(exitHandlers, x => x.GetType().Name == "ExitHandlerInPlugin");
    }

    [Fact]
    public void TestRootDisposeWillCallDisposeAsync()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<PluginRoot>().As<IPluginRoot>().SingleInstance();
        var scope = containerBuilder.Build();
        
        scope.Resolve<IPluginRoot>().Dispose();
    }
}