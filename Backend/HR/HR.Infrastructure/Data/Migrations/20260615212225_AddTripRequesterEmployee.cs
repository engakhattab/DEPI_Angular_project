using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTripRequesterEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequesterEmployeeId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_RequesterEmployeeId",
                table: "Trips",
                column: "RequesterEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Employees_RequesterEmployeeId",
                table: "Trips",
                column: "RequesterEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Employees_RequesterEmployeeId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_RequesterEmployeeId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RequesterEmployeeId",
                table: "Trips");
        }
    }
}
