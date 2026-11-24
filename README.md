# NornavindCinema

## Database and movie seed data

Configure `ConnectionStrings:DefaultConnection` for SQL Server and apply the EF Core migrations with `dotnet ef database update` before using the application. The database must be backed up as appropriate before applying migrations or importing data.

The Administration page imports the bundled `Data/omdb_responses.json` file. It adds movies that are not already present by IMDb ID and does not overwrite existing records. Repeated imports skip previously imported movies. Release dates marked `N/A` are stored as unknown.

The Administration page is restricted to one explicitly configured ASP.NET Core Identity user ID. Register and confirm an account using the existing Identity flow, find that account's `Id` in the `AspNetUsers` table, and configure it as `Admin:UserId`. For local development, run `dotnet user-secrets set "Admin:UserId" "<identity-user-id>"` from the project directory. For deployment, supply the equivalent `Admin__UserId` environment variable or another trusted configuration source. Restart the application after changing configuration and sign in with that account. Until the setting is configured, no user can access the page. Protect the configuration source and grant access only to a trusted administrator.

Navigate to **Admin** and submit **Seed movies**. Import runs as a single database transaction; errors are not reported as success. The result shows the number added and skipped. The seed data file is included in published output and must remain in the `Data` directory under the application's content root.
