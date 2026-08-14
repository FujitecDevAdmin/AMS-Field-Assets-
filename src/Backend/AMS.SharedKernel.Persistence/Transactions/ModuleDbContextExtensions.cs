using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AMS.SharedKernel.Persistence.Transactions;

/// <summary>
/// Registers a module's <see cref="DbContext"/> the way rule 4a needs it.
/// </summary>
/// <remarks>
/// Every module calls this instead of <c>AddDbContext</c> with a connection
/// string. The difference is the whole point: a context built from a
/// connection string opens its own connection, and two connections cannot
/// share a transaction without a distributed coordinator. Built on the
/// request's shared connection, they can.
/// </remarks>
public static class ModuleDbContextExtensions
{
    /// <summary>
    /// Adds a module context on the request's shared connection, with the
    /// migrations history table in the module's own schema.
    /// </summary>
    /// <typeparam name="TContext">The module's context.</typeparam>
    /// <param name="services">The container.</param>
    /// <param name="schemaName">
    /// The module's schema. Its migrations history table lives there too, so
    /// fifteen modules can be migrated independently against one database.
    /// </param>
    public static IServiceCollection AddModuleDbContext<TContext>(
        this IServiceCollection services,
        string schemaName)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        services.AddDbContext<TContext>((provider, options) =>
        {
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();

            options.UseSqlServer(
                unitOfWork.Connection,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schemaName));

            options.AddInterceptors(new EnlistInUnitOfWorkInterceptor(unitOfWork));
        });

        return services;
    }
}
