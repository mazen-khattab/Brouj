using Brouj.Application.Abstractions.Authentication;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class PlainAccessTestRequest : IRequireAccess
{
    AccessLevel IRequireAccess.RequiredAccess => AccessLevel.Customer;
}
