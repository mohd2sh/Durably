using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Durably.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddStepPathHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StepPathHash",
                schema: "durable",
                table: "Executions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StepPathHash",
                schema: "durable",
                table: "Executions");
        }
    }
}
