using System.Xml.Linq;
using Brouj.API.Controllers;
using Brouj.Application.Common.Models;
using Brouj.Domain.Entities;

namespace Brouj.API.IntegrationTests;

/// <summary>Guards final layer boundaries and the explicit DTO boundary without changing project references.</summary>
public sealed class Part12ArchitectureTests
{
    [Theory]
    [InlineData("Brouj.Domain", "")]
    [InlineData("Brouj.Application", "Brouj.Domain")]
    [InlineData("Brouj.Infrastructure", "Brouj.Application,Brouj.Domain")]
    [InlineData("Brouj.API", "Brouj.Application,Brouj.Infrastructure")]
    public void Project_references_follow_the_approved_dependency_direction(string project, string expected)
    {
        var file = XDocument.Load(Path.Combine(FindRepositoryRoot(), "src", project, project + ".csproj"));
        var references = file.Descendants("ProjectReference")
            .Select(item => Path.GetFileNameWithoutExtension(item.Attribute("Include")!.Value.Replace('\\', '/')))
            .Order(StringComparer.Ordinal).ToArray();
        var allowed = expected.Split(',', StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(allowed, references);
        Assert.DoesNotContain(file.Descendants("PackageReference"), item =>
            item.Attribute("Include")!.Value.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.OrdinalIgnoreCase) ||
            item.Attribute("Include")!.Value.StartsWith("Microsoft.Extensions.Identity", StringComparison.OrdinalIgnoreCase));

        if (project == "Brouj.Domain")
        {
            Assert.Empty(file.Descendants("PackageReference"));
            Assert.Empty(file.Descendants("FrameworkReference"));
        }
    }

    [Fact]
    public void Domain_has_no_framework_dependencies_and_Application_does_not_expose_HttpContext()
    {
        var domainReferences = typeof(City).Assembly.GetReferencedAssemblies().Select(item => item.Name!).ToArray();

        Assert.DoesNotContain(domainReferences, name => name.StartsWith("Brouj.", StringComparison.Ordinal));
        Assert.DoesNotContain(domainReferences, name => name.StartsWith("Microsoft.", StringComparison.Ordinal));
        Assert.DoesNotContain(typeof(Result).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Public_API_response_contracts_do_not_expose_persisted_entities()
    {
        var entities = typeof(City).Assembly.GetTypes()
            .Where(type => type.Namespace == "Brouj.Domain.Entities").ToHashSet();
        var contracts = typeof(ApiControllerBase).Assembly.GetTypes().Where(type => !type.IsAbstract &&
                typeof(ApiControllerBase).IsAssignableFrom(type)).SelectMany(type => type.GetMethods())
            .SelectMany(method => method.GetCustomAttributes(
                typeof(Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute), true))
            .Cast<Microsoft.AspNetCore.Mvc.ProducesResponseTypeAttribute>().Select(item => item.Type);

        void Verify(Type type, HashSet<Type> visited)
        {
            Assert.DoesNotContain(type, entities);

            if (!visited.Add(type))
            {
                return;
            }

            if (type.IsArray)
            {
                Verify(type.GetElementType()!, visited);
            }

            foreach (var argument in type.GenericTypeArguments)
            {
                Verify(argument, visited);
            }

            if (type.Assembly == typeof(Result).Assembly)
            {
                foreach (var property in type.GetProperties())
                {
                    Verify(property.PropertyType, visited);
                }
            }
        }

        foreach (var contract in contracts)
        {
            Verify(contract, []);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Brouj.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Brouj repository root was not found.");
    }

}
