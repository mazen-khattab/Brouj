# Brouj

Brouj is a robust backend application built with .NET following the principles of Clean Architecture and Domain-Driven Design (DDD). The architecture ensures a clear separation of concerns, high maintainability, and scalability.

## Architecture

The solution is divided into four main layers:

- **Domain Layer (`Brouj.Domain`)**: Contains enterprise logic and types. It is independent of all other layers and contains Entities, Value Objects, Enums, and Domain Interfaces.
- **Application Layer (`Brouj.Application`)**: Contains business logic and application use cases. It implements CQRS (Command Query Responsibility Segregation) and defines interfaces for external services (infrastructure).
- **Infrastructure Layer (`Brouj.Infrastructure`)**: Contains implementations of interfaces defined in the Application layer, including database contexts, external API clients, caching, and file system access.
- **API Layer (`Brouj.API`)**: The presentation layer. It provides RESTful API endpoints, handles HTTP requests and responses, and serves as the entry point for the application.

## Project Structure

```text
Brouj/
├── src/
│   ├── Brouj.API/               # Presentation layer (Web API)
│   ├── Brouj.Application/       # Application layer (Use cases, CQRS, Behaviors)
│   ├── Brouj.Domain/            # Domain layer (Entities, Enums, Core logic)
│   └── Brouj.Infrastructure/    # Infrastructure layer (Data access, Services)
├── tests/
│   ├── Brouj.API.IntegrationTests/
│   ├── Brouj.Application.UnitTests/
│   ├── Brouj.Domain.UnitTests/
│   └── Brouj.Infrastructure.IntegrationTests/
├── docs/                        # Project documentation
└── brouj_backend_plan_v2/       # Detailed backend implementation plans
```

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (Ensure you have the version matching the project's target framework)
- Your preferred IDE (Visual Studio, JetBrains Rider, or VS Code)
- A running database instance

### Build and Run

1. Clone the repository and navigate to the root directory.
2. Restore the dependencies:
   ```bash
   dotnet restore Brouj.slnx
   ```
3. Supply the database connection string, JWT settings, storage path and required request/rate budgets through user-secrets or environment/secret providers. See [API configuration](docs/Brouj.Docs/Part8.ApiConfiguration.md); keep credentials out of committed appsettings files.
4. Apply the reviewed database migration using the appropriate deployment identity. See [final verification and migration status](docs/Brouj.Docs/Part12.FinalVerification.md). API startup does not apply migrations automatically.
5. Run the API with its HTTPS profile for browser authentication:
   ```bash
   dotnet run --project src/Brouj.API/Brouj.API.csproj --launch-profile https
   ```

Development Swagger is available at `https://localhost:7249/index.html`. Configure the frontend's actual HTTPS origin and follow the documented cookie/CSRF sequence.

## Testing

The solution includes comprehensive unit and integration tests across all layers. To run the test suite, execute the following command in the root directory:

```bash
dotnet test Brouj.slnx
```

SQL Server tests require Docker/Testcontainers and `BROUJ_RUN_SQLSERVER_TESTS=1`. For the complete Release verification command, see the [final report](docs/Brouj.Docs/Part12.FinalVerification.md). The long performance baseline requires separate opt-in; see its [runbook](scripts/performance/README.md).

## Documentation

Detailed backend implementation plans, guidelines, and documentation can be found in the `docs/` and `brouj_backend_plan_v2/` directories.

The [Backend V1 final verification report](docs/Brouj.Docs/Part12.FinalVerification.md) records accepted limitations, tests, migration SQL and frontend handoff. The [endpoint contracts](docs/Brouj.Docs/Part8.EndpointContracts.md) describe the existing API. Cache integration and measured heavy-query optimization remain deferred by the owner; no 9k–10k RPS capacity claim is made.
