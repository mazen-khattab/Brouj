using System.Reflection;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;

namespace Brouj.API.IntegrationTests.Support;

/// <summary>Records controller dispatch only. It is never evidence of Application or SQL correctness.</summary>
public class RecordingSender : DispatchProxy
{
    public object? LastRequest { get; set; }
    public Error? Error { get; set; }
    public Exception? Exception { get; set; }
    public CancellationToken LastCancellationToken { get; private set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        LastRequest = args![0];
        LastCancellationToken = (CancellationToken)args[1]!;

        if (Exception is not null)
        {
            throw Exception;
        }

        var responseType = targetMethod!.ReturnType.GenericTypeArguments[0];
        var valueType = responseType.GenericTypeArguments[0];
        var factory = responseType.GetMethod(Error is null ? "Success" : "Failure",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
        var result = factory.Invoke(null, [Error is null ? Sample(valueType) : Error]);

        return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(responseType).Invoke(null, [result]);
    }

    private static object? Sample(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PaginationResult<>))
        {
            var items = Array.CreateInstance(type.GenericTypeArguments[0], 0);

            return Activator.CreateInstance(type, items, 1, 20, 0, 100);
        }

        if (type.IsInterface && type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
             type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
             type.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)))
        {
            return Array.CreateInstance(type.GenericTypeArguments[0], 0);
        }

        return Activator.CreateInstance(type);
    }
}
