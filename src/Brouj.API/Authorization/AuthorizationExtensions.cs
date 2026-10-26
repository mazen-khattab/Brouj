using Brouj.Application.Abstractions.Authentication;
using Brouj.Domain.Enums;

namespace Brouj.API.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Reuse the trusted-claim reader. Application still verifies live account state and ownership.
            options.AddPolicy(PolicyNames.CustomerOnly, policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => Actor(context).CustomerId is not null));

            options.AddPolicy(PolicyNames.StaffOnly, policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => IsStaff(Actor(context))));

            options.AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => IsStaff(Actor(context))));

            options.AddPolicy(PolicyNames.SuperAdminOnly, policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => IsStaff(Actor(context)) && Actor(context).Role == UserRole.SuperAdmin));

            options.AddPolicy(PolicyNames.MemberOnly, policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => Actor(context).CustomerId is not null || IsStaff(Actor(context))));
        });

        return services;
    }

    private static ICurrentUser Actor(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext context)
    {
        // HTTP policies never accept identity/role fields from model-bound request data.
        var http = (HttpContext)context.Resource!;

        return http.RequestServices.GetRequiredService<ICurrentUser>();
    }

    private static bool IsStaff(ICurrentUser actor) =>
        actor.IsAuthenticated && actor.UserId is not null && actor.Role is UserRole.Admin or UserRole.SuperAdmin;
}
