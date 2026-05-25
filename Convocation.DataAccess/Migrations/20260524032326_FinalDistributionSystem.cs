using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convocation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FinalDistributionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffTask_DistributionTask_DistributionTaskId",
                table: "StaffTask");

            migrationBuilder.DropColumn(
                name: "CounterName",
                table: "DistributionTask");

            migrationBuilder.DropColumn(
                name: "DistributionDate",
                table: "DistributionTask");

            migrationBuilder.DropColumn(
                name: "IsQrRequired",
                table: "DistributionTask");

            migrationBuilder.DropColumn(
                name: "TotalDistributed",
                table: "DistributionTask");

            migrationBuilder.DropColumn(
                name: "TotalPending",
                table: "DistributionTask");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DistributionTask",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "DistributionType",
                table: "DistributionTask",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<int>(
                name: "AssignedStaffId",
                table: "DistributionTask",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "DistributionTask",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistributionTask_AssignedStaffId",
                table: "DistributionTask",
                column: "AssignedStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionTask_UserAccount_AssignedStaffId",
                table: "DistributionTask",
                column: "AssignedStaffId",
                principalTable: "UserAccount",
                principalColumn: "UserAccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffTask_DistributionTask_DistributionTaskId",
                table: "StaffTask",
                column: "DistributionTaskId",
                principalTable: "DistributionTask",
                principalColumn: "DistributionTaskId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionTask_UserAccount_AssignedStaffId",
                table: "DistributionTask");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffTask_DistributionTask_DistributionTaskId",
                table: "StaffTask");

            migrationBuilder.DropIndex(
                name: "IX_DistributionTask_AssignedStaffId",
                table: "DistributionTask");

            migrationBuilder.DropColumn(
                name: "AssignedStaffId",
                table: "DistributionTask");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "DistributionTask");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DistributionTask",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DistributionType",
                table: "DistributionTask",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "CounterName",
                table: "DistributionTask",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DistributionDate",
                table: "DistributionTask",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsQrRequired",
                table: "DistributionTask",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalDistributed",
                table: "DistributionTask",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPending",
                table: "DistributionTask",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffTask_DistributionTask_DistributionTaskId",
                table: "StaffTask",
                column: "DistributionTaskId",
                principalTable: "DistributionTask",
                principalColumn: "DistributionTaskId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
