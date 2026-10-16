using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAutoskola.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ServiceTypes",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            // Databases seeded before this migration have ServiceTypes rows without a Code (and
            // no extra-lesson price) - give them stable codes before the unique index is built.
            // On an empty database this does nothing; the seeder inserts the rows with codes.
            migrationBuilder.Sql(@"
                UPDATE ServiceTypes SET Code = CASE Name
                    WHEN N'Teorijska obuka' THEN 'THEORY'
                    WHEN N'Praktična vožnja' THEN 'PRACTICAL'
                    WHEN N'Administracija i ispit' THEN 'ADMIN_EXAM'
                    ELSE 'LEGACY_' + CAST(Id AS nvarchar(20)) END
                WHERE Code = '';

                IF EXISTS (SELECT 1 FROM ServiceTypes) AND NOT EXISTS (SELECT 1 FROM ServiceTypes WHERE Code = 'EXTRA_LESSON')
                    INSERT INTO ServiceTypes (Code, Name, Price) VALUES ('EXTRA_LESSON', N'Dodatni čas vožnje', 40.00);");

            migrationBuilder.CreateTable(
                name: "Charges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ServiceTypeId = table.Column<int>(type: "int", nullable: false),
                    LessonId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Charges_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Charges_ServiceTypes_ServiceTypeId",
                        column: x => x.ServiceTypeId,
                        principalTable: "ServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Charges_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Installments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChargeId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Installments_Charges_ChargeId",
                        column: x => x.ChargeId,
                        principalTable: "Charges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstallmentId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PayPalAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PayPalCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CapturedAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PayPalOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayPalApprovalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PayPalCaptureId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Installments_InstallmentId",
                        column: x => x.InstallmentId,
                        principalTable: "Installments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AmountKm = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PayPalRefundId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTypes_Code",
                table: "ServiceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Charges_LessonId",
                table: "Charges",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_ServiceTypeId",
                table: "Charges",
                column: "ServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_StudentId",
                table: "Charges",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_StudentId_ServiceTypeId",
                table: "Charges",
                columns: new[] { "StudentId", "ServiceTypeId" },
                unique: true,
                filter: "[LessonId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_ChargeId_Number",
                table: "Installments",
                columns: new[] { "ChargeId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InstallmentId",
                table: "Payments",
                column: "InstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayPalOrderId",
                table: "Payments",
                column: "PayPalOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PaymentId",
                table: "Refunds",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Installments");

            migrationBuilder.DropTable(
                name: "Charges");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTypes_Code",
                table: "ServiceTypes");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ServiceTypes");
        }
    }
}
