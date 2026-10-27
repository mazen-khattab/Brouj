using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Orders.Commands.CompleteInitiative;
using Brouj.Application.Features.Orders.Commands.CreateFromReservation;
using Brouj.Application.Features.Orders.Errors;
using Brouj.Application.Features.Projects.Commands.Create;
using Brouj.Application.Features.Projects.Errors;
using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Application.Features.Reservations.Errors;
using Brouj.Application.Features.TimePlans.Commands.AddStageFromTemplate;
using Brouj.Application.Features.TimePlans.Commands.Create;
using Brouj.Application.Features.TimePlans.Commands.Delete;
using Brouj.Application.Features.TimePlans.Errors;
using Brouj.Application.Features.Units.Commands.Create;
using Brouj.Application.Features.Units.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Brouj.Application.Behaviors;

public sealed class FeatureFailureBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger<FeatureFailureBehavior<TRequest, TResponse>> _logger;

    public FeatureFailureBehavior(ILogger<FeatureFailureBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (PersistenceConflictException exception)
        {
            // Rollback and deferred cleanup discard finish before this outer behavior returns failure.
            // Provider details never leave Infrastructure through this safe exception.
            var error = request switch
            {
                CreateReservationCommand when exception.Conflict == PersistenceConflict.ActiveReservation =>
                    ReservationErrors.AlreadyActive,

                CreateReservationCommand when exception.Conflict == PersistenceConflict.ConcurrentWrite =>
                    ReservationErrors.Conflict("The reservation changed concurrently. Please try again."),

                CreateOrderFromReservationCommand when exception.Conflict == PersistenceConflict.ReservationOrder =>
                    OrderErrors.Conflict("The reservation already has an order."),

                CreateOrderFromReservationCommand or CompleteInitiativeOrderCommand
                    when exception.Conflict == PersistenceConflict.ConcurrentWrite =>
                    OrderErrors.Conflict("Order eligibility changed concurrently. Please try again."),

                CreateProjectCommand when exception.Conflict == PersistenceConflict.ConcurrentWrite =>
                    ProjectErrors.Conflict("The project changed concurrently. Please try again."),

                CreateUnitCommand when exception.Conflict == PersistenceConflict.ConcurrentWrite =>
                    UnitErrors.Conflict("The unit changed concurrently. Please try again."),

                CreateTimePlanCommand or DeleteTimePlanCommand or AddStageFromTemplateCommand
                    when exception.Conflict == PersistenceConflict.ConcurrentWrite =>
                    TimePlanErrors.Conflict("The time plan changed concurrently. Please try again."),

                _ => null
            };

            if (error is null || typeof(TResponse) != typeof(Result<Guid>))
            {
                throw new FeaturePersistenceException();
            }

            _logger.LogInformation("Mapped {RequestType} database conflict to {ErrorCode}.",
                typeof(TRequest).Name, error.Code);

            // These commands declare Result<Guid>; the type check keeps this generic pipeline safe.
            return (TResponse)(object)Result<Guid>.Failure(error);
        }
        catch (Exception exception) when (exception is DbUpdateException or System.Data.Common.DbException)
        {
            throw new FeaturePersistenceException();
        }
    }
}
