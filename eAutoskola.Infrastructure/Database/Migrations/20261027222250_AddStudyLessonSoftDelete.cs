using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAutoskola.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyLessonSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StudyLessons",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StudyLessons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StudyLessons");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StudyLessons");
        }
    }
}
