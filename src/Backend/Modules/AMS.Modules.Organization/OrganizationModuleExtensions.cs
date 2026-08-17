using AMS.Modules.Organization.PublicApi;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.Organization.Features.CreateApplication;
using AMS.Modules.Organization.Features.CreateDepartment;
using AMS.Modules.Organization.Features.CreateEmployee;
using AMS.Modules.Organization.Features.CreateBranch;
using AMS.Modules.Organization.Features.CreateRegion;
using AMS.Modules.Organization.Features.CreateVendor;
using AMS.Modules.Organization.Features.DeactivateEmployee;
using AMS.Modules.Organization.Features.GetEmployee;
using AMS.Modules.Organization.Features.GetEmployeeApplications;
using AMS.Modules.Organization.Features.GetMyApplicationAccess;
using AMS.Modules.Organization.Features.GrantApplicationAccess;
using AMS.Modules.Organization.Features.RevokeApplicationAccess;
using AMS.Modules.Organization.Features.SearchApplications;
using AMS.Modules.Organization.Features.SearchDepartments;
using AMS.Modules.Organization.Features.SearchEmployees;
using AMS.Modules.Organization.Features.SearchBranches;
using AMS.Modules.Organization.Features.SearchRegions;
using AMS.Modules.Organization.Features.SearchVendors;
using AMS.Modules.Organization.Features.UpdateApplication;
using AMS.Modules.Organization.Features.UpdateDepartment;
using AMS.Modules.Organization.Features.UpdateEmployee;
using AMS.Modules.Organization.Features.UpdateBranch;
using AMS.Modules.Organization.Features.UpdateRegion;
using AMS.Modules.Organization.Features.UpdateVendor;
using AMS.Modules.Organization.Persistence;
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

namespace AMS.Modules.Organization;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
public static class OrganizationModuleExtensions
{
    public static IServiceCollection AddOrganizationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Not AddDbContext with a connection string: the context is built on the
        // REQUEST'S connection so one transaction can span modules (rule 4a).
        // A context that opened its own connection could not take part.
        services.AddModuleDbContext<OrganizationDbContext>(OrganizationDbContext.SchemaName);

        // Rule 3: what other modules may ask this one, and the only way

        // they may ask it.

        services.AddScoped<IEmployeeDirectory, EmployeeDirectory>();
        services.AddScoped<IBranchDirectory, BranchDirectory>();
        services.AddScoped<IVendorDirectory, VendorDirectory>();


        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<CreateEmployeeValidator>(ServiceLifetime.Scoped);

