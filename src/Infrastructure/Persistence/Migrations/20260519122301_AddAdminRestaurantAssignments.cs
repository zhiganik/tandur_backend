using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminRestaurantAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminRestaurantAssignments",
                columns: table => new
                {
                    AssignedAdminsId = table.Column<string>(type: "text", nullable: false),
                    AssignedRestaurantsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRestaurantAssignments", x => new { x.AssignedAdminsId, x.AssignedRestaurantsId });
                    table.ForeignKey(
                        name: "FK_AdminRestaurantAssignments_AspNetUsers_AssignedAdminsId",
                        column: x => x.AssignedAdminsId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdminRestaurantAssignments_Restaurants_AssignedRestaurantsId",
                        column: x => x.AssignedRestaurantsId,
                        principalTable: "Restaurants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminRestaurantAssignments_AssignedRestaurantsId",
                table: "AdminRestaurantAssignments",
                column: "AssignedRestaurantsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminRestaurantAssignments");
        }
    }
}
