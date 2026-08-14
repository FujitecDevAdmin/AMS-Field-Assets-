namespace AMS.ArchitectureTests;

/// <summary>
/// docs/01ARCHITECTURE.md §3 and §7 — the shape of a vertical slice.
/// </summary>
/// <remarks>
/// This one asserts against the FILE SYSTEM, not the compiled assembly,
/// because the rule is about layout: seven files, one type each, nothing
/// swept into a "Common" folder. That is not visible in IL.
/// </remarks>
public sealed class SliceLayoutTests
{
    /// <summary>The eight suffixes 01 §7 allows. A slice uses seven: Command xor Query.</summary>
    private static readonly string[] AllowedSuffixes =
    [
        "Command", "Query", "Request", "Response", "Validator", "Handler", "Mapper", "Endpoint",
    ];

    [Fact]
    public void Every_file_in_a_slice_uses_an_allowed_suffix()
    {
        var violations = new List<string>();

        foreach (var slice in Solution.SliceFolders())
        {
            foreach (var file in slice.GetFiles("*.cs"))
            {
                var stem = Path.GetFileNameWithoutExtension(file.Name);

                if (!AllowedSuffixes.Any(s => stem.EndsWith(s, StringComparison.Ordinal)))
                {
                    violations.Add($"{slice.Name}/{file.Name}");
                }
            }
        }

        violations.ShouldBeEmpty(
            "A slice contains a file that is none of the eight allowed kinds. Shared logic moves "
            + "DOWN into Domain/, never sideways into a helper beside the handler (01 §3).");
    }

    [Fact]
    public void Every_slice_is_a_command_or_a_query_but_not_both()
    {
        var violations = new List<string>();

        foreach (var slice in Solution.SliceFolders())
        {
            var names = slice.GetFiles("*.cs").Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToArray();

            var hasCommand = names.Any(n => n.EndsWith("Command", StringComparison.Ordinal));
            var hasQuery = names.Any(n => n.EndsWith("Query", StringComparison.Ordinal));

            if (hasCommand && hasQuery)
            {
                violations.Add($"{slice.Name}: both a Command and a Query");
            }
            else if (!hasCommand && !hasQuery && names.Length > 0)
            {
                violations.Add($"{slice.Name}: neither a Command nor a Query");
            }
        }

        violations.ShouldBeEmpty("01 §3: 'A slice is one or the other, never both.'");
    }

    [Fact]
    public void Every_slice_has_the_full_set_of_files()
    {
        // Not "most of them". A missing Validator is how a field added later
        // reaches the handler unchecked (02 §5).
        var required = new[] { "Request", "Response", "Validator", "Handler", "Mapper", "Endpoint" };
        var violations = new List<string>();

        foreach (var slice in Solution.SliceFolders())
        {
            var names = slice.GetFiles("*.cs").Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToArray();

            if (names.Length == 0)
            {
                continue;
            }

            var missing = required
                .Where(r => !names.Any(n => n.EndsWith(r, StringComparison.Ordinal)))
                .ToArray();

            if (missing.Length > 0)
            {
                violations.Add($"{slice.Name} is missing: {string.Join(", ", missing)}");
            }
        }

        violations.ShouldBeEmpty("Every slice carries its whole set of files (01 §3).");
    }

    [Fact]
    public void No_module_has_a_dumping_ground_folder()
    {
        // 02 §10's first checklist line, made mechanical.
        var banned = new[] { "Common", "Shared", "Helpers", "Utils", "Misc", "Core" };
        var modules = new DirectoryInfo(Path.Combine(Solution.Root.FullName, "src", "Backend", "Modules"));

        if (!modules.Exists)
        {
            return;
        }

        var violations = modules
            .GetDirectories("*", SearchOption.AllDirectories)
            .Where(d => banned.Contains(d.Name, StringComparer.OrdinalIgnoreCase))
            .Select(d => d.FullName.Replace(Solution.Root.FullName, string.Empty, StringComparison.Ordinal))
            .ToArray();

        violations.ShouldBeEmpty(
            "A module grew a general-purpose folder. Whatever went in there belongs in Domain/, "
            + "in the slice that uses it, or in SharedKernel if it is genuinely universal.");
    }
}
