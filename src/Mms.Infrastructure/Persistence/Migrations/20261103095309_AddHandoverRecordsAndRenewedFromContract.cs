using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverRecordsAndRenewedFromContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RenewedFromContractId",
                table: "Contracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HandoverRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RoomCondition = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    KeyCount = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverRecords_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverRecords_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_RenewedFromContractId",
                table: "Contracts",
                column: "RenewedFromContractId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverRecords_ContractId_Type",
                table: "HandoverRecords",
                columns: new[] { "ContractId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_HandoverRecords_CreatedByUserId",
                table: "HandoverRecords",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Contracts_RenewedFromContractId",
                table: "Contracts",
                column: "RenewedFromContractId",
                principalTable: "Contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Contracts_RenewedFromContractId",
                table: "Contracts");

            migrationBuilder.DropTable(
                name: "HandoverRecords");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_RenewedFromContractId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "RenewedFromContractId",
                table: "Contracts");
        }
    }
}
