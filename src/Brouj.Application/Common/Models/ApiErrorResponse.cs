using Brouj.Application.Common.Errors;

namespace Brouj.Application.Common.Models;

public sealed class ApiErrorResponse
{
    public ApiErrorResponse(
        FeatureErrorCode code,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (code == FeatureErrorCode.None)
        {
            throw new ArgumentException(
                "An error response must contain a feature error code.",
                nameof(code));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Errors = errors;
    }

    public bool Success => false;
    public FeatureErrorCode Code { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}
