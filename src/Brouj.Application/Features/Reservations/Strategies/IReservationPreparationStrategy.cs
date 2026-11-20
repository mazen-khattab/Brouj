using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations.Commands.Create;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;

namespace Brouj.Application.Features.Reservations.Strategies;

public interface IReservationPreparationStrategy
{
    ProjectType Type { get; }
    Task<Result> PrepareAsync(Reservation reservation, CreateReservationCommand request, CancellationToken cancellationToken);
}
