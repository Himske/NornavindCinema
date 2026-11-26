# NornavindCinema

## Database and movie seed data

Configure `ConnectionStrings:DefaultConnection` for SQL Server and apply the EF Core migrations with `dotnet ef database update` before using the application. The database must be backed up as appropriate before applying migrations or importing data.

The migrations add the Identity roles `Guest`, `Member`, and `Admin` if they do not already exist. New accounts are assigned `Member` when their email is first confirmed; existing confirmed accounts are not automatically changed. Confirmation and role assignment succeed together, so if the `Member` role is missing, restore the role and retry the confirmation link. `Guest` and `Admin` are not assigned automatically. Rolling back the role migration leaves the roles and any user assignments in place.

The Administration page imports the bundled `Data/omdb_responses.json` file. It adds movies that are not already present by IMDb ID and does not overwrite existing records. Repeated imports skip previously imported movies. Release dates marked `N/A` are stored as unknown.

The Administration page is restricted to users assigned the `Admin` Identity role. Register and confirm an account, then have a trusted database administrator assign that account to the existing `Admin` role through ASP.NET Core Identity's `UserManager<ApplicationUser>.AddToRoleAsync(user, "Admin")` or a controlled database administration process. After a role assignment, sign out and back in to refresh the authentication cookie. Existing `Admin:UserId` configuration no longer grants access; the previously configured user must also be assigned the role. Role changes to existing cookies take effect when the security stamp is validated or the user signs in again.

Open the account dropdown, select **Administration**, and submit **Seed movies**. Import runs as a single database transaction; errors are not reported as success. The result shows the number added and skipped. The seed data file is included in published output and must remain in the `Data` directory under the application's content root.

Administrators can use **Manage users** to add or remove Guest, Member and Admin roles and permanently delete accounts. Deleting an account also deletes its favorites, reviews and ratings. Administrators cannot remove their own Admin role or delete their own account, and the last administrator cannot be removed. Role changes invalidate existing sessions on the next request.

Administrators can use **Moderate reviews** to hide or restore a member's review and rating together. Hidden ratings are excluded from public averages. Authors can still edit hidden submissions, which remain hidden until an administrator restores them.

## Member favorites and reviews

Signed-in Members and Admins can add or remove favorites and save one review and an optional 1–10 rating per movie on its details page. A review or a rating can be saved on its own and edited later. Member reviews are publicly visible without displaying account names. The account menu links to **My favorites**. Favorites and reviews are stored per account in the database; the earlier browser-only home-page list is not imported automatically.

## API credentials

Administrators can select **Configure APIs** from the account dropdown or Administration page to save or remove the OMDB API key, TMDB API key, and TMDB API read access token. Leave a field blank to retain its current value. The page indicates whether a value is present but never displays saved secrets. These settings are stored for future API integrations; saving them does not by itself connect to either service.

Credentials are encrypted using ASP.NET Core Data Protection before being stored in SQL Server. Persist and back up the application's Data Protection key ring in a secure location that survives deployments and is shared by all instances, and protect the key ring at rest using an appropriate platform or external key-management provider. Loss of the key ring makes saved credentials unreadable; they must then be entered again. Restrict access to both the database and the key ring. Follow the ASP.NET Core Data Protection deployment guidance for the hosting environment.
