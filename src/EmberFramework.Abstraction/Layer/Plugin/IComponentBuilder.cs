using Microsoft.Extensions.DependencyInjection;

namespace EmberFramework.Abstraction.Layer.Plugin;

public interface IComponentBuilder
{
    ValueTask<IServiceCollection> BuildComponents(CancellationToken cancellationToken = default);
}
