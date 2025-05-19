using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CampusBites.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchemaAndData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "Id",
                keyValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "Category", "Description", "ImageUrl", "IsAvailable", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Food", "Classic beef burger with lettuce, tomato, and cheese", "/uploads/menu-items/burger.jpg", true, "Campus Burger", 5.99m },
                    { 2, "Food", "Grilled chicken with fresh veggies in a soft tortilla", "/uploads/menu-items/wrap.jpg", true, "Chicken Wrap", 6.49m },
                    { 3, "Drinks", "Refreshing cola drink", "/uploads/menu-items/coke.jpg", true, "Cola", 1.50m },
                    { 4, "Snacks", "Salted potato chips", "/uploads/menu-items/chips.jpg", true, "Chips", 1.00m }
                });
        }
    }
}
