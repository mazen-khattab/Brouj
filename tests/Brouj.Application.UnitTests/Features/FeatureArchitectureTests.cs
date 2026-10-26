using System.Text.RegularExpressions;
using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Common.Models;
using Brouj.Application.UnitTests.Features.Support;
using FluentValidation;
using MediatR;

namespace Brouj.Application.UnitTests.Features;

public sealed class FeatureArchitectureTests
{
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Result).Assembly;

    public static IEnumerable<object[]> Requests => ApplicationAssembly.GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract && type.GetInterfaces()
            .Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)))
        .Select(type => new object[] { type });

    public static IEnumerable<object[]> ProtectedRequests => Requests.Where(row => !PublicRequests.Contains(((Type)row[0]).Name));

    private static readonly HashSet<string> PublicRequests =
    [
        "RegisterCustomerCommand", "LoginCustomerCommand", "RefreshCustomerSessionCommand", "LogoutCustomerCommand",
        "LoginUserCommand", "RefreshUserSessionCommand", "LogoutUserCommand", "GetCitiesQuery", "GetCityByIdQuery",
        "GetAreasQuery", "GetAreasByCityQuery", "GetAreaByIdQuery", "GetProjectsQuery", "GetProjectByIdQuery",
        "GetUnitsByProjectQuery", "GetUnitByIdQuery", "GetActiveTermsAndConditionsQuery"
    ];

    [Fact]
    public async Task Every_feature_and_shared_feature_support_file_contains_exactly_one_named_type()
    {
        var app = Path.Combine(RepositoryFiles.Root, "src", "Brouj.Application");
        var folders = new[] { "Features", "Common/DTOs", "Common/Services", "Common/Utils",
            "Common/Attributes", "Common/Notifications" };

        foreach (var folder in folders)
        {
            foreach (var path in Directory.GetFiles(Path.Combine(app, folder), "*.cs", SearchOption.AllDirectories))
            {
                var source = await File.ReadAllTextAsync(path);
                var declarations = Regex.Matches(source,
                    @"^\s*(?:public|internal|private|protected)\s+(?:(?:sealed|static|abstract|partial)\s+)*(?:class|record|interface|enum)\s+(\w+)",
                    RegexOptions.Multiline);

                Assert.Single(declarations);
                Assert.Equal(Path.GetFileNameWithoutExtension(path), declarations[0].Groups[1].Value);
            }
        }
    }

    [Fact]
    public async Task Every_planned_request_exists_without_adding_future_operations()
    {
        var plan = await File.ReadAllTextAsync(Path.Combine(
            RepositoryFiles.Root, "brouj_backend_plan_v2", "07_PART_7_APPLICATION_FEATURES.md"));
        var expected = Regex.Matches(plan, @"\b[A-Z][A-Za-z]+(?:Command|Query)\b")
            .Select(match => match.Value).Distinct().Order().ToArray();
        var actual = Requests.Select(row => ((Type)row[0]).Name).Order().ToArray();

        Assert.Equal(101, actual.Length);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task Every_operation_has_one_request_one_validator_and_one_handler(Type requestType)
    {
        var validatorType = ApplicationAssembly.GetType(requestType.FullName + "Validator")!;
        var handlerType = ApplicationAssembly.GetType(requestType.FullName + "Handler")!;

        Assert.NotNull(validatorType);
        Assert.NotNull(handlerType);
        Assert.Contains(typeof(IValidator<>).MakeGenericType(requestType), validatorType.GetInterfaces());
        Assert.Single(handlerType.GetInterfaces(), contract =>
            contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

        var relative = requestType.Namespace!.Replace("Brouj.Application.", "").Replace('.', Path.DirectorySeparatorChar);
        var folder = Path.Combine(RepositoryFiles.Root, "src", "Brouj.Application", relative);
        var names = Directory.GetFiles(folder, "*.cs").Select(Path.GetFileName).Order().ToArray();

        Assert.Equal(new[] { requestType.Name + ".cs", requestType.Name + "Handler.cs",
            requestType.Name + "Validator.cs" }.Order(), names);

        foreach (var path in Directory.GetFiles(folder, "*.cs"))
        {
            var source = await File.ReadAllTextAsync(path);
            var declarations = Regex.Matches(source,
                @"^\s*public\s+(?:(?:sealed|static|abstract)\s+)?(?:class|record|interface|enum)\s+(\w+)",
                RegexOptions.Multiline);

            Assert.Single(declarations);
            Assert.Equal(Path.GetFileNameWithoutExtension(path), declarations[0].Groups[1].Value);
        }
    }

    [Theory]
    [MemberData(nameof(ProtectedRequests))]
    public async Task Protected_requests_reject_anonymous_actors_before_validation_or_writing(Type requestType)
    {
        using var fixture = new FeatureHarness();
        var result = await fixture.ExecuteAsync(Activator.CreateInstance(requestType)!);

        Assert.True(result.IsFailure);
        Assert.Equal(0, fixture.Db.SaveCount);
        Assert.Equal(0, fixture.Transactions.ExecutionCount);
        Assert.Empty(fixture.Files.Deleted);
    }

    [Theory]
    [MemberData(nameof(Requests))]
    public void Public_and_protected_requests_declare_the_expected_authorization_boundary(Type requestType)
    {
        Assert.Equal(!PublicRequests.Contains(requestType.Name), typeof(IRequireAccess).IsAssignableFrom(requestType));
    }

    [Fact]
    public async Task Feature_dtos_and_errors_are_scoped_and_common_dtos_are_actually_shared()
    {
        var app = Path.Combine(RepositoryFiles.Root, "src", "Brouj.Application");
        var featureFiles = Directory.GetFiles(Path.Combine(app, "Features"), "*.cs", SearchOption.AllDirectories);
        var sources = new Dictionary<string, string>();

        foreach (var path in featureFiles)
        {
            var source = await File.ReadAllTextAsync(path);
            sources[path] = source;

            if (path.EndsWith("Dto.cs") || path.EndsWith("Input.cs") ||
                path.EndsWith("DtoValidator.cs") || path.EndsWith("InputValidator.cs"))
            {
                Assert.Equal("DTOs", Path.GetFileName(Path.GetDirectoryName(path)));
            }

            if (path.EndsWith("Errors.cs"))
            {
                Assert.Equal("Errors", Path.GetFileName(Path.GetDirectoryName(path)));
            }

            Assert.DoesNotContain("namespace Brouj.Application.Features.Common", source);
        }

        foreach (var path in Directory.GetFiles(Path.Combine(app, "Common", "DTOs"), "*.cs")
            .Where(path => !path.EndsWith("Validator.cs")))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var features = sources.Where(pair => Regex.IsMatch(pair.Value, @"\b" + name + @"\b"))
                .Select(pair => Path.GetRelativePath(Path.Combine(app, "Features"), pair.Key).Split(Path.DirectorySeparatorChar)[0])
                .Distinct().ToArray();

            Assert.True(features.Length >= 2, name + " must be shared by at least two features.");
        }

        Assert.True(File.Exists(Path.Combine(app, "Common", "Services", "PendingFileCleanup.cs")));
    }
}
