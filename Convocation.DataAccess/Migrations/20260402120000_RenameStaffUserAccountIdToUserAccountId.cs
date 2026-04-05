using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Convocation.DataAccess.Migrations
{
    public partial class RenameStaffUserAccountIdToUserAccountId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing foreign key that references the old column name
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionLogs_UserAccounts_StaffUserAccountId",
                table: "DistributionLogs");

            // Rename the column and its index
            migrationBuilder.RenameColumn(
                name: "StaffUserAccountId",
                table: "DistributionLogs",
                newName: "UserAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributionLogs_StaffUserAccountId",
                table: "DistributionLogs",
                newName: "IX_DistributionLogs_UserAccountId");

            // Recreate foreign key with the new column name
            migrationBuilder.AddForeignKey(
                name: "FK_DistributionLogs_UserAccounts_UserAccountId",
                table: "DistributionLogs",
                column: "UserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "UserAccountId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionLogs_UserAccounts_UserAccountId",
                table: "DistributionLogs");

            migrationBuilder.RenameIndex(
                name: "IX_DistributionLogs_UserAccountId",
                table: "DistributionLogs",
                newName: "IX_DistributionLogs_StaffUserAccountId");

            migrationBuilder.RenameColumn(
                name: "UserAccountId",
                table: "DistributionLogs",
                newName: "StaffUserAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionLogs_UserAccounts_StaffUserAccountId",
                table: "DistributionLogs",
                column: "StaffUserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "UserAccountId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
