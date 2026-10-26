namespace Brouj.API.IntegrationTests.Support;

public sealed class EndpointCase
{
    public required string Method { get; init; }
    public required string Route { get; init; }
    public required string Request { get; init; }
    public required string RequestNamespace { get; init; }
    public string? Body { get; init; }
    public string? Query { get; init; }
    public required string Response { get; init; }
    public required string Policy { get; init; }
    public required string Ownership { get; init; }
    public int SuccessStatus { get; init; }
    public required string Outcomes { get; init; }
    public required string RateLimit { get; init; }
    public required string Cache { get; init; }
}
