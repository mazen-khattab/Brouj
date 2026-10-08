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

    private sealed record TestRequest(string Name);
}
