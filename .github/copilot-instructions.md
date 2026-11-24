# Copilot instructions for NornavindCinema

## Project overview
- This is an ASP.NET Core MVC web application targeting .NET 10 (`net10.0`). Nullable reference types and implicit usings are enabled.
- The application uses Razor views, ASP.NET Core Identity, Entity Framework Core, and SQL Server.
- The main areas are `Controllers/`, `Models/`, `Data/`, `Views/`, and `Areas/Identity/Pages/`. Static assets are under `wwwroot/`.

## Implementation conventions
- Follow the existing MVC pattern: keep request handling in controllers, view-specific presentation in Razor views, and persisted application data in the data/model layer.
- Use dependency injection for application services and database access. Use `ApplicationDbContext` for EF Core data work; do not introduce a separate database access mechanism without a clear need.
- Preserve ASP.NET Core Identity conventions and existing Razor Pages under `Areas/Identity` when changing authentication or account functionality.
- Use MVC tag helpers and the shared layout where appropriate. Keep static styles and scripts in `wwwroot/css` and `wwwroot/js` rather than embedding them in views.
- Match the formatting and naming of the closest existing code. Keep nullable annotations meaningful and handle potentially missing configuration or data explicitly.
- Keep changes focused; do not add dependencies, abstractions, or new project structure unless the feature needs them.

## Data and configuration
- When changing the EF Core model, add and review the corresponding migration under `Data/Migrations`; do not edit generated migration snapshots by hand unless the change specifically requires it.
- The application expects a `DefaultConnection` connection string. Keep credentials and other secrets out of source control; use development user-secrets or environment-specific configuration for sensitive values.
- Do not expose detailed exception information outside development. Preserve the existing development/production exception handling distinction.

## Validation
- Build from the solution or project root with `dotnet build NornavindCinema.slnx` (or `dotnet build NornavindCinema.csproj` for a project-only check).
- If tests are added or changed, run the relevant test project; do not assume there is an existing test project when none is present.
- For database changes, verify the migration is generated for the intended model change and follows the repository's existing EF Core setup.
