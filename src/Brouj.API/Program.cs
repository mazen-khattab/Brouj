using Brouj.Application;
using Brouj.Infrastructure;

namespace Brouj.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register HTTP services and safe production error responses before building the host.
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddProblemDetails();

            // Infrastructure binds and validates JWT, cookie, caching, and storage settings.
            // Signing keys and connection credentials come from configuration/secret providers.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(connectionString);
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddBroujCookieAuthentication();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.MapOpenApi();
            }
            else
            {
                // Catch downstream failures without exposing exception details to clients.
                app.UseExceptionHandler();
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            // CSRF validation needs the authenticated identity and must run before authorization.
            // Keep these calls adjacent so unsafe requests cannot bypass cookie protection.
            app.UseAuthentication();
            app.UseBroujCookieProtection();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
