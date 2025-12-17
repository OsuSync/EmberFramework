using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using EmberFramework.Abstraction.Layer.Plugin;

namespace EmberFramework.Layer;

public class AssemblyPluginResolver(
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

    public IAsyncEnumerable<T> ResolveServiceAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (_pluginExecuteScopes is null) return AsyncEnumerable.Empty<T>();
        
        return !_pluginExecuteScopes.TryResolve<IEnumerable<T>>(out var services)
            ? AsyncEnumerable.Empty<T>()
            : services.ToAsyncEnumerable();
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await using var r = _pluginRegisterScopes;
        await using var e = _pluginExecuteScopes;
        context.Unload();
    }
}
