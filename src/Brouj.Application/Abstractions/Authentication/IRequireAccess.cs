namespace Brouj.Application.Abstractions.Authentication;

/// <summary>Declares the fixed application permission required before a request can execute.</summary>
public interface IRequireAccess
{
    AccessLevel RequiredAccess { get; }
}
