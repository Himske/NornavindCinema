# NornavindCinema

NornavindCinema is an ASP.NET Core movie catalog with a dark-themed home page, searchable movie list, member favorites and reviews, and administrative tools for managing the catalog and users. Movies can be entered manually or imported from OMDB and TMDB.

## Requirements

- .NET 10 SDK for development and builds, or the ASP.NET Core 10 runtime for a published deployment.
- SQL Server or SQL Server LocalDB. The checked-in development connection uses Windows LocalDB; on other platforms or hosts, configure a reachable SQL Server instance.
- An account with permission to create or update the target database when applying migrations.
- The EF Core command-line tool (`dotnet-ef`) compatible with the project's EF Core 10 packages for CLI migrations. Visual Studio's Package Manager Console can be used instead.
- Optional: an OMDB API key and a TMDB API read access token for the respective search and import features. Network access to the providers and their image hosts is required.

## Getting started

Run the following commands from the repository root in PowerShell, or open `NornavindCinema.slnx` in Visual Studio and restore/build the solution.

1. Configure the database. The default `ConnectionStrings:DefaultConnection` in `appsettings.json` points to LocalDB with integrated authentication. For another server, override it with user secrets in development, for example:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=<server>;Database=<database>;Integrated Security=True;TrustServerCertificate=True"
   ```

   Adjust authentication and TLS settings for your server. `TrustServerCertificate=True` is appropriate only where you have assessed the trust implications; use a trusted certificate in production. For environment-based deployment, use `ConnectionStrings__DefaultConnection`. Keep passwords out of source control.

2. Restore and build:

   ```powershell
   dotnet restore NornavindCinema.slnx
   dotnet build NornavindCinema.slnx
   ```

3. Apply the migrations. If `dotnet ef` is not installed, install the EF Core CLI tool separately; use a version compatible with the project's EF Core packages. Back up existing databases before applying migrations or importing data.

   ```powershell
   dotnet ef database update --project NornavindCinema.csproj
   ```

4. Start the application:

   ```powershell
   dotnet run --project NornavindCinema.csproj --launch-profile https
   ```

   The checked-in HTTPS profile uses `https://localhost:7058` and `http://localhost:5002`; the HTTP profile uses port 5002. Trust the ASP.NET Core development certificate if your browser requires it (`dotnet dev-certs https --trust`). Launch settings are for local development and can be overridden.

5. Register and confirm an account, then arrange the first administrator as described below. An administrator can seed the bundled catalog or configure an API and import movies.

### Account confirmation and first administrator

Sign-in requires a confirmed account. **The current registration confirmation page displays a confirmation link and the application does not configure a real email sender.** This is a development convenience, not proof of email ownership, and must not be used as a production confirmation workflow. Before deployment, configure a real Identity email sender and change the confirmation page so it no longer displays the link; also test confirmation, password reset and related email flows.

The migrations create `Guest`, `Member`, and `Admin` roles if absent. When an account is first confirmed, the application assigns `Member` in the same transaction. Existing confirmed accounts are not automatically changed. If `Member` is missing, restore the role and retry confirmation. Rolling back the role migration leaves roles and assignments intact.

There is no automatic first administrator. Have a trusted operator assign a confirmed account to the existing `Admin` role using ASP.NET Core Identity's `UserManager<ApplicationUser>.AddToRoleAsync(user, "Admin")` or a controlled database administration process. Sign out and back in after assignment. The former `Admin:UserId` setting does not grant access.

## Roles and permissions

| Visitor or role | Capabilities |
| --- | --- |
| Anonymous or Guest | Browse the home page and movie list, view details and visible reviews. The Guest role does not grant member or administrator permissions. |
| Member | All public features, plus favorites and one editable review/rating per movie. |
| Admin | Member features plus movie creation, editing and deletion; dashboard, imports, API configuration, user management and review moderation. |

Authorization is enforced by the `Administrator` and `MovieMember` policies, not just by hiding links. Administrators can assign or remove Guest, Member and Admin roles; role changes invalidate existing sessions on security-stamp validation. An administrator cannot remove their own Admin role or delete their own account, and the last administrator cannot be removed. Deleting an account also deletes its favorites and reviews.

## Using the application

