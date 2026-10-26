namespace Brouj.API.Extensions;

/// <summary>Associates a transport action with its single approved MediatR request for contract verification.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EndpointContractAttribute : Attribute
{
    public EndpointContractAttribute(Type requestType)
    {
        RequestType = requestType;
    }

    public Type RequestType { get; }
}
