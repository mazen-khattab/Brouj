using Brouj.Application.Behaviors;
using FluentValidation;
using MediatR;

namespace Brouj.Application.UnitTests.Behaviors;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenRequestIsInvalid_ThrowsAndDoesNotCallNext()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).NotEmpty();
        var behavior = new ValidationBehavior<TestRequest, string>([validator]);
        var nextCalled = false;

        Task<string> Next(CancellationToken cancellationToken)
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }

        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(new TestRequest(string.Empty), Next, CancellationToken.None));

        Assert.False(nextCalled);
        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(TestRequest.Name));
    }

    [Fact]
    public async Task Handle_WhenRequestIsValid_CallsNextAndReturnsResponse()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).NotEmpty();
        var behavior = new ValidationBehavior<TestRequest, string>([validator]);

        var response = await behavior.Handle(
            new TestRequest("valid"),
            cancellationToken => Task.FromResult("handled"),
            CancellationToken.None);

        Assert.Equal("handled", response);
    }

    [Fact]
    public async Task Handle_PropagatesCancellationTokenToValidatorsAndHandler()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var expectedToken = cancellationTokenSource.Token;
        var validatorToken = CancellationToken.None;
        var handlerToken = CancellationToken.None;
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name)
            .CustomAsync((_, _, cancellationToken) =>
            {
                validatorToken = cancellationToken;
                return Task.CompletedTask;
            });
        var behavior = new ValidationBehavior<TestRequest, string>([validator]);

        await behavior.Handle(
            new TestRequest("valid"),
            cancellationToken =>
            {
                handlerToken = cancellationToken;
                return Task.FromResult("handled");
            },
            expectedToken);

        Assert.Equal(expectedToken, validatorToken);
        Assert.Equal(expectedToken, handlerToken);
    }

    [Fact]
    public async Task Handle_AwaitsEachValidatorBeforeStartingTheNextScopedOperation()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operations = new List<string>();
        var first = new InlineValidator<TestRequest>();
        first.RuleFor(request => request.Name).CustomAsync(async (_, _, token) =>
        {
            operations.Add("first-started");
            firstStarted.SetResult();
            await releaseFirst.Task.WaitAsync(token);
            operations.Add("first-completed");
        });
        var second = new InlineValidator<TestRequest>();
        second.RuleFor(request => request.Name).CustomAsync((_, _, _) =>
        {
            operations.Add("second-started");
            return Task.CompletedTask;
        });
        var behavior = new ValidationBehavior<TestRequest, string>([first, second]);

        var handling = behavior.Handle(
            new TestRequest("valid"),
            token =>
            {
                operations.Add("handler-started");
                return Task.FromResult("handled");
            },
            CancellationToken.None);

        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        try
        {
            // The gate represents an unfinished scoped database operation. No timing assertion or
            // process-local lock is needed to prove the next validator has not started alongside it.
            Assert.DoesNotContain("second-started", operations);
        }
        finally
        {
            releaseFirst.TrySetResult();
            await handling;
        }

        Assert.Equal(
            ["first-started", "first-completed", "second-started", "handler-started"],
            operations);
    }

    [Fact]
    public async Task Handle_AggregatesEachValidatorsFailuresExactlyOnce()
    {
        var first = new InlineValidator<TestRequest>();
        first.RuleFor(request => request.Name).NotEmpty().WithErrorCode("first-failure");
        var second = new InlineValidator<TestRequest>();
        second.RuleFor(request => request.Name).MinimumLength(3).WithErrorCode("second-failure");
        var behavior = new ValidationBehavior<TestRequest, string>([first, second]);
        var nextCalled = false;

        var error = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(
            new TestRequest(string.Empty),
            token =>
            {
                nextCalled = true;
                return Task.FromResult("handled");
            },
            CancellationToken.None));

        Assert.False(nextCalled);
        Assert.Equal(["first-failure", "second-failure"], error.Errors.Select(failure => failure.ErrorCode));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_PreCancelledRequestDoesNotStartValidationOrHandler(bool hasValidator)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var validatorCalled = false;
        var nextCalled = false;
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(request => request.Name).CustomAsync((_, _, _) =>
        {
            validatorCalled = true;
            return Task.CompletedTask;
        });
        var behavior = new ValidationBehavior<TestRequest, string>(hasValidator ? [validator] : []);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(
            new TestRequest("valid"),
            token =>
            {
                nextCalled = true;
                return Task.FromResult("handled");
            },
            cancellation.Token));

        Assert.False(validatorCalled);
        Assert.False(nextCalled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_CancellationStopsRemainingValidatorsAndHandler(bool hasSecondValidator)
    {
        using var cancellation = new CancellationTokenSource();
        var nextCalled = false;
        var secondCalled = false;
        var first = new InlineValidator<TestRequest>();
        first.RuleFor(request => request.Name).CustomAsync((_, _, _) =>
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        });
        var second = new InlineValidator<TestRequest>();
        second.RuleFor(request => request.Name).CustomAsync((_, _, _) =>
        {
            secondCalled = true;
            return Task.CompletedTask;
        });
        var behavior = new ValidationBehavior<TestRequest, string>(hasSecondValidator ? [first, second] : [first]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(
            new TestRequest("valid"),
            token =>
            {
                nextCalled = true;
                return Task.FromResult("handled");
            },
            cancellation.Token));

        Assert.False(secondCalled);
        Assert.False(nextCalled);
    }

    private sealed record TestRequest(string Name);
}
