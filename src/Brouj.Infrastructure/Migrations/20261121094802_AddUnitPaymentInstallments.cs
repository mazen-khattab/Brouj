using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brouj.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitPaymentInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnitPaymentInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstallmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    InstallmentPercentage = table.Column<decimal>(type: "decimal(7,4)", precision: 7, scale: 4, nullable: false),
                    InstallmentPeriodMonths = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitPaymentInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitPaymentInstallments_BaseUnits_UnitId",
                        column: x => x.UnitId,
                        principalTable: "BaseUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_UnitPaymentInstallments_UnitId_InstallmentNumber",
                table: "UnitPaymentInstallments",
                columns: new[] { "UnitId", "InstallmentNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitPaymentInstallments");
        }
    }
}
