namespace Brouj.API.Requests;

/// <summary>Explicit HTTP body; route identifiers and access requirements are bound separately.</summary>
public sealed class CreateReservationRequest
{
    public Guid UnitId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? AgeGroup { get; init; }
    public string? IdentityNumber { get; init; }
    public string? PlaceOfResidence { get; init; }
    public int? PreferredFinancialBracket { get; set; }
    public int? PreferredRepaymentPeriod { get; set; }
}
