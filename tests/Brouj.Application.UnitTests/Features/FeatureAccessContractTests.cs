using System.Text.Json;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Errors;
using Brouj.Application.Features.Projects.Commands.Create;
using Brouj.Application.UnitTests.Features.Support;

namespace Brouj.Application.UnitTests.Features;

public sealed class FeatureAccessContractTests
{
    // These are the approved exceptions to staff access among the protected Part 7 operations.
    private static readonly Dictionary<string, AccessLevel> SpecificRequirements = new()
    {
        ["ChangeCustomerPasswordCommand"] = AccessLevel.Customer,
        ["UpdateCustomerProfileCommand"] = AccessLevel.Customer,
        ["GetCurrentCustomerQuery"] = AccessLevel.Customer,
        ["CreateReservationCommand"] = AccessLevel.Customer,
        ["CancelReservationCommand"] = AccessLevel.Customer,
        ["GetMyReservationsQuery"] = AccessLevel.Customer,
        ["GetReservationByIdQuery"] = AccessLevel.Customer,
        ["GetMyOrdersQuery"] = AccessLevel.Customer,
        ["GetOrderByIdQuery"] = AccessLevel.Member,
        ["CreateUserCommand"] = AccessLevel.SuperAdmin,
        ["UpdateUserCommand"] = AccessLevel.SuperAdmin,
        ["SoftDeleteUserCommand"] = AccessLevel.SuperAdmin,
        ["RestoreUserCommand"] = AccessLevel.SuperAdmin,
        ["GetUsersQuery"] = AccessLevel.SuperAdmin,
        ["GetUserByIdQuery"] = AccessLevel.SuperAdmin
    };

    [Theory]
    [MemberData(nameof(FeatureArchitectureTests.ProtectedRequests), MemberType = typeof(FeatureArchitectureTests))]
    public void Each_protected_request_preserves_its_approved_fixed_requirement(Type requestType)
    {
        var request = Assert.IsAssignableFrom<IRequireAccess>(Activator.CreateInstance(requestType));
        var expected = SpecificRequirements.GetValueOrDefault(requestType.Name, AccessLevel.Staff);

        Assert.Equal(expected, request.RequiredAccess);
        Assert.DoesNotContain(requestType.GetProperties(), property => property.Name == nameof(IRequireAccess.RequiredAccess));
    }

    [Fact]
    public async Task A_client_cannot_lower_the_required_permission_through_request_JSON()
    {
        using var fixture = new FeatureHarness();
        fixture.Customer();
        var request = JsonSerializer.Deserialize<CreateProjectCommand>(
            "{\"RequiredAccess\":0,\"requiredAccess\":\"Customer\",\"Input\":{}}")!;

        var result = await fixture.SendAsync(request, CancellationToken.None);

        Assert.Equal(AccessLevel.Staff, ((IRequireAccess)request).RequiredAccess);
        Assert.Equal(ApplicationErrors.Unauthenticated, result.Error);
        Assert.DoesNotContain("RequiredAccess", JsonSerializer.Serialize(request));
        Assert.Equal(0, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task Feature_handlers_do_not_depend_on_or_call_the_permission_service()
    {
        var root = Path.Combine(RepositoryFiles.Root, "src", "Brouj.Application", "Features");

        foreach (var path in Directory.GetFiles(root, "*Handler.cs", SearchOption.AllDirectories))
        {
            var source = await File.ReadAllTextAsync(path);

            Assert.DoesNotContain("FeatureAccess", source);
            Assert.DoesNotContain("accessError", source);
        }
    }
}
