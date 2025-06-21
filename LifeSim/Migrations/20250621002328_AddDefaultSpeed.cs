using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSim.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultSpeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "DefaultSpeed",
                table: "Animals",
                type: "REAL",
                nullable: false,
                defaultValue: 16f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultSpeed",
                table: "Animals");
        }
    }
}
