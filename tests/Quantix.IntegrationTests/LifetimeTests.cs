using Microsoft.Extensions.DependencyInjection;
using Quantix;
using Xunit;

namespace Quantix.IntegrationTests;

/// <summary>Integration tests for the dependency-injection lifetimes Quantix registers.</summary>
public class LifetimeTests
{
    [Fact]
    public void Mediator_is_registered_scoped()
    {
        ServiceProvider provider = new ServiceCollection()
            .AddQuantix()
            .BuildServiceProvider();

        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();

        IMediator firstMediator = first.ServiceProvider.GetRequiredService<IMediator>();
        IMediator firstAgain = first.ServiceProvider.GetRequiredService<IMediator>();
        IMediator secondMediator = second.ServiceProvider.GetRequiredService<IMediator>();

        Assert.Same(firstMediator, firstAgain);
        Assert.NotSame(firstMediator, secondMediator);
    }
}
