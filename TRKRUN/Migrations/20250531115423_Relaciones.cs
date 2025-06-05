using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TRKRUN.Migrations
{
    /// <inheritdoc />
    public partial class Relaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Torneo_Circuito_circuito_id",
                table: "Torneo");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Torneo_torneo_id",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_torneo_id",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "UserTorneo",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    torneo_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTorneo", x => new { x.user_id, x.torneo_id });
                    table.ForeignKey(
                        name: "FK_UserTorneo_Torneo_torneo_id",
                        column: x => x.torneo_id,
                        principalTable: "Torneo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTorneo_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserTorneo_torneo_id",
                table: "UserTorneo",
                column: "torneo_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Torneo_Circuito_circuito_id",
                table: "Torneo",
                column: "circuito_id",
                principalTable: "Circuito",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Torneo_Circuito_circuito_id",
                table: "Torneo");

            migrationBuilder.DropTable(
                name: "UserTorneo");

            migrationBuilder.CreateIndex(
                name: "IX_Users_torneo_id",
                table: "Users",
                column: "torneo_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Torneo_Circuito_circuito_id",
                table: "Torneo",
                column: "circuito_id",
                principalTable: "Circuito",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Torneo_torneo_id",
                table: "Users",
                column: "torneo_id",
                principalTable: "Torneo",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
