# NornavindCinema

## Database and movie seed data

Configure `ConnectionStrings:DefaultConnection` for SQL Server and apply the EF Core migrations with `dotnet ef database update` before using the application. The database must be backed up as appropriate before applying migrations or importing data.

The migrations add the Identity roles `Guest`, `Member`, and `Admin` if they do not already exist. New accounts are assigned `Member` when their email is first confirmed; existing confirmed accounts are not automatically changed. Confirmation and role assignment succeed together, so if the `Member` role is missing, restore the role and retry the confirmation link. `Guest` and `Admin` are not assigned automatically. Rolling back the role migration leaves the roles and any user assignments in place.

The Administration page imports the bundled `Data/omdb_responses.json` file. It adds movies that are not already present by IMDb ID and does not overwrite existing records. Repeated imports skip previously imported movies. Release dates marked `N/A` are stored as unknown.

The Administration page is restricted to users assigned the `Admin` Identity role. Register and confirm an account, then have a trusted database administrator assign that account to the existing `Admin` role through ASP.NET Core Identity's `UserManager<ApplicationUser>.AddToRoleAsync(user, "Admin")` or a controlled database administration process. After a role assignment, sign out and back in to refresh the authentication cookie. Existing `Admin:UserId` configuration no longer grants access; the previously configured user must also be assigned the role. Role changes to existing cookies take effect when the security stamp is validated or the user signs in again.

Administrators can open **Dashboard** in the navbar or **Administration** in the account dropdown. The dashboard shows movie and user totals, visible written reviews, the highest average visible member rating, top genres, and the most recently inserted movies. Historical activity and trends are unavailable because timestamps are not recorded.

From the dashboard, submit **Seed movies**. Import runs as a single database transaction; errors are not reported as success. The result shows the number added and skipped. The seed data file is included in published output and must remain in the `Data` directory under the application's content root.

Administrators can use **Manage users** to add or remove Guest, Member and Admin roles and permanently delete accounts. Deleting an account also deletes its favorites, reviews and ratings. Administrators cannot remove their own Admin role or delete their own account, and the last administrator cannot be removed. Role changes invalidate existing sessions on the next request.

Administrators can use **Moderate reviews** to hide or restore a member's review and rating together. Hidden ratings are excluded from public averages. Authors can still edit hidden submissions, which remain hidden until an administrator restores them.

## Member favorites and reviews

Signed-in Members and Admins can add or remove favorites and save one review and an optional 1–10 rating per movie on its details page. Use **Write a review** to open the form or **Edit your review** to change the existing submission. A review or a rating can be saved on its own and edited later. Member reviews appear in the **Member reviews** tab and are publicly visible without displaying account names. The account menu links to **My favorites**. Favorites and reviews are stored per account in the database; the earlier browser-only home-page list is not imported automatically.

## API credentials

Administrators can select **Configure APIs** from the account dropdown or Administration page to save or remove the OMDB API key, TMDB API key, and TMDB API read access token. Leave a field blank to retain its current value. The page indicates whether a value is present but never displays saved secrets.

With an OMDB API key configured, administrators can use **Search OMDB** to search movies and import full details. Imports skip existing IMDb IDs without changing them. OMDB returns up to ten results per page and supports at most 100 pages. On the movie edit page, **Update from OMDB** loads data using the entered IMDb ID and replaces the form's OMDB fields, including ratings. The TMDB ID and backdrop are retained. Review the result and select **Save** to persist it; no changes are saved by the lookup itself.

With the TMDB API read access token configured, administrators can use **Search TMDB** to search and import movies. Imports skip existing TMDB IDs or matching IMDb IDs without changing existing records. A match against an OMDB movie can only be detected when importing, because the search results do not contain IMDb IDs. TMDB images and ratings are credited to TMDB; missing IMDb ratings remain unset. The separate TMDB API key is not used for this integration. TMDB data is provided by TMDB, and this product is not endorsed or certified by TMDB.

On the movie edit page, **Update from TMDB** loads available movie details using the entered TMDB ID, or finds it using the IMDb ID if the TMDB ID is blank. It retains existing values where TMDB provides no data and preserves other rating sources. Review the form and select **Save** to persist the changes.

Credentials are encrypted using ASP.NET Core Data Protection before being stored in SQL Server. Persist and back up the application's Data Protection key ring in a secure location that survives deployments and is shared by all instances, and protect the key ring at rest using an appropriate platform or external key-management provider. Loss of the key ring makes saved credentials unreadable; they must then be entered again. Restrict access to both the database and the key ring. Follow the ASP.NET Core Data Protection deployment guidance for the hosting environment.
