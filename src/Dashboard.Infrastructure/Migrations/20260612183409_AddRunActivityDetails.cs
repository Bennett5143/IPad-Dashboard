using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunActivityDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DetailsBackfilledUtc",
                table: "SyncStates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AverageHeartRate",
                table: "RunActivities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ElevationGainMeters",
                table: "RunActivities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxHeartRate",
                table: "RunActivities",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DetailsBackfilledUtc",
                table: "SyncStates");

            migrationBuilder.DropColumn(
                name: "AverageHeartRate",
                table: "RunActivities");

            migrationBuilder.DropColumn(
                name: "ElevationGainMeters",
                table: "RunActivities");

            migrationBuilder.DropColumn(
                name: "MaxHeartRate",
                table: "RunActivities");
        }
    }
}
