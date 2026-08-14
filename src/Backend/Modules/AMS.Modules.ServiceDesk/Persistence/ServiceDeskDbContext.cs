using AMS.Modules.ServiceDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AMS.Modules.ServiceDesk.Persistence;

/// <summary>
/// The ServiceDesk module's context. Owns schema <c>[ServiceDesk]</c> and nothing
/// else (docs/01 §2 rule 1).
/// </summary>
public sealed class ServiceDeskDbContext(DbContextOptions<ServiceDeskDbContext> options) : DbContext(options)
{
    /// <summary>The schema this module owns, and its migrations-history schema.</summary>
    public const string SchemaName = "ServiceDesk";

    public DbSet<ApprovalNotificationLog> ApprovalNotificationLogs => Set<ApprovalNotificationLog>();

    public DbSet<ApprovalStageApproverRule> ApprovalStageApproverRules => Set<ApprovalStageApproverRule>();

    public DbSet<ApprovalWorkflowDefinition> ApprovalWorkflowDefinitions => Set<ApprovalWorkflowDefinition>();

    public DbSet<ApprovalWorkflowStage> ApprovalWorkflowStages => Set<ApprovalWorkflowStage>();

    public DbSet<NewServiceRequestDetail> NewServiceRequestDetails => Set<NewServiceRequestDetail>();

    public DbSet<NewServiceRequestItem> NewServiceRequestItems => Set<NewServiceRequestItem>();

    public DbSet<RequestApprovalDecision> RequestApprovalDecisions => Set<RequestApprovalDecision>();

    public DbSet<RequestApprovalInstance> RequestApprovalInstances => Set<RequestApprovalInstance>();

    public DbSet<RequestApprovalParticipant> RequestApprovalParticipants => Set<RequestApprovalParticipant>();

    public DbSet<RequestApprovalStep> RequestApprovalSteps => Set<RequestApprovalStep>();

    public DbSet<RequestAttachment> RequestAttachments => Set<RequestAttachment>();

    public DbSet<RequestCategory> RequestCategories => Set<RequestCategory>();

    public DbSet<RequestEmail> RequestEmails => Set<RequestEmail>();

    public DbSet<RequestHistory> RequestHistories => Set<RequestHistory>();

    public DbSet<RequestStatus> RequestStatuses => Set<RequestStatus>();

    public DbSet<RequestSubCategory> RequestSubCategories => Set<RequestSubCategory>();

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    public DbSet<ServiceTemplate> ServiceTemplates => Set<ServiceTemplate>();

    public DbSet<SupportTeam> SupportTeams => Set<SupportTeam>();

    public DbSet<SupportTeamMember> SupportTeamMembers => Set<SupportTeamMember>();

    /// <summary>
    /// Drops EF's automatic index-per-foreign-key convention.
    /// </summary>
    /// <remarks>
    /// The reviewed design decides its own indexes: it adds one where a
    /// query needs it (IX_UserRole_RoleId, IX_RoleCapability_CapabilityName)
    /// and leaves it out where nothing reads that way. Letting EF add one
    /// per foreign key produced 14 indexes the script never asked for -
    /// each of them a write cost on a table somebody measured.
    /// </remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();
    }

    // The parameter must be named modelBuilder: CA1725 requires an override
    // to keep the base member's parameter names, and warnings are errors.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.HasSequence<long>("RequestNumberSequence", SchemaName)
            .StartsAt(1)
            .IncrementsBy(1);

        // This assembly only. A configuration from another module would put
        // another schema's table under this context.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceDeskDbContext).Assembly);
    }
}
