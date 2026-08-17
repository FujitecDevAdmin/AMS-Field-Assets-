using AMS.Modules.Assets.Features.CreateAssetClass;
using AMS.Modules.Assets.Features.CreateAssetStatus;
using AMS.Modules.Assets.Features.CreateAssetType;
using AMS.Modules.Assets.Features.CreateChartOfAccount;
using AMS.Modules.Assets.Features.CreateAuditorLocation;
using AMS.Modules.Assets.Features.DefineCustomField;
using AMS.Modules.Assets.Features.DeleteAsset;
using AMS.Modules.Assets.Features.GetAsset;
using AMS.Modules.Assets.Features.GetAssetDashboard;
using AMS.Modules.Assets.Features.GetAssetTimeline;
using AMS.Modules.Assets.Features.GetAssetTypeCustomFields;
using AMS.Modules.Assets.Features.ImportAssetsExcel;
using AMS.Modules.Assets.Features.ListAuditorLocations;
using AMS.Modules.Assets.Features.SaveAssetDetails;
using AMS.Modules.Assets.Features.SetAssetCustomValues;
using AMS.Modules.Assets.Features.RegisterAsset;
using AMS.Modules.Assets.Features.SearchAssetClasses;
using AMS.Modules.Assets.Features.SearchAssets;
using AMS.Modules.Assets.Features.UpdateAsset;
using AMS.Modules.Assets.Features.UpdateImportedAssetDetails;
using AMS.Modules.Assets.Features.SearchAssetStatuses;
using AMS.Modules.Assets.Features.SearchAssetTypes;
using AMS.Modules.Assets.Features.SearchChartOfAccounts;
using AMS.Modules.Assets.Features.UpdateAssetClass;
using AMS.Modules.Assets.Features.UpdateAssetStatus;
using AMS.Modules.Assets.Features.UpdateAssetType;
using AMS.Modules.Assets.Features.UpdateChartOfAccount;
using AMS.Modules.Assets.Features.UpdateCustomField;
using AMS.Modules.Assets.Persistence;
using AMS.Modules.Assets.PublicApi;
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

namespace AMS.Modules.Assets;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
public static class AssetsModuleExtensions
{
    public static IServiceCollection AddAssetsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Not AddDbContext with a connection string: the context is built on the
        // REQUEST'S connection so one transaction can span modules (rule 4a).
        // A context that opened its own connection could not take part.
        services.AddModuleDbContext<AssetsDbContext>(AssetsDbContext.SchemaName);

        // The rule 4a write contract. Other modules append to an asset's
        // timeline through this and never touch [Assets] tables directly.
        services.AddScoped<IAssetTimeline, AssetTimeline>();

        // The second write contract: Movements changes an asset's branch on
        // receipt, and may not touch [Assets] to do it.
        services.AddScoped<IAssetCustody, AssetCustody>();

        // The read side. Narrow on purpose: custody facts only, so a
        // snapshot never becomes the way other modules read the register.
        services.AddScoped<IAssetSnapshot, AssetSnapshotReader>();

        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<CreateAssetTypeValidator>(ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_AssetType_Name", "AssetType.NameTaken",
                "An asset type with that name already exists.")
            .Register("UX_AssetStatus_Name", "AssetStatus.NameTaken",
                "A status with that name already exists.")
            .Register("UX_AssetClass_Code", "AssetClass.CodeTaken",
                "An asset class with that code already exists.")
            .Register("UX_AssetClass_Name", "AssetClass.NameTaken",
                "An asset class with that name already exists.")
            // Reachable only if a class is inserted outside CreateAssetClass,
            // which never sets IsAuc. Registered anyway: an unregistered index
            // surfaces as a raw DbUpdateException, and "which index" is not a
            // question the API should have to guess at run time.
            .Register("UX_AssetClass_OneAuc", "AssetClass.AucExists",
                "There is already an assets-under-construction class.")
            .Register("UX_ChartOfAccount_Code", "ChartOfAccount.CodeTaken",
                "A chart-of-account code with that value already exists.")
            .Register("UX_CustomFieldDefinition_TypeField", "CustomField.NameTaken",
                "That asset type already has a field with this name.")
            .Register("UX_CustomFieldOption_Value", "CustomField.DuplicateOption",
                "Two dropdown options cannot have the same value.")
            .Register("UX_AssetCustomValue_AssetField", "CustomField.ValueAlreadySet",
                "That custom field already has a value on this asset.")

