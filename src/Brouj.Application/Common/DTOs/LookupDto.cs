namespace Brouj.Application.Common.DTOs;

/// <summary>
/// Base Data Transfer Object for lightweight lookup items containing an identifier and name.
/// Designed for inheritance by specialized lookup DTOs requiring additional columns.
/// </summary>
public class LookupDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public LookupDto()
    {
    }

    public LookupDto(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
}
