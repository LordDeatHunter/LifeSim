using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSim.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalHealths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Health",
                table: "Animals",
                type: "REAL",
                nullable: false,
                defaultValue: 20f);

            migrationBuilder.AddColumn<float>(
                name: "MaxHealth",
                table: "Animals",
                type: "REAL",
                nullable: false,
                defaultValue: 20f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Health",
                table: "Animals");

            migrationBuilder.DropColumn(
                name: "MaxHealth",
                table: "Animals");
        }
    }
}
