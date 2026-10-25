using Brouj.Application.Abstractions.Authentication;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class AccessTestRequest : IRequireAccess
{
    private readonly AccessLevel _requiredAccess;

    public AccessTestRequest(AccessLevel requiredAccess)
    {
        _requiredAccess = requiredAccess;
    }

    AccessLevel IRequireAccess.RequiredAccess => _requiredAccess;
}
