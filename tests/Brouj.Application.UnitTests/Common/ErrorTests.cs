using Brouj.Application.Common.Errors;

namespace Brouj.Application.UnitTests.Common;

public sealed class ErrorTests
{
    [Fact]
    public void Factory_RejectsFeatureErrorCodeNone()
    {
        Assert.Throws<ArgumentException>(() =>
            Error.NotFound(FeatureErrorCode.None, "Safe message."));
    }
}
