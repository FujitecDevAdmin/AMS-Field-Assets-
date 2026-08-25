using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Verification.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260824_Verification_AddGpsValidationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AllowedRadiusMetres",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanceFromLocationMetres",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GpsAccuracyMetres",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "decimal(9,2)",
                precision: 9,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpsValidationMessage",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpsValidationStatus",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasLocationMismatch",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "bit",
                nullable: false,
                defaultValueSql: "0")
                .Annotation("Relational:DefaultConstraintName", "DF_PhysicalVerification_HasLocationMismatch");

            migrationBuilder.AddColumn<bool>(
                name: "IsMockLocation",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferenceLatitude",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferenceLongitude",
                schema: "Verification",
                table: "PhysicalVerification",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhysicalVerification_AllowedRadius",
                schema: "Verification",
                table: "PhysicalVerification",
                sql: "([AllowedRadiusMetres] IS NULL OR [AllowedRadiusMetres] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhysicalVerification_Distance",
                schema: "Verification",
                table: "PhysicalVerification",
                sql: "([DistanceFromLocationMetres] IS NULL OR [DistanceFromLocationMetres] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhysicalVerification_GpsAccuracy",
                schema: "Verification",
                table: "PhysicalVerification",
                sql: "([GpsAccuracyMetres] IS NULL OR [GpsAccuracyMetres] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhysicalVerification_GpsValidationStatus",
                schema: "Verification",
                table: "PhysicalVerification",
                sql: "([GpsValidationStatus] IS NULL OR [GpsValidationStatus] IN (N'NotValidated', N'InsideGeofence', N'OutsideGeofence', N'ReferenceUnavailable'))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhysicalVerification_ReferenceLatitude",
                schema: "Verification",
                table: "PhysicalVerification",
                sql: "([ReferenceLatitude] IS NULL OR ([ReferenceLatitude] >= -90 AND [ReferenceLatitude] <= 90))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PhysicalVerification_ReferenceLongitude",
                schema: "Verification",
                table: "PhysicalVerification",
                sql: "([ReferenceLongitude] IS NULL OR ([ReferenceLongitude] >= -180 AND [ReferenceLongitude] <= 180))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PhysicalVerification_AllowedRadius",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhysicalVerification_Distance",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhysicalVerification_GpsAccuracy",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhysicalVerification_GpsValidationStatus",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhysicalVerification_ReferenceLatitude",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PhysicalVerification_ReferenceLongitude",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "AllowedRadiusMetres",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "DistanceFromLocationMetres",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "GpsAccuracyMetres",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "GpsValidationMessage",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "GpsValidationStatus",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "HasLocationMismatch",
                schema: "Verification",
                table: "PhysicalVerification")
                .Annotation("Relational:DefaultConstraintName", "DF_PhysicalVerification_HasLocationMismatch");

            migrationBuilder.DropColumn(
                name: "IsMockLocation",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "ReferenceLatitude",
                schema: "Verification",
                table: "PhysicalVerification");

            migrationBuilder.DropColumn(
                name: "ReferenceLongitude",
                schema: "Verification",
                table: "PhysicalVerification");
        }
    }
}
