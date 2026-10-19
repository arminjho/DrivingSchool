using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAutoskola.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[Role] IN ('Admin', 'Instructor', 'Student')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");
        }
    }
}