- **Movies:** Search by title, filter by genre and paginate the catalog. List state is retained per browser tab when returning from a movie. Titles open the details page. Administrators can create, edit and delete movies.
- **Favorites and reviews:** Members and admins can favorite movies and use **Write a review** or **Edit your review** on the details page. A review or a rating can be saved independently; ratings are 1–10. One submission per user and movie is enforced by the database. Public reviews do not display account names. The account menu links to **My favorites**. Earlier browser-only home-page favorites are not imported.
- **Moderation:** Administrators can hide or restore a review and its rating together. Hidden submissions do not appear publicly or contribute to member-rating averages. Authors can edit a hidden submission, but it stays hidden until restored.
- **Dashboard:** Admins can open **Dashboard** in the navbar or **Administration** in the account dropdown. It shows movie and user totals, visible written reviews, the highest average visible member rating, top genres and the most recently inserted movies. Historical activity and trends are unavailable because timestamps are not recorded.
- **Seed movies:** On the dashboard, submit **Seed movies** to import the bundled `Data/omdb_responses.json`. The import is transactional and skips existing or duplicate IMDb IDs without changing existing movies. Unknown release dates are stored as null. The file is included in published output and must remain under the application's content root at `Data/omdb_responses.json`.

## OMDB and TMDB integration

An administrator can open **Configure APIs** from the dashboard or account dropdown to save or remove the OMDB API key, TMDB API key and TMDB API read access token. Blank inputs retain existing values; saved secrets are never displayed. The TMDB integration uses the **read access token**, not the separately stored TMDB API key.

- **Search OMDB** searches and imports full movie details. Existing IMDb IDs are skipped. OMDB returns up to ten results per page; the UI supports up to 100 pages.
- **Search TMDB** searches and imports details, credits, poster and backdrop URLs. Imports skip existing TMDB IDs or matching IMDb IDs. TMDB search results do not include IMDb IDs, so a match with an OMDB movie can only be detected during import. The UI supports up to 500 pages.
- On the movie edit page, **Update from OMDB** uses the entered IMDb ID and replaces OMDB form fields, including ratings, while retaining the TMDB ID and backdrop. **Update from TMDB** uses the TMDB ID or looks it up by IMDb ID if absent; available TMDB values replace the corresponding form fields, missing values are retained, and other rating sources are preserved. Both actions populate the form only: review the changes and select **Save** to persist them.

TMDB ratings are identified as The Movie Database ratings, not IMDb ratings. TMDB data is provided by TMDB; this product is not endorsed or certified by TMDB. Review each provider's terms and attribution requirements before deployment.

API credentials are encrypted with ASP.NET Core Data Protection before storage in SQL Server. Persist and back up the Data Protection key ring in a secure location that survives deployments and is shared by all application instances. Protect it at rest with an appropriate platform or external key-management provider. Losing the key ring makes saved credentials unreadable; they must be entered again. Restrict access to the database and key ring, and follow the ASP.NET Core Data Protection deployment guidance.

## Project structure and architecture

```text
NornavindCinema.slnx / NornavindCinema.csproj   Solution and web project
Program.cs                                      Dependency injection, policies and middleware
Controllers/                                    MVC request handlers
Models/                                         Persisted movie, favorite, review and credential models
ViewModels/                                     Models for views and form input
Data/                                           EF Core context, migrations, API clients and seed data
Views/                                          MVC Razor views and shared layout
Areas/Identity/Pages/                           Identity Razor Pages
wwwroot/css/ and wwwroot/js/                    Site styles and browser behavior
Properties/launchSettings.json                  Local launch profiles
```

The application uses ASP.NET Core MVC for catalog and admin pages, Razor Pages for Identity, and EF Core with SQL Server through `ApplicationDbContext`. Identity users and roles share the database with movie data. Favorites and member reviews are keyed by user and movie; imported source ratings are separate from member ratings. OMDB and TMDB clients are injected HTTP clients; imports and seed operations check for existing identifiers before inserting. The movie list's filter and page state is stored in browser session storage, not in the database.

Database schema changes require an EF Core migration under `Data/Migrations`. Review generated migrations and the model snapshot, and apply them to the intended database before running code that depends on the new schema.

## Deployment and troubleshooting

- Set `ConnectionStrings__DefaultConnection` using secure deployment configuration and apply migrations deliberately. Back up the database and the Data Protection key ring before upgrades or imports.
- Configure a production email sender and remove the on-page confirmation link before exposing registration. Configure HTTPS and a trusted certificate for the deployment. Outside Development, the application uses its error page and HSTS; avoid exposing detailed exceptions to users.
- If database access or migration fails, verify the SQL Server instance, connection string, authentication, permissions and installed EF CLI version. `DefaultConnection` is the connection used by the app.
- If confirmation fails, ensure migrations have been applied and the `Member` role exists. Existing confirmed users require an explicit role assignment if needed.
- If an import or refresh fails, check the configured provider credential, movie ID, network access and application logs. Do not put credentials in bug reports or source files.
- If saved API credentials cannot be decrypted after moving or redeploying the app, check that the original Data Protection key ring is available; otherwise re-enter the credentials.

## Development and validation

Use `dotnet build NornavindCinema.slnx` for a solution build or `dotnet build NornavindCinema.csproj` for the web project. No test project is currently included in the repository. When adding tests, run the relevant test project and keep database-backed verification isolated from real data. Local configuration and secrets should not be committed.
