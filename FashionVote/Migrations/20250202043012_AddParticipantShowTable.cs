using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FashionVote.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantShowTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantShow_Participants_ParticipantId",
                table: "ParticipantShow");

            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantShow_Shows_ShowId",
                table: "ParticipantShow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParticipantShow",
                table: "ParticipantShow");

            migrationBuilder.RenameTable(
                name: "ParticipantShow",
                newName: "ParticipantShows");

            migrationBuilder.RenameIndex(
                name: "IX_ParticipantShow_ShowId",
                table: "ParticipantShows",
                newName: "IX_ParticipantShows_ShowId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParticipantShows",
                table: "ParticipantShows",
                columns: new[] { "ParticipantId", "ShowId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantShows_Participants_ParticipantId",
                table: "ParticipantShows",
                column: "ParticipantId",
                principalTable: "Participants",
                principalColumn: "ParticipantId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantShows_Shows_ShowId",
                table: "ParticipantShows",
                column: "ShowId",
                principalTable: "Shows",
                principalColumn: "ShowId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantShows_Participants_ParticipantId",
                table: "ParticipantShows");

            migrationBuilder.DropForeignKey(
                name: "FK_ParticipantShows_Shows_ShowId",
                table: "ParticipantShows");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParticipantShows",
                table: "ParticipantShows");

            migrationBuilder.RenameTable(
                name: "ParticipantShows",
                newName: "ParticipantShow");

            migrationBuilder.RenameIndex(
                name: "IX_ParticipantShows_ShowId",
                table: "ParticipantShow",
                newName: "IX_ParticipantShow_ShowId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParticipantShow",
                table: "ParticipantShow",
                columns: new[] { "ParticipantId", "ShowId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantShow_Participants_ParticipantId",
                table: "ParticipantShow",
                column: "ParticipantId",
                principalTable: "Participants",
                principalColumn: "ParticipantId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParticipantShow_Shows_ShowId",
                table: "ParticipantShow",
                column: "ShowId",
                principalTable: "Shows",
                principalColumn: "ShowId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
