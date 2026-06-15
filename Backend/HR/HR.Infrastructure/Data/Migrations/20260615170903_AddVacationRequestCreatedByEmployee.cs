using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVacationRequestCreatedByEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByEmployeeId",
                table: "VacationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_CreatedByEmployeeId",
                table: "VacationRequests",
                column: "CreatedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_VacationRequests_Employees_CreatedByEmployeeId",
                table: "VacationRequests",
                column: "CreatedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VacationRequests_Employees_CreatedByEmployeeId",
                table: "VacationRequests");

            migrationBuilder.DropIndex(
                name: "IX_VacationRequests_CreatedByEmployeeId",
                table: "VacationRequests");

            migrationBuilder.DropColumn(
                name: "CreatedByEmployeeId",
                table: "VacationRequests");
        }
    }
}
