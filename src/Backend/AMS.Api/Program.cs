using System.Text;
using AMS.Api;
using AMS.Infrastructure.Security;
using AMS.Infrastructure.Time;
using AMS.Modules.Allocations;
using AMS.Modules.Assets;
using AMS.Modules.Identity;
using AMS.Modules.Movements;
using AMS.Modules.Organization;
using AMS.Modules.ServiceDesk;
using AMS.Modules.Contracts;
using AMS.Modules.Discovery;
using AMS.Modules.Notifications;
using AMS.Modules.ServiceLevel;
using AMS.Modules.Verification;
using AMS.Modules.Transfers;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence.Transactions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Ams")
    ?? throw new InvalidOperationException(
        "Connection string 'Ams' is not configured. One database, fifteen schemas (01 §1).");

// ---------------------------------------------------------------- the kernel

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// One connection per request, shared by every module context, so a transaction
// can span modules without a distributed coordinator (rule 4a).
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IDispatcher, Dispatcher>();

// ------------------------------------------------------------ authentication

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    // On start, not on first sign-in. A deployment missing its signing key
    // should refuse to boot naming the setting, rather than accept requests
    // and then 401 everybody with no explanation.
    .ValidateOnStart();

builder.Services.AddSingleton<IAccessTokens, JwtAccessTokens>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"The '{JwtOptions.SectionName}' section is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),

            // No grace period. The default five minutes means a revoked or
            // expired session keeps working for five more, which is exactly the
            // window somebody would use.
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// Builds a policy for every capability: an endpoint asks for, on demand, so an
// endpoint's declaration IS its registration and the two cannot drift.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, CapabilityPolicyProvider>();

// ----------------------------------------------------------------- modules
// docs/02 §9: Program.cs is a list of these calls and nothing else.

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddOrganizationModule(builder.Configuration);
builder.Services.AddAssetsModule(builder.Configuration);
builder.Services.AddAllocationsModule(builder.Configuration);
builder.Services.AddMovementsModule(builder.Configuration);
builder.Services.AddTransfersModule(builder.Configuration);
builder.Services.AddServiceDeskModule(builder.Configuration);
builder.Services.AddServiceLevelModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);
builder.Services.AddContractsModule(builder.Configuration);
builder.Services.AddVerificationModule(builder.Configuration);
builder.Services.AddDiscoveryModule(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityModule();
app.MapOrganizationModule();
app.MapAssetsModule();
app.MapAllocationsModule();
app.MapMovementsModule();
app.MapTransfersModule();
app.MapServiceDeskModule();
app.MapServiceLevelModule();
app.MapNotificationsModule();
app.MapContractsModule();
app.MapVerificationModule();
app.MapDiscoveryModule();

app.MapHealthEndpoints();

await app.RunAsync();

/// <summary>Exposed so the integration tests can boot the real host.</summary>
public partial class Program;
