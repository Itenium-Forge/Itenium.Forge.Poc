using System.Net;
using System.Net.Http.Json;

namespace FeatureFlags.Api.Tests;

[TestFixture]
public class FeatureFlagsTests
{
    private FeatureFlagsFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new FeatureFlagsFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Get_ReturnsHelloFromFeatureFlags()
    {
        var response = await _client.GetAsync("/");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("Hello from Feature Flags"));
    }

    [Test]
    public async Task Flags_ReturnsListOfFlags()
    {
        var flags = await _client.GetFromJsonAsync<Flag[]>("/api/flags");
        Assert.That(flags, Is.Not.Null);
        Assert.That(flags!, Has.Length.GreaterThan(0));
        Assert.That(flags!, Has.All.Matches<Flag>(f => f.Name != null));
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
    public async Task Flags_WithAllowedOrigin_ReturnsCorsHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/flags");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await _client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.True);
    }

    private record Flag(string Name, bool Enabled);
}
