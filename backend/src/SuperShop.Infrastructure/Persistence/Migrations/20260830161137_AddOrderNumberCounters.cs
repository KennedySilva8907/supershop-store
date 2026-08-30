using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SuperShop.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumberCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderNumberCounters",
                columns: table => new
                {
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Next = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderNumberCounters", x => x.Year);
                });

            migrationBuilder.Sql("""
                INSERT INTO "OrderNumberCounters" ("Year", "Next")
                SELECT CAST(SUBSTRING("OrderNumber" FROM 4 FOR 4) AS integer),
                       MAX(CAST(SUBSTRING("OrderNumber" FROM 9) AS integer))
                FROM "Orders"
                GROUP BY CAST(SUBSTRING("OrderNumber" FROM 4 FOR 4) AS integer);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderNumberCounters");
        }
    }
}
