using Brouj.Application.Abstractions.Authentication;
using Brouj.Application.Abstractions.Persistence;
using Brouj.Application.Abstractions.Storage;
using Brouj.Application.Common.Models;
using Brouj.Domain.Entities;
using Brouj.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application.UnitTests.Features.Support;

internal sealed class FeatureHarness : IDisposable
{
    public FeatureTestDb Db { get; } = new();

    public FeatureActor Actor { get; } = new();

    public FeatureClock Clock { get; } = new();

    public FeatureRefresh Refresh { get; } = new();

    public FeatureSession Session { get; } = new();

    public FeatureFiles Files { get; } = new();

    public FeatureTransactionManager Transactions { get; } = new();

    public ServiceProvider Services { get; }

    public FeatureHarness()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IApplicationDbContext>(Db);
        services.AddSingleton<ICurrentUser>(Actor);
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IPasswordHasher, FeatureHasher>();
        services.AddSingleton<IJwtTokenService, FeatureJwt>();
        services.AddSingleton<IRefreshTokenService>(Refresh);
        services.AddSingleton<IAuthenticationSession>(Session);
        services.AddSingleton<IFileStorageService>(Files);
        services.AddSingleton<IApplicationTransactionManager>(Transactions);
        Services = services.BuildServiceProvider();
    }

    public User Staff(UserRole role = UserRole.SuperAdmin)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Role = role,
            Name = "Staff",
            Email = "staff@example.test",
            Phone = "01012345678"
        };
        Db.Users.Add(user);
        Actor.IsAuthenticated = true;
        Actor.UserId = user.Id;
        Actor.CustomerId = null;
        Actor.Role = role;

        return user;
    }

    public Customer Customer()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FName = "First",
            LName = "Last",
            Email = "customer@example.test",
            Phone = "01012345678",
            IdentityNumber = "12345678901234",
            HashPassword = new FeatureHasher().Hash("password")
        };
        Db.Customers.Add(customer);
        Actor.IsAuthenticated = true;
        Actor.CustomerId = customer.Id;
        Actor.UserId = null;
        Actor.Role = null;
        Refresh.ActorId = customer.Id;

        return customer;
    }

    public BaseUnit Unit(ProjectType type = ProjectType.Regular)
    {
        var city = new City { Id = Guid.NewGuid(), Name = "Cairo" };
        var area = new Area { Id = Guid.NewGuid(), Name = "Area", CityId = city.Id, City = city };
        var project = new BaseProject
        {
            Id = Guid.NewGuid(),
            AreaId = area.Id,
            Area = area,
            Name = "Project",
            Price = 1000,
            ProjectType = type
        };
        var unit = new BaseUnit
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Project = project,
            Number = "U1",
            Size = 10,
            MeterPrice = 10
        };
        Db.Cities.Add(city);
        Db.Areas.Add(area);
        Db.BaseProjects.Add(project);
        Db.BaseUnits.Add(unit);

        return unit;
    }

    public Reservation Reserve(BaseUnit unit, Customer customer)
    {
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            Unit = unit,
            CustomerId = customer.Id,
            Customer = customer,
            Status = ReservationStatus.Processing,
            Number = Guid.NewGuid().ToString("N")
        };
        Db.Reservations.Add(reservation);

        return reservation;
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        return Services.GetRequiredService<ISender>().Send(request, cancellationToken);
    }

    public async Task<Result> ExecuteAsync(object request)
    {
        // Inventory tests use the same registered authorization/validation pipeline as application callers.
        var response = await Services.GetRequiredService<ISender>().Send(request, CancellationToken.None);

        return (Result)(response ?? throw new InvalidOperationException("The request returned no result."));
    }

    public void Dispose() => Services.Dispose();
}
