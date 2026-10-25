namespace Brouj.Application.Common.Attributes;


[AttributeUsage(AttributeTargets.Class)]
public sealed class InvalidatesCacheAttribute : Attribute
{
    public InvalidatesCacheAttribute(params string[] resources)
    {
        Resources = resources;
    }

    public IReadOnlyList<string> Resources { get; }
}
