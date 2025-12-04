using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeSim.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordOAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Bets");
            migrationBuilder.DropTable(name: "Users");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    DiscordId = table.Column<string>(type: "TEXT", nullable: false),
                    DiscordUsername = table.Column<string>(type: "TEXT", nullable: false),
                    DiscordAvatar = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Balance = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.DiscordId);
                });

            migrationBuilder.CreateTable(
                name: "Bets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiscordId = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BetType = table.Column<string>(type: "TEXT", nullable: false),
                    InitialCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bets_Users_DiscordId",
                        column: x => x.DiscordId,
                        principalTable: "Users",
                        principalColumn: "DiscordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bets_DiscordId",
                table: "Bets",
                column: "DiscordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Bets");
            migrationBuilder.DropTable(name: "Users");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Balance = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "Bets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<ulong>(type: "INTEGER", nullable: false),
                    BetType = table.Column<string>(type: "TEXT", nullable: false),
                    InitialCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bets_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "ClientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bets_ClientId",
                table: "Bets",
                column: "ClientId");
        }
    }
}
