namespace Brouj.Application.Common.Errors;

public sealed record Error
{
    public static readonly Error None = new(
        FeatureErrorCode.None,
        string.Empty,
        ErrorType.None);

    private Error(
        FeatureErrorCode code,
        string message,
        ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    public FeatureErrorCode Code { get; }

    public string Message { get; }

    public ErrorType Type { get; }

    public static Error Validation(
        FeatureErrorCode code,
        string message) =>
        Create(code, message, ErrorType.Validation);

    public static Error Unauthorized(
        FeatureErrorCode code,
        string message) =>
        Create(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(
        FeatureErrorCode code,
        string message) =>
        Create(code, message, ErrorType.Forbidden);

    public static Error NotFound(
        FeatureErrorCode code,
        string message) =>
        Create(code, message, ErrorType.NotFound);

    public static Error Conflict(
        FeatureErrorCode code,
        string message) =>
        Create(code, message, ErrorType.Conflict);

    public static Error Failure(
        FeatureErrorCode code,
        string message) =>
        Create(code, message, ErrorType.Failure);

    private static Error Create(
        FeatureErrorCode code,
        string message,
        ErrorType type)
    {
        if (code == FeatureErrorCode.None)
        {
            throw new ArgumentException(
                "A failed error must contain a feature error code.",
                nameof(code));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new Error(code, message, type);
    }
}
