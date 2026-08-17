using AMS.Modules.Identity.PublicApi;
using AMS.Modules.Identity.PublicApi.Identity;
using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Features.AssignUserRoles;
using AMS.Modules.Identity.Features.ChangeMyPassword;
using AMS.Modules.Identity.Features.ConfirmMfaEnrolment;
using AMS.Modules.Identity.Features.CreateRole;
using AMS.Modules.Identity.Features.CreateAuditorAccount;
using AMS.Modules.Identity.Features.CreateUser;
using AMS.Modules.Identity.Features.EnrolMfa;
using AMS.Modules.Identity.Features.GetCapabilities;
using AMS.Modules.Identity.Features.GetMyProfile;
using AMS.Modules.Identity.Features.GetUser;
using AMS.Modules.Identity.Features.GetUserCapabilities;
using AMS.Modules.Identity.Features.LockUser;
using AMS.Modules.Identity.Features.ListAuditorAccounts;
using AMS.Modules.Identity.Features.RegenerateRecoveryCodes;
using AMS.Modules.Identity.Features.ResetUserPassword;
using AMS.Modules.Identity.Features.SearchRoles;
using AMS.Modules.Identity.Features.SearchUsers;
using AMS.Modules.Identity.Features.SetRoleCapabilities;
using AMS.Modules.Identity.Features.SetUserBranches;
using AMS.Modules.Identity.Features.SetUserCapabilityOverride;
using AMS.Modules.Identity.Features.SignIn;
using AMS.Modules.Identity.Features.UnlockUser;
using AMS.Modules.Identity.Features.UpdateRole;
using AMS.Modules.Identity.Features.UpdateUser;
using AMS.Modules.Identity.Features.VerifyMfaCode;
using AMS.Modules.Identity.Persistence;
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

