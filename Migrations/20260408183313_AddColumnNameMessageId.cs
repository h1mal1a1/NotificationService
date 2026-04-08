using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnNameMessageId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "notifications",
                newName: "message_id");

            migrationBuilder.RenameIndex(
                name: "IX_notifications_MessageId",
                table: "notifications",
                newName: "IX_notifications_message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "message_id",
                table: "notifications",
                newName: "MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_notifications_message_id",
                table: "notifications",
                newName: "IX_notifications_MessageId");
        }
    }
}
