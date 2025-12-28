using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSim.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEntityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Animals");

            migrationBuilder.DropTable(
                name: "Foods");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Foods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Size = table.Column<float>(type: "REAL", nullable: false),
                    Age = table.Column<float>(type: "REAL", nullable: false),
                    Lifespan = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Foods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Animals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false),
                    ColorHex = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Size = table.Column<float>(type: "REAL", nullable: false),
                    PredationInclination = table.Column<float>(type: "REAL", nullable: false),
                    Saturation = table.Column<float>(type: "REAL", nullable: false),
                    ReproductionCooldown = table.Column<float>(type: "REAL", nullable: false),
                    Speed = table.Column<float>(type: "REAL", nullable: false),
                    DefaultSpeed = table.Column<float>(type: "REAL", nullable: false, defaultValue: 16f),
                    Age = table.Column<float>(type: "REAL", nullable: false),
                    Lifespan = table.Column<float>(type: "REAL", nullable: false),
                    Health = table.Column<float>(type: "REAL", nullable: false, defaultValue: 20f),
                    MaxHealth = table.Column<float>(type: "REAL", nullable: false, defaultValue: 20f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Animals", x => x.Id);
                });
        }
    }
}

