using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuItems_RestaurantId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_Categories_RestaurantId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "ix_restaurants_isactive_createdat",
                table: "Restaurants",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_restaurants_name",
                table: "Restaurants",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "ix_menuitems_restaurantid_available_sortorder",
                table: "MenuItems",
                columns: new[] { "RestaurantId", "IsActive", "IsAvailable", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_menuitems_restaurantid_sortorder",
                table: "MenuItems",
                columns: new[] { "RestaurantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_restaurantid_isvisible_sortorder",
                table: "Categories",
                columns: new[] { "RestaurantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_restaurantid_sortorder",
                table: "Categories",
                columns: new[] { "RestaurantId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_users_firstname_createdat",
                table: "AspNetUsers",
                columns: new[] { "FirstName", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_users_lastname_createdat",
                table: "AspNetUsers",
                columns: new[] { "LastName", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_users_phonenumber",
                table: "AspNetUsers",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_restaurants_isactive_createdat",
                table: "Restaurants");

            migrationBuilder.DropIndex(
                name: "ix_restaurants_name",
                table: "Restaurants");

            migrationBuilder.DropIndex(
                name: "ix_menuitems_restaurantid_available_sortorder",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "ix_menuitems_restaurantid_sortorder",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "ix_categories_restaurantid_isvisible_sortorder",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "ix_categories_restaurantid_sortorder",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "ix_users_firstname_createdat",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "ix_users_lastname_createdat",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "ix_users_phonenumber",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_RestaurantId",
                table: "MenuItems",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_RestaurantId",
                table: "Categories",
                column: "RestaurantId");
        }
    }
}
