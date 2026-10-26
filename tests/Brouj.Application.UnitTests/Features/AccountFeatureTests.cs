using Brouj.Application.Common.DTOs;
using Brouj.Application.Common.Errors;
using Brouj.Application.Common.Utils;
using Brouj.Application.Features.Customers.Commands.ChangePassword;
using Brouj.Application.Features.Customers.Commands.Login;
using Brouj.Application.Features.Customers.Commands.Logout;
using Brouj.Application.Features.Customers.Commands.RefreshSession;
using Brouj.Application.Features.Customers.Commands.Register;
using Brouj.Application.Features.Customers.Commands.Restore;
using Brouj.Application.Features.Customers.Commands.SoftDelete;
using Brouj.Application.Features.Customers.Commands.UpdateProfile;
using Brouj.Application.Features.Customers.DTOs;
using Brouj.Application.Features.Customers.Queries.GetAll;
using Brouj.Application.Features.Customers.Queries.GetById;
using Brouj.Application.Features.Users.Commands.ChangePassword;
using Brouj.Application.Features.Users.Commands.Create;
using Brouj.Application.Features.Users.Commands.Login;
using Brouj.Application.Features.Users.Commands.Logout;
using Brouj.Application.Features.Users.Commands.RefreshSession;
using Brouj.Application.Features.Users.Commands.Restore;
using Brouj.Application.Features.Users.Commands.SoftDelete;
using Brouj.Application.Features.Users.Commands.Update;
using Brouj.Application.Features.Users.DTOs;
using Brouj.Application.Features.Users.Queries.GetCurrent;
using Brouj.Application.UnitTests.Features.Support;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Features;

