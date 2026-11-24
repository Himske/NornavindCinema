using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NornavindCinema.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedIdentityRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
                SELECT CONVERT(nvarchar(36), NEWID()), roles.[Name], roles.[NormalizedName], CONVERT(nvarchar(36), NEWID())
                FROM (VALUES (N'Guest', N'GUEST'), (N'Member', N'MEMBER'), (N'Admin', N'ADMIN')) AS roles ([Name], [NormalizedName])
                WHERE NOT EXISTS (SELECT 1 FROM [AspNetRoles] existing WHERE existing.[NormalizedName] = roles.[NormalizedName]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve roles and any user assignments when rolling back.
        }
    }
}
