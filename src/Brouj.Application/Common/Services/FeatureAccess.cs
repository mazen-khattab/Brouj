using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Common.Errors;
using Brouj.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Brouj.Application.Common.Services;

/// <summary>Checks current database account state once per protected dispatch through AuthorizationBehavior.</summary>
public sealed class FeatureAccess
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _actor;

    public FeatureAccess(IApplicationDbContext db, ICurrentUser actor)
    {
        _db = db;
        _actor = actor;
    }

    private Guid CustomerId => _actor.CustomerId ?? Guid.Empty;

    private Guid UserId => _actor.UserId ?? Guid.Empty;

    public Task<Error?> CheckAsync(AccessLevel requiredAccess, CancellationToken cancellationToken) =>
        requiredAccess switch
        {
            AccessLevel.Customer => CustomerAsync(cancellationToken),
            AccessLevel.Staff => StaffAsync(cancellationToken),
            AccessLevel.SuperAdmin => SuperAdminAsync(cancellationToken),
            AccessLevel.Member => MemberAsync(cancellationToken),
            // Unknown requirements must never turn a protected request into a public one.
            _ => Task.FromResult<Error?>(ApplicationErrors.Forbidden)
        };

    private async Task<Error?> CustomerAsync(CancellationToken cancellationToken)
    {
        if (!_actor.IsAuthenticated || CustomerId == Guid.Empty || _actor.UserId is not null)
        {
            return ApplicationErrors.Unauthenticated;
        }

        // Claims identify the actor; the database remains authoritative for deleted accounts.
        var active = await _db.Customers.AsNoTracking()
            .AnyAsync(customer => customer.Id == CustomerId && customer.DeletedAt == null, cancellationToken);

        return active ? null : ApplicationErrors.Unauthenticated;
    }

    private Task<Error?> StaffAsync(CancellationToken cancellationToken) => StaffAsync(false, cancellationToken);

    private Task<Error?> SuperAdminAsync(CancellationToken cancellationToken) => StaffAsync(true, cancellationToken);

    private Task<Error?> MemberAsync(CancellationToken cancellationToken) =>
        _actor.CustomerId is not null ? CustomerAsync(cancellationToken) : StaffAsync(cancellationToken);

    private async Task<Error?> StaffAsync(bool superAdmin, CancellationToken cancellationToken)
    {
        if (!_actor.IsAuthenticated || UserId == Guid.Empty || _actor.CustomerId is not null)
        {
            return ApplicationErrors.Unauthenticated;
        }

        var role = await _db.Users.AsNoTracking()
            .Where(user => user.Id == UserId && user.DeletedAt == null)
            .Select(user => (UserRole?)user.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (role is null || !Enum.IsDefined(role.Value) || role != _actor.Role)
        {
            return ApplicationErrors.Forbidden;
        }

        if (superAdmin && role != UserRole.SuperAdmin)
        {
            return ApplicationErrors.Forbidden;
        }

        return null;
    }
}
