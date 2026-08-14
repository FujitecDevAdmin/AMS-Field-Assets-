using System.Reflection;

namespace AMS.ArchitectureTests;

/// <summary>
/// docs/01ARCHITECTURE.md §2 — the module rules, enforced.
/// </summary>
/// <remarks>
/// These are the rules a document alone cannot hold. Several developers and
/// several different AI coding tools write here; a boundary that is only
/// described gets crossed by whoever read the description least carefully.
/// </remarks>
public sealed class ModuleBoundaryTests
{
    [Fact]
    public void The_solution_has_the_fifteen_modules_the_design_names()
    {
        // R3 removed FieldAssets: a second asset register is the same mistake as
        // a second login table, one level up. Its rows fold into Assets and its
        // capabilities became a scoped view of the one register.
        var expected = new[]
        {
            "Allocations", "Assets", "Audit", "Contracts", "DataImport", "Discovery",
            "Identity", "Movements", "Notifications", "Organization",
            "SapSync", "ServiceDesk", "ServiceLevel", "Transfers", "Verification",
        };

        // Contract assemblies are not modules. AMS.Modules.Assets.Contracts
        // holds the shape other modules call; the module holds the behaviour.
        var actual = Solution.ModuleAssemblies
            .Select(Solution.ModuleName)
            .Where(name => !name.EndsWith(Solution.ContractsSuffix, StringComparison.Ordinal))
            .ToArray();

        // One schema per module, one module per schema. If this fails, either a
        // module was added without a schema or the module map in the design
        // script has moved. Both are decisions, not accidents.
        actual.ShouldBe(expected, ignoreOrder: true);
    }

    [Fact]
    public void No_module_references_another_modules_implementation()
    {
        // docs/01 §2 rule 2, and the test 01 §1 sets for the boundary:
        // "Deleting a module project must not break the build of any other module."
        //
        // Rule 3 says to depend on the other module's CONTRACT instead, and
        // that is now possible: contracts live in their own assembly, so this
        // permits AMS.Modules.Assets.Contracts and still refuses
        // AMS.Modules.Assets.
        var violations = new List<string>();

        foreach (var (name, csproj) in Solution.SourceProjects
                     .Where(p => Solution.IsModuleImplementation(p.Key)))
        {
            violations.AddRange(
                Solution.ProjectReferencesOf(csproj)
                    .Where(r => Solution.IsModuleImplementation(r) && r != name)
                    .Select(r => $"{name} -> {r}"));
        }

        violations.ShouldBeEmpty(
            "A module referenced another module's implementation. Reference its "
            + ".Contracts assembly instead (01 §2 rule 3); if you need its tables, you "
            + "want a write contract, not a reference.");
    }

    [Fact]
    public void A_contract_assembly_carries_no_implementation()
    {
        // The whole point of a contract assembly is that referencing it costs
        // the consumer nothing. One that grew an EF Core dependency would drag
        // it into every module that reads the contract, and quietly rebuild the
        // coupling the split exists to remove.
        var violations = new List<string>();

        foreach (var (name, csproj) in Solution.SourceProjects.Where(p => Solution.IsContracts(p.Key)))
        {
            violations.AddRange(
                Solution.PackageReferencesOf(csproj)
                    .Where(package =>
                        package.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                        || package.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                        || package.StartsWith("FluentValidation", StringComparison.Ordinal))
                    .Select(package => $"{name} -> {package}"));

            violations.AddRange(
                Solution.ProjectReferencesOf(csproj)
                    .Where(r => r != "AMS.SharedKernel")
                    .Select(r => $"{name} -> {r}"));
        }

        violations.ShouldBeEmpty(
            "A contract assembly took a dependency. It may reference AMS.SharedKernel "
            + "and nothing else: an interface and its DTOs, no EF, no ASP.NET, no "
            + "validation. Anything more belongs in the module that implements it.");
    }

