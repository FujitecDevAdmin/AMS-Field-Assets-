using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AMS.Api.Tests;

/// <summary>
/// The wiring, over HTTP, on the real host.
/// </summary>
/// <remarks>
/// Every other test in this solution calls a handler directly, which means
/// none of them would notice if authentication were missing, the capability
/// policies were never registered, or the dispatcher never committed. These
/// are the tests that would.
/// </remarks>
[Collection(nameof(ApiCollectionDefinition))]
public sealed class CompositionRootTests(ApiFixture fixture)
{
    private const string Password = "correct horse battery";

    // ----------------------------------------------------------- it boots

    [Fact]
    public async Task The_host_starts_and_reports_itself_live()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync(
            new Uri("/health/live", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task It_reports_ready_when_it_can_reach_the_database()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync(
            new Uri("/health/ready", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // -------------------------------------------------------- authentication

    [Fact]
    public async Task A_protected_route_refuses_an_anonymous_caller()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync(
            new Uri("/api/v1/assets/types", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Signing_in_returns_a_token_that_works()
    {
        await ApiFixture.ResetIdentityAsync();
        await fixture.AddUserAsync("alice", Password, "asset.view");

        var token = await SignInAsync("alice", Password);
        token.ShouldNotBeNullOrWhiteSpace();

        var client = Authenticated(token);
        var response = await client.GetAsync(
            new Uri("/api/v1/assets/types", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_wrong_password_is_refused_and_issues_no_token()
    {
        await ApiFixture.ResetIdentityAsync();
        await fixture.AddUserAsync("bob", Password);

        var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/identity/sign-in", UriKind.Relative),
            new { Username = "bob", Password = "wrong" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_garbled_token_is_refused()
    {
        var client = Authenticated("not.a.token");

        var response = await client.GetAsync(
            new Uri("/api/v1/assets/types", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // --------------------------------------------------------- capabilities

    [Fact]
    public async Task A_capability_the_caller_lacks_is_a_403_and_not_a_401()
    {
        // 401 says "who are you", 403 says "not you". Confusing them sends the
        // client to the sign-in screen for a permission problem, and the user
        // signs in again and again to no effect.
        await ApiFixture.ResetIdentityAsync();
        await fixture.AddUserAsync("carol", Password, "asset.view");

        var client = Authenticated(await SignInAsync("carol", Password));

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/assets/types", UriKind.Relative),
            new { TypeName = "Laptops" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_capability_the_endpoint_declares_is_the_one_that_is_checked()
    {
        await ApiFixture.ResetIdentityAsync();
        await ApiFixture.ResetAssetsAsync();
        await fixture.AddUserAsync("dave", Password, "asset.view", "asset-taxonomy.manage");

        var client = Authenticated(await SignInAsync("dave", Password));

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/assets/types", UriKind.Relative),
            new { TypeName = $"Laptops {Guid.NewGuid():N}" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    // ------------------------------------------------------- the pipeline

    [Fact]
    public async Task A_validation_failure_is_a_400_from_the_pipeline()
    {
        // FluentValidation runs in the dispatcher, not in the handler. Nothing
        // else would catch it being unregistered.
        await ApiFixture.ResetIdentityAsync();
        await ApiFixture.ResetAssetsAsync();
        await fixture.AddUserAsync("erin", Password, "asset.view", "asset-taxonomy.manage");

        var client = Authenticated(await SignInAsync("erin", Password));

        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/assets/types", UriKind.Relative),
            new { TypeName = "" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_failed_command_leaves_nothing_behind()
    {
        // The register writes the asset and then its timeline line. This asks
        // for an asset type that does not exist, so the command fails - and the
        // proof that the transaction is real is that the register is still
        // empty afterwards.
        await ApiFixture.ResetIdentityAsync();
        await ApiFixture.ResetAssetsAsync();
        await fixture.AddUserAsync("frank", Password, "asset.view", "asset.manage");

        var client = Authenticated(await SignInAsync("frank", Password));

        var failed = await client.PostAsJsonAsync(
            new Uri("/api/v1/assets", UriKind.Relative),
            new
            {
                AssetNumber = "AST-0001",
                AssetName = "A laptop",
                AssetTypeId = 987654,
                AssetStatusId = 987654,
            },
            TestContext.Current.CancellationToken);

        failed.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var page = await client.GetFromJsonAsync<JsonElement>(
            new Uri("/api/v1/assets", UriKind.Relative), TestContext.Current.CancellationToken);
        page.GetProperty("totalCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task A_successful_command_commits_across_two_schemas()
    {
        // Registering an asset writes [Assets].[Asset] AND [Assets].[AssetEvent]
        // in one transaction the dispatcher owns. Reading the timeline back
        // proves the commit reached both.
        await ApiFixture.ResetIdentityAsync();
        await ApiFixture.ResetAssetsAsync();
        await fixture.AddUserAsync(
            "grace", Password, "asset.view", "asset.manage", "asset-taxonomy.manage");

        var client = Authenticated(await SignInAsync("grace", Password));

        var typeId = await CreateAsync(client, "/api/v1/assets/types", new { TypeName = "Laptops" });
        var statusId = await CreateAsync(
            client, "/api/v1/assets/statuses",
            new { StatusName = "In Stock", IsTerminal = false, DisplayOrder = 1 });

        var created = await client.PostAsJsonAsync(
            new Uri("/api/v1/assets", UriKind.Relative),
            new
            {
                AssetNumber = "AST-0001",
                AssetName = "A laptop",
                AssetTypeId = typeId,
                AssetStatusId = statusId,
            },
            TestContext.Current.CancellationToken);

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var assetId = (await created.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken)).GetProperty("id").GetInt32();

        var timeline = await client.GetFromJsonAsync<JsonElement>(
            new Uri($"/api/v1/assets/{assetId}/timeline", UriKind.Relative),
            TestContext.Current.CancellationToken);

        timeline.GetProperty("totalCount").GetInt32().ShouldBe(1);
        timeline.GetProperty("rows")[0].GetProperty("eventType").GetString().ShouldBe("Registered");
    }

    [Fact]
    public async Task A_refused_sign_in_still_counts_against_the_lockout()
    {
        // IPersistsOnFailure. SignIn increments the failed-attempt counter and
        // THEN refuses; if the dispatcher rolled that back with the refusal,
        // every wrong password would be free and lockout unreachable.
        await ApiFixture.ResetIdentityAsync();
        await fixture.AddUserAsync("heidi", Password);

        var client = fixture.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await client.PostAsJsonAsync(
                new Uri("/api/v1/identity/sign-in", UriKind.Relative),
                new { Username = "heidi", Password = "wrong" },
                TestContext.Current.CancellationToken);
        }

        // Locked now, so even the RIGHT password is refused.
        var correct = await client.PostAsJsonAsync(
            new Uri("/api/v1/identity/sign-in", UriKind.Relative),
            new { Username = "heidi", Password },
            TestContext.Current.CancellationToken);

        correct.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // -------------------------------------------------------------- helpers

    private async Task<string> SignInAsync(string username, string password)
    {
        var client = fixture.CreateClient();
        var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/identity/sign-in", UriKind.Relative),
            new { Username = username, Password = password },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken);
        return body.GetProperty("accessToken").GetString()!;
    }

    private HttpClient Authenticated(string token)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<int> CreateAsync(HttpClient client, string route, object body)
    {
        var response = await client.PostAsJsonAsync(
            new Uri(route, UriKind.Relative), body, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(
            TestContext.Current.CancellationToken)).GetProperty("id").GetInt32();
    }
}
