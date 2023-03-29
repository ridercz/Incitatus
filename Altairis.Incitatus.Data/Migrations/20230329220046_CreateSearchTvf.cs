using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altairis.Incitatus.Data.Migrations {
    /// <inheritdoc />
    public partial class CreateSearchTvf : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("""
                CREATE FUNCTION SearchPagesInSite(@SiteId AS uniqueidentifier, @Query AS nvarchar(4000))
                RETURNS TABLE
                AS RETURN (
                    SELECT Rank = R.RANK, P.Url, P.Title, P.Description, P.DateLastUpdated
                    FROM CONTAINSTABLE(Pages, *, @Query) AS R
                    LEFT JOIN Pages AS P ON R.[KEY] = P.Id
                    WHERE P.SiteId = @SiteId
                )
                """, suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("DROP FUNCTION SearchPagesInSite", suppressTransaction: true);
        }
    }
}