    [Fact]
    public void No_module_references_the_Api_or_Infrastructure()
    {
        // Dependencies point inward. A module that knows about the host cannot
        // be tested without one.
        //
        // AMS.SharedKernel.Web is deliberately NOT on this list: every module's
        // *Endpoint.cs needs the ToHttpResult and RequireCapability helpers,
        // they cannot live in SharedKernel (which must reference nothing) and
        // they cannot live in Infrastructure (which modules may not reference).
        // See docs/00DESIGNDECISIONS.md.
        var forbidden = new[] { "AMS.Api", "AMS.Infrastructure", "AMS.Reporting" };
        var violations = new List<string>();

        foreach (var (name, csproj) in Solution.SourceProjects
                     .Where(p => p.Key.StartsWith("AMS.Modules.", StringComparison.Ordinal)))
        {
            violations.AddRange(
                Solution.ProjectReferencesOf(csproj)
                    .Where(r => forbidden.Contains(r, StringComparer.Ordinal))
                    .Select(r => $"{name} -> {r}"));
        }

        violations.ShouldBeEmpty("A module referenced the host or infrastructure. Dependencies point inward.");
    }

    [Fact]
    public void SharedKernel_depends_on_nothing()
    {
        // 01 §1: "Result<T>, Error, IDispatcher, base types, NO business logic".
        // The moment the kernel takes a dependency, every module inherits it.
        var csproj = Solution.SourceProjects["AMS.SharedKernel"];

        Solution.ProjectReferencesOf(csproj).ShouldBeEmpty(
            "SharedKernel must not reference any other project.");

        var packages = System.Xml.Linq.XDocument.Load(csproj.FullName)
            .Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .ToArray();

        packages.ShouldBeEmpty(
            "SharedKernel took a package dependency, so every module now has it too. "
            + "If the type needs EF Core or ASP.NET, it belongs in Infrastructure.");
    }

    [Fact]
    public void SharedKernel_does_not_know_about_EF_Core_or_AspNetCore()
    {
        var kernel = Assembly.Load("AMS.SharedKernel");

        var leaked = kernel.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                        || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .ToArray();

        leaked.ShouldBeEmpty(
            "SharedKernel picked up a persistence or web dependency. Move the type that needs it "
            + "into Infrastructure, or express it as an interface with no vendor types in its signature.");
    }

    [Fact]
    public void A_handler_never_injects_another_modules_DbContext()
    {
        // docs/02 §4. This is the specific mistake rule 4a exists to prevent:
        // reaching for a second DbContext instead of the owning module's
        // write contract. It compiles, it even works, and it quietly makes the
        // module un-deployable on its own.
        var violations = new List<string>();

        foreach (var module in Solution.ModuleAssemblies)
        {
            var moduleName = Solution.ModuleName(module);

            foreach (var handler in module.GetTypes().Where(t => t.Name.EndsWith("Handler", StringComparison.Ordinal)))
            {
                foreach (var ctor in handler.GetConstructors())
                {
                    violations.AddRange(
                        ctor.GetParameters()
                            .Select(p => p.ParameterType.Name)
                            .Where(n => n.EndsWith("DbContext", StringComparison.Ordinal)
                                     && !n.StartsWith(moduleName, StringComparison.Ordinal))
                            .Select(n => $"{handler.FullName} injects {n}"));
                }
            }
        }

        violations.ShouldBeEmpty(
            "A handler injected another module's DbContext. Use that module's PublicApi write "
            + "contract; UnitOfWorkBehavior already puts both inside one transaction (01 rule 4a).");
    }

    [Fact]
    public void Domain_does_not_depend_on_persistence()
    {
        // docs/01 §7. Domain rules must be testable without a database.
        var violations = new List<string>();

        foreach (var module in Solution.ModuleAssemblies)
        {
            var domainTypes = module.GetTypes()
                .Where(t => t.Namespace?.Contains(".Domain", StringComparison.Ordinal) == true);

            foreach (var type in domainTypes)
            {
                var touchesEf = type.GetConstructors()
                    .SelectMany(c => c.GetParameters())
                    .Select(p => p.ParameterType.Namespace)
                    .Concat(type.GetProperties().Select(p => p.PropertyType.Namespace))
                    .Any(ns => ns?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true);

                if (touchesEf)
                {
                    violations.Add(type.FullName!);
                }
            }
        }

        violations.ShouldBeEmpty("A Domain type referenced EF Core. Persistence concerns belong in Persistence/.");
    }
}
