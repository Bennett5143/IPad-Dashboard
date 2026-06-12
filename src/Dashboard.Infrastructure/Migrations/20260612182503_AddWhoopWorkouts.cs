using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWhoopWorkouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhoopWorkouts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Sport = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    HighIntensityShare = table.Column<double>(type: "double precision", nullable: false),
                    Strain = table.Column<double>(type: "double precision", nullable: true),
                    Kilojoule = table.Column<double>(type: "double precision", nullable: true),
                    AverageHeartRate = table.Column<int>(type: "integer", nullable: true),
                    MaxHeartRate = table.Column<int>(type: "integer", nullable: true),
                    Zone0Milli = table.Column<long>(type: "bigint", nullable: true),
                    Zone1Milli = table.Column<long>(type: "bigint", nullable: true),
                    Zone2Milli = table.Column<long>(type: "bigint", nullable: true),
                    Zone3Milli = table.Column<long>(type: "bigint", nullable: true),
                    Zone4Milli = table.Column<long>(type: "bigint", nullable: true),
                    Zone5Milli = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhoopWorkouts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhoopWorkouts_StartUtc",
                table: "WhoopWorkouts",
                column: "StartUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhoopWorkouts");
        }
    }
}
