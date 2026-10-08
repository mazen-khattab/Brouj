using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;
using Brouj.Application.Features.Reservations;

namespace Brouj.Application.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResultWithoutError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result.Success("value");

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void GenericFailure_ExposesErrorAndRejectsValueAccess()
    {
        var error = ReservationErrors.AlreadyActive;
        var result = Result.Failure<string>(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_RejectsNoError()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
        Assert.Throws<ArgumentException>(() => Result.Failure<string>(Error.None));
    }
}
