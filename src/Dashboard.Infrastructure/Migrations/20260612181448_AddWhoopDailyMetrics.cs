using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWhoopDailyMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhoopDailyMetrics",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    RecoveryScore = table.Column<int>(type: "integer", nullable: true),
                    HrvMillis = table.Column<double>(type: "double precision", nullable: true),
                    RestingHeartRate = table.Column<int>(type: "integer", nullable: true),
                    SleepHours = table.Column<double>(type: "double precision", nullable: true),
                    SleepPerformance = table.Column<int>(type: "integer", nullable: true),
                    DayStrain = table.Column<double>(type: "double precision", nullable: true),
                    LightSleepHours = table.Column<double>(type: "double precision", nullable: true),
                    DeepSleepHours = table.Column<double>(type: "double precision", nullable: true),
                    RemSleepHours = table.Column<double>(type: "double precision", nullable: true),
                    AwakeHours = table.Column<double>(type: "double precision", nullable: true),
                    SleepStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SleepEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RespiratoryRate = table.Column<double>(type: "double precision", nullable: true),
                    Spo2Percentage = table.Column<double>(type: "double precision", nullable: true),
                    SkinTempCelsius = table.Column<double>(type: "double precision", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhoopDailyMetrics", x => x.Date);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhoopDailyMetrics");
        }
    }
}
