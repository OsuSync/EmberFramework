using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using EmberFramework.Abstraction.Layer.Plugin;

namespace EmberFramework.Layer;

public class TypedPluginResolver(ILifetimeScope parent, Type target) : IPluginResolver
{
    private ILifetimeScope? _pluginRegisterScopes;
    private ILifetimeScope? _pluginExecuteScopes;
    
    public async ValueTask BuildScopeAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        _pluginRegisterScopes = parent.BeginLifetimeScope((builder) =>
        {
            builder.RegisterType(target).As<IPlugin>().SingleInstance();
        });
        _pluginExecuteScopes = await _pluginRegisterScopes.MakePluginExecuteScopeAsync(cancellationToken);
        await _pluginExecuteScopes.InitializePluginScope(cancellationToken);
    }

    public IAsyncEnumerable<T> ResolveServiceAsync<T>(CancellationToken cancellationToken = new CancellationToken()) where T : class
    {
        if (_pluginExecuteScopes is null) return AsyncEnumerable.Empty<T>();
        
        return !_pluginExecuteScopes.TryResolve<IEnumerable<T>>(out var services)
            ? AsyncEnumerable.Empty<T>()
            : services.ToAsyncEnumerable();
    }

    public void Dispose()
    {
        _pluginRegisterScopes?.Dispose();
        _pluginExecuteScopes?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_pluginRegisterScopes != null) await _pluginRegisterScopes.DisposeAsync();
        if (_pluginExecuteScopes != null) await _pluginExecuteScopes.DisposeAsync();
    }
}