namespace AMS.Modules.Identity;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
public static class IdentityModuleExtensions
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Not AddDbContext with a connection string: the context is built on the
        // REQUEST'S connection so one transaction can span modules (rule 4a).
        // A context that opened its own connection could not take part.
        services.AddModuleDbContext<IdentityDbContext>(IdentityDbContext.SchemaName);

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();
        services.AddScoped<IMfaChallengeTokens, MfaChallengeTokens>();
        services.AddScoped<ITotpCodes, TotpCodes>();

        // Shared by the sign-in slices and GetUserCapabilities, so the screen
        // that shows somebody's access and the token that grants it cannot
        // disagree.
        services.AddScoped<EffectiveAccess>();

        // Rule 3: what other modules may ask this one, and the only way

        // they may ask it.

        services.AddScoped<IUserDirectory, UserDirectory>();


        AddHandlers(services);

        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>(ServiceLifetime.Scoped);

        // Every unique index this module relies on registers the 409 it
        // produces. An unregistered index surfaces as a raw SQL Server message,
        // which is not something to show a branch administrator (docs/03 §7).
        // SqlErrorRegistrationTests reads the live schema and fails if one of
        // them is missing from this list.
        services.AddSingleton(new SqlErrorTranslator()
            .Register("UX_User_Username", "User.UsernameTaken",
                "That username is already in use.")
            .Register("UX_User_Employee", "User.EmployeeAlreadyLinked",
                "That employee already has a login.")
            .Register("UX_UserBranch_OnePrimary", "User.OnePrimaryBranch",
                "A user can have only one primary branch.")
            .Register("UX_Role_Name", "Role.NameTaken",
                "A role with that name already exists."));

        return services;
    }

    /// <summary>
    /// Contributes this module's routes. The host knows the group; the module
    /// knows its endpoints (01 §5).
    /// </summary>
    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/identity")
            .RequireAuthorization()
            // Shape checks run before the endpoint, so no endpoint has to
            // remember to ask (02 §6).
            .AddEndpointFilter<ValidationEndpointFilter>();

        // Sign In screen. Both are anonymous: there is nobody to authorise yet.
        SignInEndpoint.Map(group);
        VerifyMfaCodeEndpoint.Map(group);

        // My Profile screen. Authenticated, but no capability - a capability
        // here would let somebody be locked out of their own password change.
        GetMyProfileEndpoint.Map(group);
        ChangeMyPasswordEndpoint.Map(group);
        EnrolMfaEndpoint.Map(group);
        ConfirmMfaEnrolmentEndpoint.Map(group);
        RegenerateRecoveryCodesEndpoint.Map(group);

        // Users screen.
        SearchUsersEndpoint.Map(group);
        GetUserEndpoint.Map(group);
        CreateUserEndpoint.Map(group);
        CreateAuditorAccountEndpoint.Map(group);
        ListAuditorAccountsEndpoint.Map(group);
        UpdateUserEndpoint.Map(group);
        LockUserEndpoint.Map(group);
        UnlockUserEndpoint.Map(group);
        ResetUserPasswordEndpoint.Map(group);
        AssignUserRolesEndpoint.Map(group);
        SetUserBranchesEndpoint.Map(group);

        // Roles and Capabilities screen.
        SearchRolesEndpoint.Map(group);
        GetCapabilitiesEndpoint.Map(group);
        CreateRoleEndpoint.Map(group);
        UpdateRoleEndpoint.Map(group);
        SetRoleCapabilitiesEndpoint.Map(group);
        SetUserCapabilityOverrideEndpoint.Map(group);
        GetUserCapabilitiesEndpoint.Map(group);

        return endpoints;
    }

    private static void AddHandlers(IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<SignInCommand, SignInResponse>, SignInHandler>();
        services.AddScoped<IRequestHandler<VerifyMfaCodeCommand, VerifyMfaCodeResponse>, VerifyMfaCodeHandler>();

        services.AddScoped<IRequestHandler<GetMyProfileQuery, GetMyProfileResponse>, GetMyProfileHandler>();
        services.AddScoped<IRequestHandler<ChangeMyPasswordCommand, ChangeMyPasswordResponse>, ChangeMyPasswordHandler>();
        services.AddScoped<IRequestHandler<EnrolMfaCommand, EnrolMfaResponse>, EnrolMfaHandler>();
        services.AddScoped<IRequestHandler<ConfirmMfaEnrolmentCommand, ConfirmMfaEnrolmentResponse>, ConfirmMfaEnrolmentHandler>();
        services.AddScoped<IRequestHandler<RegenerateRecoveryCodesCommand, RegenerateRecoveryCodesResponse>, RegenerateRecoveryCodesHandler>();

        services.AddScoped<IRequestHandler<SearchUsersQuery, SearchUsersResponse>, SearchUsersHandler>();
        services.AddScoped<IRequestHandler<GetUserQuery, GetUserResponse>, GetUserHandler>();
        services.AddScoped<IRequestHandler<CreateUserCommand, CreateUserResponse>, CreateUserHandler>();
        services.AddScoped<IRequestHandler<CreateAuditorAccountCommand, CreateAuditorAccountResponse>, CreateAuditorAccountHandler>();
        services.AddScoped<IRequestHandler<ListAuditorAccountsQuery, ListAuditorAccountsResponse>, ListAuditorAccountsHandler>();
        services.AddScoped<IRequestHandler<UpdateUserCommand, UpdateUserResponse>, UpdateUserHandler>();
        services.AddScoped<IRequestHandler<LockUserCommand, LockUserResponse>, LockUserHandler>();
        services.AddScoped<IRequestHandler<UnlockUserCommand, UnlockUserResponse>, UnlockUserHandler>();
        services.AddScoped<IRequestHandler<ResetUserPasswordCommand, ResetUserPasswordResponse>, ResetUserPasswordHandler>();
        services.AddScoped<IRequestHandler<AssignUserRolesCommand, AssignUserRolesResponse>, AssignUserRolesHandler>();
        services.AddScoped<IRequestHandler<SetUserBranchesCommand, SetUserBranchesResponse>, SetUserBranchesHandler>();

        services.AddScoped<IRequestHandler<SearchRolesQuery, SearchRolesResponse>, SearchRolesHandler>();
        services.AddScoped<IRequestHandler<GetCapabilitiesQuery, GetCapabilitiesResponse>, GetCapabilitiesHandler>();
        services.AddScoped<IRequestHandler<CreateRoleCommand, CreateRoleResponse>, CreateRoleHandler>();
        services.AddScoped<IRequestHandler<UpdateRoleCommand, UpdateRoleResponse>, UpdateRoleHandler>();
        services.AddScoped<IRequestHandler<SetRoleCapabilitiesCommand, SetRoleCapabilitiesResponse>, SetRoleCapabilitiesHandler>();
        services.AddScoped<IRequestHandler<SetUserCapabilityOverrideCommand, SetUserCapabilityOverrideResponse>, SetUserCapabilityOverrideHandler>();
        services.AddScoped<IRequestHandler<GetUserCapabilitiesQuery, GetUserCapabilitiesResponse>, GetUserCapabilitiesHandler>();
    }
}
