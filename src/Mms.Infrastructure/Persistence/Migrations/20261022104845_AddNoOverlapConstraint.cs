using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNoOverlapConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.Sql("""
                ALTER TABLE "Contracts"
                ADD CONSTRAINT "EX_Contracts_NoOverlap"
                EXCLUDE USING gist (
                    "RoomId" WITH =,
                    daterange("StartDate", "EndDate", '[)') WITH &&
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
             migrationBuilder.Sql("""ALTER TABLE "Contracts" DROP CONSTRAINT "EX_Contracts_NoOverlap";""");
        }
    }
}
