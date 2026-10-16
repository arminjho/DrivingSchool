using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAutoskola.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddTestsAndStudyLessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyLessons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QuestionAreaId = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ValidForYear = table.Column<int>(type: "int", nullable: false),
                    ReadingMinutes = table.Column<int>(type: "int", nullable: false),
                    ChapterLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyLessons_QuestionAreas_QuestionAreaId",
                        column: x => x.QuestionAreaId,
                        principalTable: "QuestionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QuestionAreaId = table.Column<int>(type: "int", nullable: false),
                    PassThresholdPercent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tests_QuestionAreas_QuestionAreaId",
                        column: x => x.QuestionAreaId,
                        principalTable: "QuestionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudyLessonId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationLogs_StudyLessons_StudyLessonId",
                        column: x => x.StudyLessonId,
                        principalTable: "StudyLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecommendationLogs_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentStudyLessons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StudyLessonId = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastOpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentStudyLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentStudyLessons_StudyLessons_StudyLessonId",
                        column: x => x.StudyLessonId,
                        principalTable: "StudyLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentStudyLessons_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyLessonSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudyLessonId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyLessonSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyLessonSections_StudyLessons_StudyLessonId",
                        column: x => x.StudyLessonId,
                        principalTable: "StudyLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuestionAreaId = table.Column<int>(type: "int", nullable: false),
                    TestId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_QuestionAreas_QuestionAreaId",
                        column: x => x.QuestionAreaId,
                        principalTable: "QuestionAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Questions_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    TestId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    IncorrectCount = table.Column<int>(type: "int", nullable: false),
                    Percent = table.Column<double>(type: "float", nullable: false),
                    Passed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestAttempts_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestAttempts_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnswerOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswerOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnswerOptions_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestAttemptId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedAnswerOptionId = table.Column<int>(type: "int", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_AnswerOptions_SelectedAnswerOptionId",
                        column: x => x.SelectedAnswerOptionId,
                        principalTable: "AnswerOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttemptAnswers_TestAttempts_TestAttemptId",
                        column: x => x.TestAttemptId,
                        principalTable: "TestAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnswerOptions_QuestionId",
                table: "AnswerOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_QuestionId",
                table: "AttemptAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_SelectedAnswerOptionId",
                table: "AttemptAnswers",
                column: "SelectedAnswerOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttemptAnswers_TestAttemptId",
                table: "AttemptAnswers",
                column: "TestAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionAreaId",
                table: "Questions",
                column: "QuestionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_TestId",
                table: "Questions",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationLogs_StudentId",
                table: "RecommendationLogs",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationLogs_StudyLessonId",
                table: "RecommendationLogs",
                column: "StudyLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyLessons_StudentId_StudyLessonId",
                table: "StudentStudyLessons",
                columns: new[] { "StudentId", "StudyLessonId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyLessons_StudyLessonId",
                table: "StudentStudyLessons",
                column: "StudyLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyLessons_QuestionAreaId",
                table: "StudyLessons",
                column: "QuestionAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyLessonSections_StudyLessonId",
                table: "StudyLessonSections",
                column: "StudyLessonId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_StudentId",
                table: "TestAttempts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_TestId",
                table: "TestAttempts",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_QuestionAreaId",
                table: "Tests",
                column: "QuestionAreaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttemptAnswers");

            migrationBuilder.DropTable(
                name: "RecommendationLogs");

            migrationBuilder.DropTable(
                name: "StudentStudyLessons");

            migrationBuilder.DropTable(
                name: "StudyLessonSections");

            migrationBuilder.DropTable(
                name: "AnswerOptions");

            migrationBuilder.DropTable(
                name: "TestAttempts");

            migrationBuilder.DropTable(
                name: "StudyLessons");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Tests");
        }
    }
}
