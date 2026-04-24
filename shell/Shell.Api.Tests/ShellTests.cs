using System.Net;
using System.Net.Http.Json;

namespace Shell.Api.Tests;

[TestFixture]
public class ShellTests
{
    private ShellFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ShellFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Get_ReturnsHelloWorld()
    {
        var response = await _client.GetAsync("/");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("Hello World"));
    }

    [Test]
    public async Task HealthLive_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/live");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task HealthReady_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/ready");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Apps_ReturnsConfiguredApps()
    {
        var apps = await _client.GetFromJsonAsync<AppEntry[]>("/apps");
        Assert.That(apps, Is.Not.Null);
        Assert.That(apps!, Has.Length.GreaterThan(0));
        Assert.That(apps!, Has.All.Matches<AppEntry>(a => a.Name != null && a.RemoteUrl != null));
        Assert.That(apps!, Has.Some.Matches<AppEntry>(a => string.Equals(a.Name, "featureFlags", StringComparison.Ordinal)));
        Assert.That(apps!, Has.All.Matches<AppEntry>(a => !a.RemoteUrl!.Contains("remoteEntry.js")));
    }

    private record AppEntry(string Name, string? RemoteUrl);

    [Test]
    public async Task ApiFlagsProxy_ReturnsFlags()
    {
        var result = await _client.GetFromJsonAsync<PagedResult<Flag>>("/api/flags");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items, Has.Count.GreaterThan(0));
        Assert.That(result.Items, Has.All.Matches<Flag>(f => f.Name != null));
        Assert.That(result.Page, Is.Not.Null);
        Assert.That(result.Page.TotalCount, Is.EqualTo(result.Items.Count));
    }

    [Test]
    public async Task Get_WithAllowedOrigin_ReturnsCorsHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await _client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.True);
    }

    private record PagedResult<T>(List<T> Items, PageInfo Page);
    private record PageInfo(int TotalCount);
    private record Flag(string Name, bool Enabled);
}
