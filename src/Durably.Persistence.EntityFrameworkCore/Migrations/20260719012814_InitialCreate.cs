using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Durably.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "durable");

            migrationBuilder.CreateTable(
                name: "Executions",
                schema: "durable",
                columns: table => new
                {
                    FlowName = table.Column<string>(maxLength: 200, nullable: false),
                    InstanceId = table.Column<string>(maxLength: 200, nullable: false),
                    Status = table.Column<int>(nullable: false),
                    CurrentStep = table.Column<int>(nullable: false),
                    ContextJson = table.Column<string>(nullable: false),
                    Attempts = table.Column<int>(nullable: false),
                    FailedStep = table.Column<string>(maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(nullable: true),
                    Version = table.Column<long>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    UpdatedAt = table.Column<DateTime>(nullable: false),
                    LockedBy = table.Column<string>(maxLength: 200, nullable: true),
                    LockedUntil = table.Column<DateTime>(nullable: true),
                    MetadataJson = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => new { x.FlowName, x.InstanceId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_durable_Executions_Status_LockedUntil",
                schema: "durable",
                table: "Executions",
                columns: new[] { "Status", "LockedUntil" });

            migrationBuilder.CreateTable(
                name: "Traces",
                schema: "durable",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("Sqlite:Autoincrement", true)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FlowName = table.Column<string>(maxLength: 200, nullable: false),
                    InstanceId = table.Column<string>(maxLength: 200, nullable: false),
                    StepKey = table.Column<string>(maxLength: 200, nullable: false),
                    Attempt = table.Column<int>(nullable: false),
                    Outcome = table.Column<int>(nullable: false),
                    InputJson = table.Column<string>(nullable: true),
                    OutputJson = table.Column<string>(nullable: true),
                    DurationMs = table.Column<int>(nullable: false),
                    ExceptionMessage = table.Column<string>(nullable: true),
                    Timestamp = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Traces", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_durable_Traces_Flow_Instance",
                schema: "durable",
                table: "Traces",
                columns: new[] { "FlowName", "InstanceId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Executions", schema: "durable");
            migrationBuilder.DropTable(name: "Traces", schema: "durable");
        }
    }
}
