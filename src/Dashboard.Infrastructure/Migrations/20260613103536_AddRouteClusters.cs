using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteClusters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RouteClusterId",
                table: "RunActivities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RouteMatchedUtc",
                table: "RunActivities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RouteClusters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RepresentativeRunId = table.Column<long>(type: "bigint", nullable: false),
                    RepresentativeDistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteClusters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunActivities_RouteClusterId",
                table: "RunActivities",
                column: "RouteClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_RunActivities_RouteMatchedUtc",
                table: "RunActivities",
                column: "RouteMatchedUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteClusters");

            migrationBuilder.DropIndex(
                name: "IX_RunActivities_RouteClusterId",
                table: "RunActivities");

            migrationBuilder.DropIndex(
                name: "IX_RunActivities_RouteMatchedUtc",
                table: "RunActivities");

            migrationBuilder.DropColumn(
                name: "RouteClusterId",
                table: "RunActivities");

            migrationBuilder.DropColumn(
                name: "RouteMatchedUtc",
                table: "RunActivities");
        }
    }
}
