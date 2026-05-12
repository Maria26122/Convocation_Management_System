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
                name: "FK_DistributionLog_UserAccount_StaffUserAccountId",
                table: "DistributionLog");

            // Rename the column and its index
            migrationBuilder.RenameColumn(
                name: "StaffUserAccountId",
                table: "DistributionLog",
                newName: "UserAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_DistributionLog_StaffUserAccountId",
                table: "DistributionLog",
                newName: "IX_DistributionLog_UserAccountId");

            // Recreate foreign key with the new column name
            migrationBuilder.AddForeignKey(
                name: "FK_DistributionLog_UserAccount_UserAccountId",
                table: "DistributionLog",
                column: "UserAccountId",
                principalTable: "UserAccount",
                principalColumn: "UserAccountId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DistributionLog_UserAccount_UserAccountId",
                table: "DistributionLog");

            migrationBuilder.RenameIndex(
                name: "IX_DistributionLog_UserAccountId",
                table: "DistributionLog",
                newName: "IX_DistributionLog_StaffUserAccountId");

            migrationBuilder.RenameColumn(
                name: "UserAccountId",
                table: "DistributionLog",
                newName: "StaffUserAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_DistributionLog_UserAccount_StaffUserAccountId",
                table: "DistributionLog",
                column: "StaffUserAccountId",
                principalTable: "UserAccount",
                principalColumn: "UserAccountId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
