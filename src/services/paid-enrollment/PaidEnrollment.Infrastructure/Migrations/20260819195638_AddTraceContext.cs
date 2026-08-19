using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaidEnrollment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "trace_context",
                table: "purchases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trace_context",
                table: "outbox_messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trace_context",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "trace_context",
                table: "outbox_messages");
        }
    }
}