            // The register's own indexes. Their slices are not built yet, but
            // the guard below enumerates the LIVE schema, so leaving them out
            // would mean either a failing test or a 500 the first time somebody
            // types a duplicate asset number.
            .Register("UX_Asset_Number", "Asset.NumberTaken",
                "An asset with that number already exists.")
            .Register("UX_Asset_QrCode", "Asset.QrCodeTaken",
                "That QR code is already on another asset.")
            .Register("UX_Asset_SapNumber", "Asset.SapNumberTaken",
                "That SAP asset number is already on another asset.")
            .Register("UX_AssetVehicleDetail_Registration", "Vehicle.RegistrationTaken",
                "That registration number is already on another vehicle.")
            .Register("UX_AssetDepreciationEntry_AssetYear", "Depreciation.YearAlreadySynced",
                "That asset already has a depreciation row for this financial year.")

            // R3, and the reason design rule 6 works for stock: two concurrent
            // receipts of the same bulk line at the same place collide here
            // rather than both inserting, and the loser retries as an increment.
            .Register("UX_AssetHolding_AssetLocation", "Holding.AlreadyAtLocation",
                "That asset already has a holding at this branch.")
            .Register("UX_AssetHolding_AssetSite", "Holding.AlreadyAtSite",
                "That asset already has a holding at this customer site."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapAssetsModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/assets")
            .RequireAuthorization()
            // Shape checks run before the endpoint, so no endpoint has to
            // remember to ask (02 §6).
            .AddEndpointFilter<ValidationEndpointFilter>();

        // The register itself. Mapped first because its routes are the bare
        // group root and /{id}, and a later /{id:int} would otherwise be
        // shadowed by nothing — but reading them together is what matters.
        SearchAssetsEndpoint.Map(group);
        GetAssetDashboardEndpoint.Map(group);
        RegisterAssetEndpoint.Map(group);
        UpdateAssetEndpoint.Map(group);
        DeleteAssetEndpoint.Map(group);

        // The detail screen and its timeline.
        GetAssetEndpoint.Map(group);
        GetAssetTimelineEndpoint.Map(group);
        SaveAssetDetailsEndpoint.Map(group);
        SetAssetCustomValuesEndpoint.Map(group);
        ImportAssetsExcelEndpoint.Map(group);
        UpdateImportedAssetDetailsEndpoint.Map(group);
        ListAuditorLocationsEndpoint.Map(group);
        CreateAuditorLocationEndpoint.Map(group);

        // Asset types, and the custom fields that hang off them.
        SearchAssetTypesEndpoint.Map(group);
        CreateAssetTypeEndpoint.Map(group);
        UpdateAssetTypeEndpoint.Map(group);
        GetAssetTypeCustomFieldsEndpoint.Map(group);
        DefineCustomFieldEndpoint.Map(group);
        UpdateCustomFieldEndpoint.Map(group);

        // The finance taxonomy.
        SearchAssetClassesEndpoint.Map(group);
        CreateAssetClassEndpoint.Map(group);
        UpdateAssetClassEndpoint.Map(group);
        SearchChartOfAccountsEndpoint.Map(group);
        CreateChartOfAccountEndpoint.Map(group);
        UpdateChartOfAccountEndpoint.Map(group);

        // Statuses.
        SearchAssetStatusesEndpoint.Map(group);
        CreateAssetStatusEndpoint.Map(group);
        UpdateAssetStatusEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchAssetsQuery, SearchAssetsResponse>, SearchAssetsHandler>();
        services.AddScoped<IRequestHandler<GetAssetDashboardQuery, GetAssetDashboardResponse>, GetAssetDashboardHandler>();
        services.AddScoped<IRequestHandler<RegisterAssetCommand, RegisterAssetResponse>, RegisterAssetHandler>();
        services.AddScoped<IRequestHandler<UpdateAssetCommand, UpdateAssetResponse>, UpdateAssetHandler>();
        services.AddScoped<IRequestHandler<DeleteAssetCommand, DeleteAssetResponse>, DeleteAssetHandler>();
        services.AddScoped<IRequestHandler<GetAssetQuery, GetAssetResponse>, GetAssetHandler>();
        services.AddScoped<IRequestHandler<GetAssetTimelineQuery, GetAssetTimelineResponse>, GetAssetTimelineHandler>();
        services.AddScoped<IRequestHandler<SaveAssetDetailsCommand, SaveAssetDetailsResponse>, SaveAssetDetailsHandler>();
        services.AddScoped<IRequestHandler<SetAssetCustomValuesCommand, SetAssetCustomValuesResponse>, SetAssetCustomValuesHandler>();
        services.AddScoped<IRequestHandler<ImportAssetsExcelCommand, ImportAssetsExcelResponse>, ImportAssetsExcelHandler>();
        services.AddScoped<IRequestHandler<UpdateImportedAssetDetailsCommand, UpdateImportedAssetDetailsResponse>, UpdateImportedAssetDetailsHandler>();
        services.AddScoped<IRequestHandler<ListAuditorLocationsQuery, ListAuditorLocationsResponse>, ListAuditorLocationsHandler>();
        services.AddScoped<IRequestHandler<CreateAuditorLocationCommand, CreateAuditorLocationResponse>, CreateAuditorLocationHandler>();

        services.AddScoped<IRequestHandler<SearchAssetTypesQuery, SearchAssetTypesResponse>, SearchAssetTypesHandler>();
        services.AddScoped<IRequestHandler<CreateAssetTypeCommand, CreateAssetTypeResponse>, CreateAssetTypeHandler>();
        services.AddScoped<IRequestHandler<UpdateAssetTypeCommand, UpdateAssetTypeResponse>, UpdateAssetTypeHandler>();

        services.AddScoped<IRequestHandler<SearchAssetClassesQuery, SearchAssetClassesResponse>, SearchAssetClassesHandler>();
        services.AddScoped<IRequestHandler<CreateAssetClassCommand, CreateAssetClassResponse>, CreateAssetClassHandler>();
        services.AddScoped<IRequestHandler<UpdateAssetClassCommand, UpdateAssetClassResponse>, UpdateAssetClassHandler>();

        services.AddScoped<IRequestHandler<SearchChartOfAccountsQuery, SearchChartOfAccountsResponse>, SearchChartOfAccountsHandler>();
        services.AddScoped<IRequestHandler<CreateChartOfAccountCommand, CreateChartOfAccountResponse>, CreateChartOfAccountHandler>();
        services.AddScoped<IRequestHandler<UpdateChartOfAccountCommand, UpdateChartOfAccountResponse>, UpdateChartOfAccountHandler>();

        services.AddScoped<IRequestHandler<SearchAssetStatusesQuery, SearchAssetStatusesResponse>, SearchAssetStatusesHandler>();
        services.AddScoped<IRequestHandler<CreateAssetStatusCommand, CreateAssetStatusResponse>, CreateAssetStatusHandler>();
        services.AddScoped<IRequestHandler<UpdateAssetStatusCommand, UpdateAssetStatusResponse>, UpdateAssetStatusHandler>();

        services.AddScoped<IRequestHandler<GetAssetTypeCustomFieldsQuery, GetAssetTypeCustomFieldsResponse>, GetAssetTypeCustomFieldsHandler>();
        services.AddScoped<IRequestHandler<DefineCustomFieldCommand, DefineCustomFieldResponse>, DefineCustomFieldHandler>();
        services.AddScoped<IRequestHandler<UpdateCustomFieldCommand, UpdateCustomFieldResponse>, UpdateCustomFieldHandler>();
    }
}
