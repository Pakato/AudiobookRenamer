using FluentAssertions;
using Goodreads.Scraper.Http;

namespace AudioBookManager.Core.Tests.Goodreads.Scraper.Http;

/// <summary>
/// Unit tests for UserAgentRotator.
/// </summary>
public class UserAgentRotatorTests
{
    [Fact]
    public void GetRandomUserAgent_ReturnsNonEmptyString()
    {
        // Arrange
        var rotator = new UserAgentRotator();

        // Act
        var userAgent = rotator.GetRandomUserAgent();

        // Assert
        userAgent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetRandomUserAgent_ReturnsValidBrowserUserAgent()
    {
        // Arrange
        var rotator = new UserAgentRotator();

        // Act
        var userAgent = rotator.GetRandomUserAgent();

        // Assert
        userAgent.Should().Contain("Mozilla/5.0");
    }

    [Fact]
    public void GetNextUserAgent_RotatesThroughAllAgents()
    {
        // Arrange
        var customAgents = new List<string>
        {
            "Agent1",
            "Agent2",
            "Agent3"
        };
        var rotator = new UserAgentRotator(customAgents);

        // Act
        var agents = new HashSet<string>();
        for (int i = 0; i < 10; i++)
        {
            agents.Add(rotator.GetNextUserAgent());
        }

        // Assert
        agents.Should().BeEquivalentTo(customAgents);
    }

    [Fact]
    public void GetNextUserAgent_WithCustomAgents_UsesCustomAgents()
    {
        // Arrange
        var customAgents = new List<string>
        {
            "CustomAgent1",
            "CustomAgent2"
        };
        var rotator = new UserAgentRotator(customAgents);

        // Act
        var agent1 = rotator.GetNextUserAgent();
        var agent2 = rotator.GetNextUserAgent();

        // Assert
        customAgents.Should().Contain(agent1);
        customAgents.Should().Contain(agent2);
    }

    [Fact]
    public void GetNextUserAgent_WithEmptyList_UsesDefaultAgents()
    {
        // Arrange
        var rotator = new UserAgentRotator(new List<string>());

        // Act
        var userAgent = rotator.GetNextUserAgent();

        // Assert
        userAgent.Should().Contain("Mozilla/5.0");
    }

    [Fact]
    public void GetBrowserHeaders_ReturnsRequiredHeaders()
    {
        // Arrange
        var userAgent = "Mozilla/5.0 Test Agent";

        // Act
        var headers = UserAgentRotator.GetBrowserHeaders(userAgent);

        // Assert
        headers.Should().ContainKey("User-Agent");
        headers.Should().ContainKey("Accept");
        headers.Should().ContainKey("Accept-Language");
        headers.Should().ContainKey("Accept-Encoding");
        headers["User-Agent"].Should().Be(userAgent);
    }

    [Fact]
    public void GetBrowserHeaders_ContainsSecurityHeaders()
    {
        // Arrange
        var userAgent = "Mozilla/5.0 Test Agent";

        // Act
        var headers = UserAgentRotator.GetBrowserHeaders(userAgent);

        // Assert
        headers.Should().ContainKey("Sec-Ch-Ua");
        headers.Should().ContainKey("Sec-Ch-Ua-Mobile");
        headers.Should().ContainKey("Sec-Ch-Ua-Platform");
        headers.Should().ContainKey("Sec-Fetch-Dest");
        headers.Should().ContainKey("Sec-Fetch-Mode");
        headers.Should().ContainKey("Sec-Fetch-Site");
    }

    [Fact]
    public void Constructor_IsThreadSafe()
    {
        // Arrange
        var rotator = new UserAgentRotator();
        var exceptions = new List<Exception>();
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act
        Parallel.For(0, 100, _ =>
        {
            try
            {
                results.Add(rotator.GetNextUserAgent());
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(ua => ua.Should().NotBeNullOrEmpty());
    }
}