        // Every unique index in this schema, with the 409 it produces.
        // SqlErrorRegistrationTests reads the live schema and fails if one is
        // missing from this list (docs/03 §7).
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_Region_Name", "Region.NameTaken",
                "A region with that name already exists.")
            .Register("UX_Branch_Code", "Branch.CodeTaken",
                "A branch with that code already exists.")
            .Register("UX_Branch_OneHeadOffice", "Branch.HeadOfficeExists",
                "Another branch is already the head office. Clear that one first.")
            .Register("UX_Department_Name", "Department.NameTaken",
                "A department with that name already exists.")
            .Register("UX_Vendor_Name", "Vendor.NameTaken",
                "A vendor with that name already exists.")
            .Register("UX_Employee_Code", "Employee.CodeTaken",
                "An employee with that code already exists.")
            .Register("UX_Application_Name", "Application.NameTaken",
                "An application with that name already exists.")
            .Register("UX_EmployeeApplication_OneActive", "ApplicationAccess.AlreadyGranted",
                "That employee already has access to this application."));

        return services;
    }

    /// <summary>Contributes this module's routes (01 §5).</summary>
    public static IEndpointRouteBuilder MapOrganizationModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/organization")
            .RequireAuthorization()
            // Shape checks run before the endpoint, so no endpoint has to
            // remember to ask (02 §6).
            .AddEndpointFilter<ValidationEndpointFilter>();

        // Regions, Branches, Departments, Vendors.
        SearchRegionsEndpoint.Map(group);
        CreateRegionEndpoint.Map(group);
        UpdateRegionEndpoint.Map(group);
        SearchBranchesEndpoint.Map(group);
        CreateBranchEndpoint.Map(group);
        UpdateBranchEndpoint.Map(group);
        SearchDepartmentsEndpoint.Map(group);
        CreateDepartmentEndpoint.Map(group);
        UpdateDepartmentEndpoint.Map(group);
        SearchVendorsEndpoint.Map(group);
        CreateVendorEndpoint.Map(group);
        UpdateVendorEndpoint.Map(group);

        // Employee Directory.
        SearchEmployeesEndpoint.Map(group);
        GetEmployeeEndpoint.Map(group);
        CreateEmployeeEndpoint.Map(group);
        UpdateEmployeeEndpoint.Map(group);
        DeactivateEmployeeEndpoint.Map(group);

        // Applications and Access. GetMyApplicationAccess is authenticated
        // only - every employee may read their own.
        SearchApplicationsEndpoint.Map(group);
        CreateApplicationEndpoint.Map(group);
        UpdateApplicationEndpoint.Map(group);
        GrantApplicationAccessEndpoint.Map(group);
        RevokeApplicationAccessEndpoint.Map(group);
        GetEmployeeApplicationsEndpoint.Map(group);
        GetMyApplicationAccessEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SearchRegionsQuery, SearchRegionsResponse>, SearchRegionsHandler>();
        services.AddScoped<IRequestHandler<CreateRegionCommand, CreateRegionResponse>, CreateRegionHandler>();
        services.AddScoped<IRequestHandler<UpdateRegionCommand, UpdateRegionResponse>, UpdateRegionHandler>();

        services.AddScoped<IRequestHandler<SearchBranchesQuery, SearchBranchesResponse>, SearchBranchesHandler>();
        services.AddScoped<IRequestHandler<CreateBranchCommand, CreateBranchResponse>, CreateBranchHandler>();
        services.AddScoped<IRequestHandler<UpdateBranchCommand, UpdateBranchResponse>, UpdateBranchHandler>();

        services.AddScoped<IRequestHandler<SearchDepartmentsQuery, SearchDepartmentsResponse>, SearchDepartmentsHandler>();
        services.AddScoped<IRequestHandler<CreateDepartmentCommand, CreateDepartmentResponse>, CreateDepartmentHandler>();
        services.AddScoped<IRequestHandler<UpdateDepartmentCommand, UpdateDepartmentResponse>, UpdateDepartmentHandler>();

        services.AddScoped<IRequestHandler<SearchVendorsQuery, SearchVendorsResponse>, SearchVendorsHandler>();
        services.AddScoped<IRequestHandler<CreateVendorCommand, CreateVendorResponse>, CreateVendorHandler>();
        services.AddScoped<IRequestHandler<UpdateVendorCommand, UpdateVendorResponse>, UpdateVendorHandler>();

        services.AddScoped<IRequestHandler<SearchEmployeesQuery, SearchEmployeesResponse>, SearchEmployeesHandler>();
        services.AddScoped<IRequestHandler<GetEmployeeQuery, GetEmployeeResponse>, GetEmployeeHandler>();
        services.AddScoped<IRequestHandler<CreateEmployeeCommand, CreateEmployeeResponse>, CreateEmployeeHandler>();
        services.AddScoped<IRequestHandler<UpdateEmployeeCommand, UpdateEmployeeResponse>, UpdateEmployeeHandler>();
        services.AddScoped<IRequestHandler<DeactivateEmployeeCommand, DeactivateEmployeeResponse>, DeactivateEmployeeHandler>();

        services.AddScoped<IRequestHandler<SearchApplicationsQuery, SearchApplicationsResponse>, SearchApplicationsHandler>();
        services.AddScoped<IRequestHandler<CreateApplicationCommand, CreateApplicationResponse>, CreateApplicationHandler>();
        services.AddScoped<IRequestHandler<UpdateApplicationCommand, UpdateApplicationResponse>, UpdateApplicationHandler>();
        services.AddScoped<IRequestHandler<GrantApplicationAccessCommand, GrantApplicationAccessResponse>, GrantApplicationAccessHandler>();
        services.AddScoped<IRequestHandler<RevokeApplicationAccessCommand, RevokeApplicationAccessResponse>, RevokeApplicationAccessHandler>();
        services.AddScoped<IRequestHandler<GetEmployeeApplicationsQuery, GetEmployeeApplicationsResponse>, GetEmployeeApplicationsHandler>();
        services.AddScoped<IRequestHandler<GetMyApplicationAccessQuery, GetMyApplicationAccessResponse>, GetMyApplicationAccessHandler>();
    }
}
