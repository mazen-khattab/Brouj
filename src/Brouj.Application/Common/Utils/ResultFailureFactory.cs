using System.Reflection;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Models;

namespace Brouj.Application.Common.Utils;

/// <summary>Creates the request's exact Result type when a pipeline rejects it before the handler.</summary>
internal static class ResultFailureFactory<TResponse>
{
    // Cache only the typed factory. Identity, role and account state are checked on every dispatch.
    private static readonly Lazy<Func<Error, TResponse>> Factory = new(CreateFactory);

    public static TResponse Create(Error error) => Factory.Value(error);

    private static Func<Error, TResponse> CreateFactory()
    {
        if (!typeof(Result).IsAssignableFrom(typeof(TResponse)))
        {
            throw new InvalidOperationException("Protected requests must return Result or Result<T>.");
        }

        var method = typeof(TResponse).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .SingleOrDefault(method => method.Name == nameof(Result.Failure) &&
                !method.ContainsGenericParameters && method.ReturnType == typeof(TResponse) &&
                method.GetParameters() is [{ ParameterType: var parameterType }] && parameterType == typeof(Error));

        if (method is null)
        {
            throw new InvalidOperationException("The protected response type has no compatible failure factory.");
        }

        return method.CreateDelegate<Func<Error, TResponse>>();
    }
}
