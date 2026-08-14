using Microsoft.Data.SqlClient;

namespace AMS.Modules.Organization.Tests;

/// <summary>
/// docs/03 §7: "new filtered unique indexes must register a translation or the
/// build's architecture test fails."
/// </summary>
/// <remarks>
/// Identity learned this the hard way: CreateRole relied on an index producing
/// a 409, nobody registered the translation, and a duplicate name surfaced as a
/// raw DbUpdateException — a 500 carrying SQL Server's wording, to somebody who
/// simply typed a name that was taken. Organization has eight unique indexes,
/// so it gets the same guard from the start.
///
/// It reads the indexes from the live schema rather than a list in code,
/// because a list in code is one more thing to forget to add to.
/// </remarks>
[Collection(nameof(OrganizationCollectionDefinition))]
public sealed class SqlErrorRegistrationTests(OrganizationFixture fixture)
{
    [Fact]
    public async Task Every_unique_index_in_the_schema_has_a_registered_translation()
    {
        var indexes = new List<string>();

        await using (var connection = new SqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var command = new SqlCommand(
                """
                SELECT i.name
                FROM   sys.indexes i
                       JOIN sys.tables t  ON t.object_id = i.object_id
                       JOIN sys.schemas s ON s.schema_id = t.schema_id
                WHERE  s.name = 'Organization'
                       AND i.is_unique = 1
                       AND i.is_primary_key = 0
                       AND i.name IS NOT NULL;
                """,
                connection);

            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                indexes.Add(reader.GetString(0));
            }
        }

        indexes.ShouldNotBeEmpty("the schema must have unique indexes, or this test proves nothing");

        var registered = fixture.SqlErrors.RegisteredIndexes;
        var missing = indexes.Except(registered, StringComparer.OrdinalIgnoreCase).ToArray();

        missing.ShouldBeEmpty(
            "each of these enforces a rule a user can break, and without a registered "
            + "translation the user gets a 500 carrying SQL Server's wording instead of a "
            + "409 saying what they did (03 §7)");
    }

    [Fact]
    public void A_registered_index_translates_to_its_error()
    {
        var error = fixture.SqlErrors.Translate(
            SharedKernel.Persistence.SqlErrorTranslator.DuplicateKeyInIndex,
            "Cannot insert duplicate key row in object 'Organization.Region' with unique index 'UX_Region_Name'.");

        error.ShouldNotBeNull();
        error.Code.ShouldBe("Region.NameTaken");
    }

    [Fact]
    public void An_unrelated_sql_error_is_not_translated()
    {
        // Returning null is what lets the handler rethrow rather than dress a
        // genuine bug up as a conflict the user could have avoided.
        fixture.SqlErrors.Translate(547, "The INSERT statement conflicted with the FOREIGN KEY constraint.")
            .ShouldBeNull();
    }
}
