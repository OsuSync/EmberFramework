using System.Runtime.Loader;
using Autofac;
using EmberFramework.Abstraction.Layer.Plugin;

namespace EmberFramework.Layer;

public class PluginResolver(
    ILifetimeScope parent,
    AssemblyLoadContext context) : IPluginResolver
{
    private ILifetimeScope? _pluginRegisterScopes;
    private ILifetimeScope? _pluginExecuteScopes;


    public async ValueTask BuildScopeAsync(CancellationToken cancellationToken = default)
    {
        _pluginRegisterScopes = parent.MakePluginRegisterScope();
        _pluginExecuteScopes = await _pluginRegisterScopes.MakePluginExecuteScopeAsync(cancellationToken);

        await _pluginExecuteScopes.InitializePluginScope(cancellationToken);
    }

    public IAsyncEnumerable<T> ResolveServiceAsync<T>(CancellationToken cancellationToken = default) where T : notnull
    {
        if (_pluginExecuteScopes is null) return AsyncEnumerable.Empty<T>();
        
        var service = _pluginExecuteScopes.Resolve<T>();

        return AsyncEnumerable.Repeat(service, 1);
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        using var r = _pluginRegisterScopes;
        using var e = _pluginExecuteScopes;
        context.Unload();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await using var r = _pluginRegisterScopes;
        await using var e = _pluginExecuteScopes;
        context.Unload();
    }
}
