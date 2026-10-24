using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Brouj.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IReadOnlyCollection<IValidator<TRequest>> _validators = validators.ToArray();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_validators.Count == 0)
        {
            return await next(cancellationToken);
        }

        var failures = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Scoped validators may share IApplicationDbContext. Await each operation before starting
            // another validator so a request never runs parallel queries on that same DbContext.
            // A fresh ValidationContext also prevents FluentValidation's failure list being shared.
            var context = new ValidationContext<TRequest>(request);
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors.Where(failure => failure is not null));
        }

        // A validator can finish just as cancellation arrives; do not start the handler afterwards.
        cancellationToken.ThrowIfCancellationRequested();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
