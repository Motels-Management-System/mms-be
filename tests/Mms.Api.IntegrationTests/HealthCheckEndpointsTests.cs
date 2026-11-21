using System.Net;
using FluentAssertions;

namespace Mms.Api.IntegrationTests;

public class HealthCheckEndpointsTests : IAsyncLifetime
{
    private readonly TestApi _api = new();

    public Task InitializeAsync() => _api.InitializeAsync();
    public Task DisposeAsync() => _api.DisposeAsync();

    [Fact]
    public async Task GetHealth_WhenDatabaseIsConnected_ReturnsOkHealthy()
    {
        // Act: Khách chưa đăng nhập cũng gọi được /health
        var response = await _api.GetAsync("/health", token: TestApi.NoAuth);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }
}
