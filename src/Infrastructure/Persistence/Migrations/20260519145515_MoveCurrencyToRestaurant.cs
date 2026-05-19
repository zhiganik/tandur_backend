using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveCurrencyToRestaurant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "MenuItems");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Restaurants",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Restaurants");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "MenuItems",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");
        }
    }
}
