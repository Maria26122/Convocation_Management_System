using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convocation.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixRoleAndDistribution : Migration
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

            migrationBuilder.DropIndex(
                name: "IX_DistributionLog_RegistrationId",
                table: "DistributionLog");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "StaffTask");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "StaffTask");

            migrationBuilder.DropColumn(
                name: "TaskTitle",
                table: "StaffTask");

            migrationBuilder.DropColumn(
                name: "AssignedStaffId",
                table: "DistributionTask");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StaffTask",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "OperationActivityLog",
                columns: table => new
                {
                    OperationActivityLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationActivityLog", x => x.OperationActivityLogId);
                    table.ForeignKey(
                        name: "FK_OperationActivityLog_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccount",
                        principalColumn: "UserAccountId");
                });

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "RoleName",
                value: "admin");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "RoleName",
                value: " eventmanager");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "RoleName",
                value: "staff");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "RoleName",
                value: "student");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionLog_RegistrationId_ActionType",
                table: "DistributionLog",
                columns: new[] { "RegistrationId", "ActionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationActivityLog_UserAccountId",
                table: "OperationActivityLog",
                column: "UserAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationActivityLog");

            migrationBuilder.DropIndex(
                name: "IX_DistributionLog_RegistrationId_ActionType",
                table: "DistributionLog");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StaffTask",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "StaffTask",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "StaffTask",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaskTitle",
                table: "StaffTask",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AssignedStaffId",
                table: "DistributionTask",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "RoleName",
                value: "Admin");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "RoleName",
                value: "Event Manager");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "RoleName",
                value: "Staff");

            migrationBuilder.UpdateData(
                table: "Role",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "RoleName",
                value: "Student");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionTask_AssignedStaffId",
                table: "DistributionTask",
                column: "AssignedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionLog_RegistrationId",
                table: "DistributionLog",
                column: "RegistrationId");

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
