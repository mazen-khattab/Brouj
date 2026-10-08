namespace Brouj.Application.Common.Models;

public sealed class ApiResponse<T>
{
    public ApiResponse(T data, string? message = null)
    {
        Data = data;
        Message = message;
    }

    public bool Success => true;
    public T Data { get; }
    public string? Message { get; }
}
