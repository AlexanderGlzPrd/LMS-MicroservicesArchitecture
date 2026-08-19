using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Enrollments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTraceContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                table: "outbox_messages");
        }
    }
}
