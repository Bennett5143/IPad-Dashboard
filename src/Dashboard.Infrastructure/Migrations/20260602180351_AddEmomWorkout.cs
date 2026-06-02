using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dashboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmomWorkout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmomWorkout",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HabitEntryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmomWorkout", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmomWorkout_HabitEntries_HabitEntryId",
                        column: x => x.HabitEntryId,
                        principalTable: "HabitEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmomSegment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmomWorkoutId = table.Column<int>(type: "integer", nullable: false),
                    FromMinute = table.Column<int>(type: "integer", nullable: false),
                    ToMinute = table.Column<int>(type: "integer", nullable: false),
                    PushupsPerMinute = table.Column<int>(type: "integer", nullable: false),
                    PullupsPerMinute = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmomSegment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmomSegment_EmomWorkout_EmomWorkoutId",
                        column: x => x.EmomWorkoutId,
                        principalTable: "EmomWorkout",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmomSegment_EmomWorkoutId",
                table: "EmomSegment",
                column: "EmomWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_EmomWorkout_HabitEntryId",
                table: "EmomWorkout",
                column: "HabitEntryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmomSegment");

            migrationBuilder.DropTable(
                name: "EmomWorkout");
        }
    }
}
