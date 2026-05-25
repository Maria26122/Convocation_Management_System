using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convocation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DistributionSystemUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionTask_UserAccount_AssignedStaffId",
                table: "DistributionTask");

            migrationBuilder.DropIndex(
                name: "IX_DistributionTask_AssignedStaffId",
                table: "DistributionTask");

            migrationBuilder.DropColumn(
                name: "AssignedStaffId",
                table: "DistributionTask");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StaffTask",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "StaffTask",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaskTitle",
                table: "DistributionTask",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DistributionTask",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DistributionTask",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
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

            migrationBuilder.AddColumn<string>(
                name: "DistributionType",
                table: "DistributionTask",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "DistributionLog",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "DistributionLog",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DistributionTaskId",
                table: "DistributionLog",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "DistributionLog",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsDelivered",
                table: "DistributionLog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsQrVerified",
                table: "DistributionLog",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_StaffTask_DistributionTaskId",
                table: "StaffTask",
                column: "DistributionTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionLog_DistributionTaskId",
                table: "DistributionLog",
                column: "DistributionTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionLog_DistributionTask_DistributionTaskId",
                table: "DistributionLog",
                column: "DistributionTaskId",
                principalTable: "DistributionTask",
                principalColumn: "DistributionTaskId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffTask_DistributionTask_DistributionTaskId",
                table: "StaffTask",
                column: "DistributionTaskId",
                principalTable: "DistributionTask",
                principalColumn: "DistributionTaskId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionLog_DistributionTask_DistributionTaskId",
                table: "DistributionLog");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffTask_DistributionTask_DistributionTaskId",
                table: "StaffTask");

            migrationBuilder.DropIndex(
                name: "IX_StaffTask_DistributionTaskId",
                table: "StaffTask");

            migrationBuilder.DropIndex(
                name: "IX_DistributionLog_DistributionTaskId",
                table: "DistributionLog");

            migrationBuilder.DropColumn(
                name: "CounterName",
                table: "DistributionTask");

            migrationBuilder.DropColumn(
                name: "DistributionDate",
                table: "DistributionTask");

            migrationBuilder.DropColumn(
                name: "DistributionType",
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

            migrationBuilder.DropColumn(
                name: "IsDelivered",
                table: "DistributionLog");

            migrationBuilder.DropColumn(
                name: "IsQrVerified",
                table: "DistributionLog");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StaffTask",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "StaffTask",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TaskTitle",
                table: "DistributionTask",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DistributionTask",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DistributionTask",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedStaffId",
                table: "DistributionTask",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "DistributionLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "DistributionLog",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DistributionTaskId",
                table: "DistributionLog",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActionType",
                table: "DistributionLog",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

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
        }
    }
}
