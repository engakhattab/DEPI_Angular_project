using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HR.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase5HrBusinessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VacationRequests_EmployeeId",
                table: "VacationRequests");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "VacationRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByEmployeeId",
                table: "VacationRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkingDayCount",
                table: "VacationRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedByEmployeeId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TerminatedAt",
                table: "Employees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VacationBalanceDays",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 21);

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_EmployeeId_Status_StartDate_EndDate",
                table: "VacationRequests",
                columns: new[] { "EmployeeId", "Status", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_ReviewedByEmployeeId",
                table: "VacationRequests",
                column: "ReviewedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_RequestedByEmployeeId",
                table: "Trips",
                column: "RequestedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email_IsDeleted_Status",
                table: "Employees",
                columns: new[] { "Email", "IsDeleted", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Employees_RequestedByEmployeeId",
                table: "Trips",
                column: "RequestedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VacationRequests_Employees_ReviewedByEmployeeId",
                table: "VacationRequests",
                column: "ReviewedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Employees_RequestedByEmployeeId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_VacationRequests_Employees_ReviewedByEmployeeId",
                table: "VacationRequests");

            migrationBuilder.DropIndex(
                name: "IX_VacationRequests_EmployeeId_Status_StartDate_EndDate",
                table: "VacationRequests");

            migrationBuilder.DropIndex(
                name: "IX_VacationRequests_ReviewedByEmployeeId",
                table: "VacationRequests");

            migrationBuilder.DropIndex(
                name: "IX_Trips_RequestedByEmployeeId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Email_IsDeleted_Status",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "VacationRequests");

            migrationBuilder.DropColumn(
                name: "ReviewedByEmployeeId",
                table: "VacationRequests");

            migrationBuilder.DropColumn(
                name: "WorkingDayCount",
                table: "VacationRequests");

            migrationBuilder.DropColumn(
                name: "RequestedByEmployeeId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TerminatedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "VacationBalanceDays",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_VacationRequests_EmployeeId",
                table: "VacationRequests",
                column: "EmployeeId");
        }
    }
}
