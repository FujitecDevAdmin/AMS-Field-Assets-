using AMS.Modules.Verification.Features.CloseVerificationCycle;
using AMS.Modules.Verification.Features.CalculateAuditAssetCount;
using AMS.Modules.Verification.Features.OpenVerificationCycle;
using AMS.Modules.Verification.Features.SearchVerificationCycles;
using AMS.Modules.Verification.Features.SearchAuditBranches;
using AMS.Modules.Verification.Features.SearchAuditAssets;
using AMS.Modules.Verification.Features.SearchVerifications;
using AMS.Modules.Verification.Features.SearchMyAudits;
using AMS.Modules.Verification.Features.ResolveAuditScan;
using AMS.Modules.Verification.Features.GetLatestAssetVerification;
using AMS.Modules.Verification.Features.AuditorVerificationActivity;
using AMS.Modules.Verification.Features.SubmitVerification;
using AMS.Modules.Verification.Features.AddAuditorsToCycle;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Persistence.Transactions;
using AMS.SharedKernel.Web.Http;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AMS.Modules.Verification;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
/// <remarks>
/// The handbook's "asset audit", done properly: QR scan, GPS, photo and a
/// working-condition judgement, captured offline on a phone and synced.
/// </remarks>
public static class VerificationModuleExtensions
{
    public static IServiceCollection AddVerificationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<VerificationDbContext>(VerificationDbContext.SchemaName);

        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<SubmitVerificationValidator>(
            ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_PhysicalVerificationCycle_Name", "VerificationCycle.NameTaken",
                "A cycle with that name already exists.")
            // R2-21: THIS is the retry. The same device sent the same capture
            // again, and the answer is the row it already made.
            .Register("UX_PhysicalVerification_ClientCapture", "Verification.AlreadyCaptured",
                "That capture has already been recorded.")
            // And this is a real conflict: somebody else got to the asset first.
            .Register("UX_PhysicalVerification_OnePerUnitAssetPerCycle", "Verification.AlreadyVerified",
                "Somebody has already verified this asset in the current cycle.")
            // R3: a bulk line is counted once per PLACE, not once per cycle.
            // Counting the same line at four branches is the correct answer.
            .Register("UX_PhysicalVerification_OneBulkCountPerPlacePerCycle", "Verification.AlreadyCounted",
                "This line has already been counted at that location in the current cycle."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapVerificationModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/verification")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationEndpointFilter>();

        SearchVerificationCyclesEndpoint.Map(group);
        SearchAuditAssetsEndpoint.Map(group);
        SearchAuditBranchesEndpoint.Map(group);
        CalculateAuditAssetCountEndpoint.Map(group);
        SearchMyAuditsEndpoint.Map(group);
        ResolveAuditScanEndpoint.Map(group);
        GetLatestAssetVerificationEndpoint.Map(group);
        AuditorVerificationActivityEndpoints.Map(group);
        OpenVerificationCycleEndpoint.Map(group);
        AddAuditorsToCycleEndpoint.Map(group);
        CloseVerificationCycleEndpoint.Map(group);

        SubmitVerificationEndpoint.Map(group);
        SearchVerificationsEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchVerificationCyclesQuery, SearchVerificationCyclesResponse>, SearchVerificationCyclesHandler>();
        services.AddScoped<IRequestHandler<SearchAuditAssetsQuery, SearchAuditAssetsResponse>, SearchAuditAssetsHandler>();
        services.AddScoped<IRequestHandler<SearchAuditBranchesQuery, SearchAuditBranchesResponse>, SearchAuditBranchesHandler>();
        services.AddScoped<IRequestHandler<CalculateAuditAssetCountQuery, CalculateAuditAssetCountResponse>, CalculateAuditAssetCountHandler>();
        services.AddScoped<IRequestHandler<SearchMyAuditsQuery, SearchMyAuditsResponse>, SearchMyAuditsHandler>();
        services.AddScoped<IRequestHandler<ResolveAuditScanQuery, ResolveAuditScanResponse>, ResolveAuditScanHandler>();
        services.AddScoped<IRequestHandler<GetLatestAssetVerificationQuery, GetLatestAssetVerificationResponse>, GetLatestAssetVerificationHandler>();
        services.AddScoped<IRequestHandler<SearchAuditorVerificationCountsQuery, SearchAuditorVerificationCountsResponse>, SearchAuditorVerificationCountsHandler>();
        services.AddScoped<IRequestHandler<GetAuditorVerificationActivityQuery, GetAuditorVerificationActivityResponse>, GetAuditorVerificationActivityHandler>();
        services.AddScoped<IRequestHandler<OpenVerificationCycleCommand, OpenVerificationCycleResponse>, OpenVerificationCycleHandler>();
        services.AddScoped<IRequestHandler<AddAuditorsToCycleCommand, AddAuditorsToCycleResponse>, AddAuditorsToCycleHandler>();
        services.AddScoped<IRequestHandler<CloseVerificationCycleCommand, CloseVerificationCycleResponse>, CloseVerificationCycleHandler>();

        services.AddScoped<IRequestHandler<SubmitVerificationCommand, SubmitVerificationResponse>, SubmitVerificationHandler>();
        services.AddScoped<IRequestHandler<SearchVerificationsQuery, SearchVerificationsResponse>, SearchVerificationsHandler>();
    }
}
