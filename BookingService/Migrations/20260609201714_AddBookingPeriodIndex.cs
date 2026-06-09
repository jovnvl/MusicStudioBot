using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPeriodIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        CREATE INDEX IF NOT EXISTS
        "IX_Bookings_RoomId_TimeBegin_TimeEnd"
        ON "Bookings"
        ("RoomId", "TimeBegin", "TimeEnd");
    """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        DROP INDEX IF EXISTS
        "IX_Bookings_RoomId_TimeBegin_TimeEnd";
    """);
        }
    }
}
