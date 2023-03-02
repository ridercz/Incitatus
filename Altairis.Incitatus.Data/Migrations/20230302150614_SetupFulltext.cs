using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altairis.Incitatus.Data.Migrations {
    /// <inheritdoc />
    public partial class SetupFulltext : Migration {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            // Create fulltext catalog; all operations must be run outside the main migration transaction
            migrationBuilder.Sql("CREATE FULLTEXT CATALOG DefaultCatalog WITH ACCENT_SENSITIVITY=OFF AS DEFAULT", suppressTransaction: true);

            // Create fulltext index on default catalog, with default stoplist and with automatic change tracking
            migrationBuilder.Sql("CREATE FULLTEXT INDEX ON Pages (Title LANGUAGE Czech, Description LANGUAGE Czech, Text LANGUAGE Czech) KEY INDEX [PK_Pages]", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            // Delete fulltext index
            migrationBuilder.Sql("DROP FULLTEXT INDEX ON Pages", suppressTransaction: true);

            // Delete fulltext catalog
            migrationBuilder.Sql("DROP FULLTEXT CATALOG DefaultCatalog", suppressTransaction: true);
        }

    }
}
