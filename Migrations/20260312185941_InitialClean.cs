using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bevera.Migrations
{
    /// <inheritdoc />
    public partial class InitialClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Distributors",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_ApplicationUserId",
                table: "Distributors",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_AspNetUsers_ApplicationUserId",
                table: "Distributors",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_AspNetUsers_ApplicationUserId",
                table: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_ApplicationUserId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Distributors");
        }
    }
}
