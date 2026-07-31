using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Durably.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ClaimIndexCreatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_durable_Executions_Status_LockedUntil",
                schema: "durable",
                table: "Executions");

            migrationBuilder.CreateIndex(
                name: "IX_durable_Executions_Status_LockedUntil_CreatedAt",
                schema: "durable",
                table: "Executions",
                columns: new[] { "Status", "LockedUntil", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_durable_Executions_Status_LockedUntil_CreatedAt",
                schema: "durable",
                table: "Executions");

            migrationBuilder.CreateIndex(
                name: "IX_durable_Executions_Status_LockedUntil",
                schema: "durable",
                table: "Executions",
                columns: new[] { "Status", "LockedUntil" });
        }
    }
}
