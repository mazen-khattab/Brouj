using System.Reflection;
using Brouj.Application.Behaviors;
using Brouj.Application.Common.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Brouj.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddScoped<FeatureAccess>();
        services.AddScoped<SessionResponse>();
        services.AddSingleton<LoginPasswordVerifier>();
        services.AddScoped<PendingFileCleanup>();
        services.AddScoped<FileUpload>();
        services.AddScoped<Features.TimePlans.Services.TimePlanDraftBuilder>();
        services.AddScoped<Features.TimePlans.Services.TimePlanOrdering>();

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(FeatureFailureBehavior<,>));
            configuration.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(CommittedEffectsBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        return services;
    }
}
