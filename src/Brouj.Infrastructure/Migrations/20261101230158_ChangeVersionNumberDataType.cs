using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Brouj.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeVersionNumberDataType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VersionNumber",
                table: "TermsAndConditions",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "VersionNumber",
                table: "TermsAndConditions",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldUnicode: false,
                oldMaxLength: 32);
        }
    }
}
