using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Behaviors;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.Commands.CompleteInitiative;
using Brouj.Application.Features.Orders.Commands.CreateFromReservation;
using Brouj.Application.Features.Projects.Commands.Create;
using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Application.Features.TimePlans.Commands.AddStageFromTemplate;
using Brouj.Application.Features.TimePlans.Commands.Create;
using Brouj.Application.Features.TimePlans.Commands.Delete;
using Brouj.Application.Features.Units.Commands.Create;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brouj.Application.UnitTests.Features;

public sealed class Part9ConflictMappingTests
{
    public static TheoryData<object, FeatureErrorCode> Commands => new()
    {
        { new CreateReservationCommand(), FeatureErrorCode.ReservationConflict },
        { new CreateOrderFromReservationCommand(), FeatureErrorCode.OrderConflict },
        { new CompleteInitiativeOrderCommand(), FeatureErrorCode.OrderConflict },
        { new CreateProjectCommand(), FeatureErrorCode.ProjectConflict },
        { new CreateUnitCommand(), FeatureErrorCode.UnitConflict },
        { new CreateTimePlanCommand(), FeatureErrorCode.TimePlanConflict },
        { new DeleteTimePlanCommand(), FeatureErrorCode.TimePlanConflict },
        { new AddStageFromTemplateCommand(), FeatureErrorCode.TimePlanConflict }
    };

    [Theory]
    [MemberData(nameof(Commands))]
    public async Task Concurrent_writes_map_to_the_commands_business_conflict(object command, FeatureErrorCode code)
    {
        var behavior = new FeatureFailureBehavior<object, Result<Guid>>(
            NullLogger<FeatureFailureBehavior<object, Result<Guid>>>.Instance);
        var result = await behavior.Handle(command,
            _ => throw new PersistenceConflictException(PersistenceConflict.ConcurrentWrite), CancellationToken.None);

        Assert.Equal(code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Unrelated_constraint_category_is_not_mapped_to_a_misleading_business_conflict()
    {
        var behavior = new FeatureFailureBehavior<CreateReservationCommand, Result<Guid>>(
            NullLogger<FeatureFailureBehavior<CreateReservationCommand, Result<Guid>>>.Instance);
        var error = await Assert.ThrowsAsync<FeaturePersistenceException>(() => behavior.Handle(
            new CreateReservationCommand(),
            _ => throw new PersistenceConflictException(PersistenceConflict.ReservationOrder), CancellationToken.None));

        Assert.Null(error.InnerException);
    }
}
