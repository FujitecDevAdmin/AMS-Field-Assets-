using System.Reflection;
using System.Xml.Linq;

namespace AMS.ArchitectureTests;

/// <summary>
/// Where the rules find the things they assert about.
/// </summary>
/// <remarks>
/// Assemblies are discovered from the build output rather than listed in
/// code. A rule you have to remember to add a module to is a rule the
/// seventeenth module silently escapes.
/// </remarks>
internal static class Solution
{
    private const string ModulePrefix = "AMS.Modules.";

    /// <summary>
    /// What marks a project as a module's CONTRACT rather than the module.
    /// </summary>
    /// <remarks>
    /// A module may reference another module's contracts and may not reference
    /// the module. The two are told apart by this suffix, which is why it is a
    /// convention with a test behind it rather than a naming preference.
    ///
    /// ".PublicApi" and not ".Contracts": there is already a business module
    /// called Contracts, so that suffix could not tell AMS.Modules.Contracts
    /// from a contract assembly. The test below caught it on the first run.
    /// </remarks>
    public const string ContractsSuffix = ".PublicApi";

    /// <summary>Whether a project name names a contract assembly.</summary>
    public static bool IsContracts(string projectName) =>
        projectName.EndsWith(ContractsSuffix, StringComparison.Ordinal);

    /// <summary>Whether a project name names a module IMPLEMENTATION.</summary>
    public static bool IsModuleImplementation(string projectName) =>
        projectName.StartsWith(ModulePrefix, StringComparison.Ordinal)
        && !IsContracts(projectName);

    /// <summary>The repository root, found by walking up to the solution file.</summary>
    public static DirectoryInfo Root { get; } = FindRoot();

    public static IReadOnlyList<Assembly> ModuleAssemblies { get; } = LoadModules();

    /// <summary>Module name as the schema spells it: "Allocations", "ServiceDesk".</summary>
    public static string ModuleName(Assembly assembly) =>
        assembly.GetName().Name![ModulePrefix.Length..];

    /// <summary>Every <c>Features/&lt;Slice&gt;/</c> folder across every module.</summary>
    public static IEnumerable<DirectoryInfo> SliceFolders()
    {
        var modules = new DirectoryInfo(Path.Combine(Root.FullName, "src", "Backend", "Modules"));
        if (!modules.Exists)
        {
            yield break;
        }

        foreach (var features in modules.GetDirectories("Features", SearchOption.AllDirectories))
        {
            foreach (var slice in features.GetDirectories())
            {
                yield return slice;
            }
        }
    }

    /// <summary>Every production .csproj under src/Backend/, keyed by project name.</summary>
    /// <remarks>
    /// The rules read PROJECT FILES, not compiled metadata. An earlier version
    /// of these tests used <c>Assembly.GetReferencedAssemblies()</c> and passed
    /// happily while Allocations referenced Assets — the compiler omits a
    /// reference nothing in the IL actually uses, so the check was blind
    /// precisely while a module was still empty and most likely to be wired up
    /// wrongly. docs/01 §2 rule 2 says "no project reference"; this reads the
    /// project references.
    /// </remarks>
    public static IReadOnlyDictionary<string, FileInfo> SourceProjects { get; } = LoadProjects();

    /// <summary>The package names a .csproj references via PackageReference.</summary>
    public static IReadOnlyList<string> PackageReferencesOf(FileInfo csproj)
    {
        var doc = XDocument.Load(csproj.FullName);

        return doc.Descendants()
            .Where(e => e.Name.LocalName == "PackageReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();
    }

    /// <summary>The project names a .csproj references via ProjectReference.</summary>
    public static IReadOnlyList<string> ProjectReferencesOf(FileInfo csproj)
    {
        var doc = XDocument.Load(csproj.FullName);

        return doc.Descendants()
            .Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Path.GetFileNameWithoutExtension(v!.Replace('\\', Path.DirectorySeparatorChar)))
            .ToList();
    }

    private static Dictionary<string, FileInfo> LoadProjects()
    {
        var backend = new DirectoryInfo(Path.Combine(Root.FullName, "src", "Backend"));

        // The test projects live UNDER src/Backend/ as well, so an unfiltered
        // sweep would hand the boundary rules a pile of *.Tests projects and
        // assert against them. A test referencing three modules at once is
        // normal; a module doing it is the thing these rules exist to catch.
        var tests = Path.Combine(backend.FullName, "tests") + Path.DirectorySeparatorChar;

        return backend.GetFiles("*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.FullName.StartsWith(tests, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(f => Path.GetFileNameWithoutExtension(f.Name), f => f, StringComparer.Ordinal);
    }

    private static DirectoryInfo FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && dir.GetFiles("AMS.slnx").Length == 0)
        {
            dir = dir.Parent;
        }

        return dir ?? throw new InvalidOperationException(
            "Could not locate the repository root (no AMS.slnx found walking up from the test output).");
    }

    private static List<Assembly> LoadModules()
    {
        var here = new DirectoryInfo(AppContext.BaseDirectory);
        return here.GetFiles($"{ModulePrefix}*.dll")
            .Select(f => Assembly.LoadFrom(f.FullName))
            .OrderBy(a => a.GetName().Name, StringComparer.Ordinal)
            .ToList();
    }
}
