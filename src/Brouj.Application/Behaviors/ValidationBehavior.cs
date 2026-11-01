using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brouj.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>>? logger = null) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IReadOnlyCollection<IValidator<TRequest>> _validators = validators.ToArray();
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger = logger ?? NullLogger<ValidationBehavior<TRequest, TResponse>>.Instance;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestType = typeof(TRequest).Name;

        if (_validators.Count == 0)
        {
            _logger.LogDebug("No validators registered for {RequestType}; skipping validation.", requestType);
            return await next(cancellationToken);
        }

        _logger.LogDebug("Validating {RequestType} with {ValidatorCount} validator(s).", requestType, _validators.Count);

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
            var failureSummaries = failures
                .Select(failure =>
                {
                    var property = string.IsNullOrWhiteSpace(failure.PropertyName) ? "Request" : failure.PropertyName;
                    return string.IsNullOrWhiteSpace(failure.ErrorCode)
                        ? property
                        : $"{property} ({failure.ErrorCode})";
                });

            _logger.LogWarning(
                "Validation failed for {RequestType} with {FailureCount} error(s): {ValidationErrors}",
                requestType,
                failures.Count,
                string.Join(", ", failureSummaries));

            throw new ValidationException(failures);
        }

        _logger.LogDebug("Validation succeeded for {RequestType}.", requestType);

        return await next(cancellationToken);
    }
}
