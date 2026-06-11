using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunStreams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double[]>(
                name: "AltitudesMeters",
                table: "RunActivities",
                type: "double precision[]",
                nullable: true);

            migrationBuilder.AddColumn<int[]>(
                name: "HeartRates",
                table: "RunActivities",
                type: "integer[]",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StreamsFetched",
                table: "RunActivities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int[]>(
                name: "TimeOffsetsSeconds",
                table: "RunActivities",
                type: "integer[]",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunActivities_StreamsFetched",
                table: "RunActivities",
                column: "StreamsFetched");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RunActivities_StreamsFetched",
                table: "RunActivities");

            migrationBuilder.DropColumn(
                name: "AltitudesMeters",
                table: "RunActivities");

            migrationBuilder.DropColumn(
                name: "HeartRates",
                table: "RunActivities");

            migrationBuilder.DropColumn(
                name: "StreamsFetched",
                table: "RunActivities");

            migrationBuilder.DropColumn(
                name: "TimeOffsetsSeconds",
                table: "RunActivities");
        }
    }
}
