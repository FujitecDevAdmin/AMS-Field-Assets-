using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Modules.Organization.Persistence.Migrations;

/// <inheritdoc />
public partial class _20260817_Organization_SeedBranches : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            SET IDENTITY_INSERT [Organization].[Branch] ON;

            INSERT INTO [Organization].[Branch]
                ([Id], [BranchCode], [BranchName], [RegionId], [TimeZoneId], [IsHeadOffice], [IsActive], [CreatedOnUtc], [CreatedBy])
            VALUES
                (1,  N'BR001', N'Fujitec India Pvt Ltd',       NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (2,  N'BR002', N'Fujitec Factory Office',     NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (3,  N'BR003', N'Fujitec Uttar Pradesh',      NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (4,  N'BR004', N'Central Service Warehouse',  NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (5,  N'BR005', N'Fujitec Pune-1',             NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (6,  N'BR006', N'Fujitec Mumbai1',            NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (7,  N'BR007', N'Fujitec Hyderabad',          NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (8,  N'BR008', N'Fujitec Chennai',            NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (9,  N'BR009', N'Fujitec Bangalore',          NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (10, N'BR010', N'Fujitec Mumbai3 (NVM)',      NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (11, N'BR011', N'Fujitec Cochin',             NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (12, N'BR012', N'Fujitec Haryana-1',          NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (13, N'BR013', N'Fujitec Delhi',              NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (14, N'BR014', N'Delivery Center',            NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (15, N'BR015', N'Fujitec NIB Head Office',    NULL, N'India Standard Time', 1, 1, SYSUTCDATETIME(), N'System Seed'),
                (16, N'BR016', N'Fujitec Goa',                NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (17, N'BR017', N'Fujitec Jaipur',             NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (18, N'BR018', N'Fujitec Kolkata',            NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (19, N'BR019', N'Fujitec Punjab',             NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (20, N'BR020', N'Fujitec Ahmedabad',          NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (21, N'BR021', N'Fujitec Andhra Pradesh',     NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (22, N'BR022', N'Fujitec Mumbai2',            NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (23, N'BR023', N'Fujitec CMRL',               NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed'),
                (24, N'BR024', N'Fujitec Odisha',             NULL, N'India Standard Time', 0, 1, SYSUTCDATETIME(), N'System Seed');

            SET IDENTITY_INSERT [Organization].[Branch] OFF;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM [Organization].[Branch]
            WHERE [Id] BETWEEN 1 AND 24
              AND [CreatedBy] = N'System Seed';
            """);
    }
}
