using Brouj.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersPipelineBehaviorsInExpectedOrder()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddApplication();

        var behaviorImplementations = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descriptor => descriptor.ImplementationType)
            .ToArray();

        Assert.Equal(
            [
                typeof(LoggingBehavior<,>),
                typeof(ValidationBehavior<,>),
                typeof(TransactionBehavior<,>)
            ],
            behaviorImplementations);
    }
}