public sealed class AccountFeatureTests
{
    [Fact]
    public async Task Registration_normalizes_email_hashes_password_and_establishes_a_session()
    {
        using var fixture = new FeatureHarness();
        var command = new RegisterCustomerCommand
        {
            Input = new RegisterCustomerInput
            {
                FName = " First ",
                LName = "Last",
                Email = " OWNER@Example.Test ",
                Phone = "01012345678",
                IdentityNumber = "12345678901234",
                Password = "        "
            }
        };
        var validation = await new RegisterCustomerCommandValidator().ValidateAsync(command);

        Assert.True(validation.IsValid);

        var result = await fixture.SendAsync(command, CancellationToken.None);
        var customer = Assert.Single(fixture.Db.Customers);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.ActorId);
        Assert.Equal("owner@example.test", customer.Email);
        Assert.Equal("First", customer.FName);
        Assert.Equal("synthetic-hash:        ", customer.HashPassword);
        Assert.NotNull(fixture.Session.Written);
    }

    [Fact]
    public async Task Duplicate_registration_does_not_write_or_start_a_session()
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        var command = new RegisterCustomerCommand
        {
            Input = new RegisterCustomerInput
            {
                FName = "Other",
                LName = "Person",
                Email = customer.Email,
                Phone = "01112345678",
                IdentityNumber = "99999999999999",
                Password = "password"
            }
        };

        var result = await fixture.SendAsync(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(0, fixture.Db.SaveCount);
        Assert.Null(fixture.Session.Written);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Unknown_wrong_password_and_deleted_logins_share_the_same_error(bool deleted, bool missing)
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        customer.DeletedAt = deleted ? fixture.Clock.GetUtcNow() : null;
        var command = new LoginCustomerCommand
        {
            Input = new LoginInput { Email = missing ? "missing@example.test" : customer.Email, Password = "wrong" }
        };

        var result = await fixture.SendAsync(command, CancellationToken.None);

        Assert.Equal(ApplicationErrors.InvalidCredentials, result.Error);
        Assert.Null(fixture.Session.Written);
    }

    [Fact]
    public async Task Valid_customer_login_creates_the_cookie_session_without_returning_credentials()
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        var result = await fixture.SendAsync(new LoginCustomerCommand
        {
            Input = new LoginInput { Email = " CUSTOMER@EXAMPLE.TEST ", Password = "password" }
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(customer.Id, result.Value.ActorId);
        Assert.NotNull(fixture.Session.Written);
        Assert.Equal(new[] { "ActorId", "ExpiresAt" }, typeof(AuthSessionDto).GetProperties().Select(p => p.Name).Order());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Refresh_handles_service_acceptance_and_rejection(bool reject)
    {
        using var fixture = new FeatureHarness();
        fixture.Customer();
        fixture.Session.Token = "synthetic-presented-token";
        fixture.Refresh.Reject = reject;

        var result = await fixture.SendAsync(new RefreshCustomerSessionCommand(), CancellationToken.None);

        Assert.Equal(!reject, result.IsSuccess);
        Assert.Equal(!reject, fixture.Session.Written is not null);
    }

    [Fact]
    public async Task Logout_revokes_the_presented_refresh_and_clears_cookies()
    {
        using var fixture = new FeatureHarness();
        fixture.Session.Token = "synthetic-presented-token";

        var result = await fixture.SendAsync(new LogoutCustomerCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(fixture.Refresh.Revoked);
        Assert.True(fixture.Session.Cleared);
    }

    [Fact]
    public async Task Profile_and_password_changes_only_affect_the_current_customer_and_keep_the_session()
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        var other = new Customer { Id = Guid.NewGuid(), Email = "other@example.test", HashPassword = "unchanged" };
        fixture.Db.Customers.Add(other);

        var profile = await fixture.SendAsync(
            new UpdateCustomerProfileCommand
            {
                Input = new CustomerProfileInput
                {
                    FName = "New",
                    LName = "Name",
                    Phone = customer.Phone,
                    IdentityNumber = customer.IdentityNumber,
                    Email = " NEW@EXAMPLE.TEST "
                }
            }, CancellationToken.None);
        var invalid = await fixture.SendAsync(
            new ChangeCustomerPasswordCommand
            {
                Input = new PasswordChangeInput { CurrentPassword = "wrong", NewPassword = "new pass" }
            }, CancellationToken.None);
        var valid = await fixture.SendAsync(
            new ChangeCustomerPasswordCommand
            {
                Input = new PasswordChangeInput { CurrentPassword = "password", NewPassword = "new pass" }
            }, CancellationToken.None);

        Assert.True(profile.IsSuccess);
        Assert.True(invalid.IsFailure);
        Assert.True(valid.IsSuccess);
        Assert.Equal("new@example.test", customer.Email);
        Assert.Equal("synthetic-hash:new pass", customer.HashPassword);
        Assert.Equal("unchanged", other.HashPassword);
        Assert.Null(fixture.Session.Written);
        Assert.False(fixture.Session.Cleared);
        Assert.False(fixture.Refresh.Revoked);
    }

    [Fact]
    public async Task Staff_login_refresh_logout_and_password_change_use_the_staff_session_boundary()
    {
        using var fixture = new FeatureHarness();
        var user = fixture.Staff(UserRole.Admin);
        user.HashPassword = new FeatureHasher().Hash("password");
        fixture.Refresh.ActorId = user.Id;
        fixture.Session.Token = "synthetic-staff";

        var login = await fixture.SendAsync(new LoginUserCommand
        {
            Input = new LoginInput { Email = user.Email, Password = "password" }
        }, CancellationToken.None);
        var refresh = await fixture.SendAsync(new RefreshUserSessionCommand(), CancellationToken.None);
        var password = await fixture.SendAsync(new ChangeUserPasswordCommand
        {
            Input = new PasswordChangeInput { CurrentPassword = "password", NewPassword = "changed!" }
        }, CancellationToken.None);
        var logout = await fixture.SendAsync(new LogoutUserCommand(), CancellationToken.None);

        Assert.True(login.IsSuccess && refresh.IsSuccess && password.IsSuccess && logout.IsSuccess);
        Assert.Equal("synthetic-hash:changed!", user.HashPassword);
        Assert.True(fixture.Refresh.Revoked && fixture.Session.Cleared);
    }

    [Fact]
    public async Task Admin_cannot_manage_users_and_stale_or_deleted_staff_claims_are_rejected()
    {
        using var fixture = new FeatureHarness();
        var staff = fixture.Staff(UserRole.Admin);
        var command = new CreateUserCommand
        {
            Input = new CreateUserInput { Name = "New", Email = "new@example.test", Phone = "01112345678", Password = "password" }
        };
        var denied = await fixture.SendAsync(command, CancellationToken.None);

        fixture.Actor.Role = UserRole.SuperAdmin;
        var stale = await fixture.SendAsync(new GetCurrentUserQuery(), CancellationToken.None);
        fixture.Actor.Role = staff.Role;
        staff.DeletedAt = fixture.Clock.GetUtcNow();
        var deleted = await fixture.SendAsync(new GetCurrentUserQuery(), CancellationToken.None);

        Assert.True(denied.IsFailure && stale.IsFailure && deleted.IsFailure);
        Assert.Equal(0, fixture.Db.SaveCount);
    }

    [Fact]
    public async Task SuperAdmin_can_manage_another_user_but_cannot_delete_or_demote_self()
    {
        using var fixture = new FeatureHarness();
        var self = fixture.Staff();
        var created = await fixture.SendAsync(new CreateUserCommand
        {
            Input = new CreateUserInput
            {
                Name = "Other",
                Email = "other@example.test",
                Phone = "01112345678",
                Role = UserRole.Admin,
                Password = "password"
            }
        }, CancellationToken.None);
        var deletion = await fixture.SendAsync(new SoftDeleteUserCommand { Id = created.Value }, CancellationToken.None);
        var restored = await fixture.SendAsync(new RestoreUserCommand { Id = created.Value }, CancellationToken.None);
        var selfDelete = await fixture.SendAsync(new SoftDeleteUserCommand { Id = self.Id }, CancellationToken.None);
        var demotion = await fixture.SendAsync(new UpdateUserCommand
        {
            Id = self.Id,
            Input = new UpdateUserInput
            {
                Name = self.Name,
                Email = self.Email,
                Phone = self.Phone,
                Role = UserRole.Admin
            }
        }, CancellationToken.None);

        Assert.True(created.IsSuccess && deletion.IsSuccess && restored.IsSuccess);
        Assert.True(selfDelete.IsFailure && demotion.IsFailure);
        Assert.Equal(UserRole.SuperAdmin, self.Role);
    }

    [Fact]
    public async Task Customer_administration_supports_paging_soft_delete_restore_and_not_found()
    {
        using var fixture = new FeatureHarness();
        var customer = fixture.Customer();
        fixture.Staff();
        var deleted = await fixture.SendAsync(new SoftDeleteCustomerCommand { Id = customer.Id }, CancellationToken.None);
        var page = await fixture.SendAsync(new GetCustomersQuery { IsDeleted = true, Search = "First", PageSize = 1 }, CancellationToken.None);
        var restored = await fixture.SendAsync(new RestoreCustomerCommand { Id = customer.Id }, CancellationToken.None);
        var missing = await fixture.SendAsync(new GetCustomerByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        Assert.True(deleted.IsSuccess && restored.IsSuccess);
        Assert.Single(page.Value.Items);
        Assert.Null(customer.DeletedAt);
        Assert.True(missing.IsFailure);
    }
}
