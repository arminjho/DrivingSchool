using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAutoskola.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class LinkRevokedTokensAndLessonStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A revoked-token row is a logout marker for a token that expires on its own; the rows that
            // exist cannot name their user, and the new column is required.
            migrationBuilder.Sql("DELETE FROM [RevokedTokens]");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "RevokedTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Existing lessons get the RequestStatus row whose Code is their status (the enum names).
            migrationBuilder.AddColumn<int>(
                name: "RequestStatusId",
                table: "Lessons",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE l SET l.[RequestStatusId] = rs.[Id]
                  FROM [Lessons] l
                  INNER JOIN [RequestStatuses] rs ON rs.[Code] = CASE l.[Status]
                      WHEN 0 THEN 'Pending' WHEN 1 THEN 'Confirmed' WHEN 2 THEN 'Cancelled' WHEN 3 THEN 'Completed' END");

            migrationBuilder.AlterColumn<int>(
                name: "RequestStatusId",
                table: "Lessons",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevokedTokens_UserId",
                table: "RevokedTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_RequestStatusId",
                table: "Lessons",
                column: "RequestStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_RequestStatuses_RequestStatusId",
                table: "Lessons",
                column: "RequestStatusId",
                principalTable: "RequestStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RevokedTokens_Users_UserId",
                table: "RevokedTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_RequestStatuses_RequestStatusId",
                table: "Lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_RevokedTokens_Users_UserId",
                table: "RevokedTokens");

            migrationBuilder.DropIndex(
                name: "IX_RevokedTokens_UserId",
                table: "RevokedTokens");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_RequestStatusId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RevokedTokens");

            migrationBuilder.DropColumn(
                name: "RequestStatusId",
                table: "Lessons");
        }
    }
}
