using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Durably.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "durable");

            migrationBuilder.CreateTable(
                name: "Executions",
                schema: "durable",
                columns: table => new
                {
                    FlowName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RunId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StepPathHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    FailedStep = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => new { x.FlowName, x.RunId });
                });

            migrationBuilder.CreateTable(
                name: "Traces",
                schema: "durable",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlowName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RunId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstanceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StepKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    ExceptionMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Traces", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_durable_Executions_Open_Flow_Instance",
                schema: "durable",
                table: "Executions",
                columns: new[] { "FlowName", "InstanceId" },
                unique: true,
                filter: "[Status] IN (0, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_durable_Executions_Flow_Instance",
                schema: "durable",
                table: "Executions",
                columns: new[] { "FlowName", "InstanceId" });

            migrationBuilder.CreateIndex(
                name: "IX_durable_Executions_Status_LockedUntil_CreatedAt",
                schema: "durable",
                table: "Executions",
                columns: new[] { "Status", "LockedUntil", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_durable_Traces_Flow_Run",
                schema: "durable",
                table: "Traces",
                columns: new[] { "FlowName", "RunId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Executions",
                schema: "durable");

            migrationBuilder.DropTable(
                name: "Traces",
                schema: "durable");
        }
    }
}
