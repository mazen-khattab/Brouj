using Brouj.Application.Behaviors;
using Brouj.Application.Features.Reservations.Strategies;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Reservation_strategies_are_registered_under_the_same_contract()
    {
        var services = new ServiceCollection().AddApplication();
        var strategies = services.Where(descriptor => descriptor.ServiceType == typeof(IReservationPreparationStrategy));
        Assert.Collection(strategies,
            regular => Assert.Equal(typeof(RegularReservationPreparationStrategy), regular.ImplementationType),
            initiative => Assert.Equal(typeof(InitiativeReservationPreparationStrategy), initiative.ImplementationType));
    }

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
                typeof(FeatureFailureBehavior<,>),
                typeof(AuthorizationBehavior<,>),
                typeof(ValidationBehavior<,>),
                typeof(CommittedEffectsBehavior<,>),
                typeof(TransactionBehavior<,>)
            ],
            behaviorImplementations);
    }
}
