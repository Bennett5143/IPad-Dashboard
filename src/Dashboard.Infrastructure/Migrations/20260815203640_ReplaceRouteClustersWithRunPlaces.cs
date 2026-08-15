using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRouteClustersWithRunPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteClusters");

            migrationBuilder.RenameColumn(
                name: "RouteMatchedUtc",
                table: "RunActivities",
                newName: "PlaceAssignedUtc");

            migrationBuilder.RenameColumn(
                name: "RouteClusterId",
                table: "RunActivities",
                newName: "PlaceId");

            migrationBuilder.RenameIndex(
                name: "IX_RunActivities_RouteMatchedUtc",
                table: "RunActivities",
                newName: "IX_RunActivities_PlaceAssignedUtc");

            migrationBuilder.RenameIndex(
                name: "IX_RunActivities_RouteClusterId",
                table: "RunActivities",
                newName: "IX_RunActivities_PlaceId");

            migrationBuilder.CreateTable(
                name: "RunPlaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CentreLatitude = table.Column<double>(type: "double precision", nullable: false),
                    CentreLongitude = table.Column<double>(type: "double precision", nullable: false),
                    MinLatitude = table.Column<double>(type: "double precision", nullable: false),
                    MinLongitude = table.Column<double>(type: "double precision", nullable: false),
                    MaxLatitude = table.Column<double>(type: "double precision", nullable: false),
                    MaxLongitude = table.Column<double>(type: "double precision", nullable: false),
                    RunCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunPlaces", x => x.Id);
                });

            // Die Spalten wurden umbenannt, nicht neu angelegt — in ihnen stünden sonst die alten
            // Cluster-Ids und jeder Lauf gälte als zugeordnet, an einen Ort, den es nicht gibt.
            // Zurücksetzen heißt: der Hintergrund-Job ordnet alle Läufe neu zu, lokal aus den
            // gespeicherten Strecken, ohne Strava.
            migrationBuilder.Sql(
                """UPDATE "RunActivities" SET "PlaceId" = NULL, "PlaceAssignedUtc" = NULL;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunPlaces");

            migrationBuilder.RenameColumn(
                name: "PlaceId",
                table: "RunActivities",
                newName: "RouteClusterId");

            migrationBuilder.RenameColumn(
                name: "PlaceAssignedUtc",
                table: "RunActivities",
                newName: "RouteMatchedUtc");

            migrationBuilder.RenameIndex(
                name: "IX_RunActivities_PlaceId",
                table: "RunActivities",
                newName: "IX_RunActivities_RouteClusterId");

            migrationBuilder.RenameIndex(
                name: "IX_RunActivities_PlaceAssignedUtc",
                table: "RunActivities",
                newName: "IX_RunActivities_RouteMatchedUtc");

            migrationBuilder.CreateTable(
                name: "RouteClusters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RepresentativeDistanceMeters = table.Column<double>(type: "double precision", nullable: false),
                    RepresentativeRunId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteClusters", x => x.Id);
                });
        }
    }
}
