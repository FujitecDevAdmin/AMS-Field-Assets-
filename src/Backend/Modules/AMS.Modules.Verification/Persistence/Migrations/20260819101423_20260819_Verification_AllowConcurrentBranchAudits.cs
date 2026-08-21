using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Verification.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260819_Verification_AllowConcurrentBranchAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PhysicalVerificationCycle_OneActivePerBranch",
                schema: "Verification",
                table: "PhysicalVerificationCycle");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_PhysicalVerificationCycle_OneActivePerBranch",
                schema: "Verification",
                table: "PhysicalVerificationCycle",
                columns: new[] { "BranchId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
