using FluentAssertions;
using Goodreads.Scraper.Configuration;

namespace AudioBookManager.Core.Tests.Goodreads.Scraper.Configuration;

/// <summary>
/// Unit tests for GoodreadsScraperSettings.
/// </summary>
public class GoodreadsScraperSettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new GoodreadsScraperSettings();

        // Assert
        settings.BaseUrl.Should().Be("https://www.goodreads.com");
        settings.RequestDelayMs.Should().Be(2000);
        settings.MaxRetries.Should().Be(3);
        settings.RetryBaseDelaySeconds.Should().Be(2);
        settings.TimeoutSeconds.Should().Be(30);
        settings.UseProxy.Should().BeFalse();
        settings.MaxSearchResults.Should().Be(10);
        settings.CustomUserAgents.Should().BeEmpty();
        settings.Proxy.Should().BeNull();
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        // Assert
        GoodreadsScraperSettings.SectionName.Should().Be("GoodreadsScraper");
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        // Arrange
        var settings = new GoodreadsScraperSettings
        {
            BaseUrl = "https://custom.goodreads.com",
            RequestDelayMs = 5000,
            MaxRetries = 5,
            RetryBaseDelaySeconds = 3,
            TimeoutSeconds = 60,
            UseProxy = true,
            MaxSearchResults = 20,
            CustomUserAgents = ["Agent1", "Agent2"],
            Proxy = new ProxySettings
            {
                Address = "http://proxy:8080"
            }
        };

        // Assert
        settings.BaseUrl.Should().Be("https://custom.goodreads.com");
        settings.RequestDelayMs.Should().Be(5000);
        settings.MaxRetries.Should().Be(5);
        settings.RetryBaseDelaySeconds.Should().Be(3);
        settings.TimeoutSeconds.Should().Be(60);
        settings.UseProxy.Should().BeTrue();
        settings.MaxSearchResults.Should().Be(20);
        settings.CustomUserAgents.Should().HaveCount(2);
        settings.Proxy.Should().NotBeNull();
    }
}

/// <summary>
/// Unit tests for ProxySettings.
/// </summary>
public class ProxySettingsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new ProxySettings();

        // Assert
        settings.Address.Should().BeNull();
        settings.Username.Should().BeNull();
        settings.Password.Should().BeNull();
        settings.BypassOnLocal.Should().BeTrue();
        settings.RotatingProxies.Should().BeEmpty();
    }

    [Fact]
    public void CreateWebProxy_WithNoAddress_ReturnsNull()
    {
        // Arrange
        var settings = new ProxySettings();

        // Act
        var proxy = settings.CreateWebProxy();

        // Assert
        proxy.Should().BeNull();
    }

    [Fact]
    public void CreateWebProxy_WithAddress_ReturnsWebProxy()
    {
        // Arrange
        var settings = new ProxySettings
        {
            Address = "http://proxy.example.com:8080"
        };

        // Act
        var proxy = settings.CreateWebProxy();

        // Assert
        proxy.Should().NotBeNull();
        proxy!.Address!.Host.Should().Be("proxy.example.com");
        proxy.Address.Port.Should().Be(8080);
    }

    [Fact]
    public void CreateWebProxy_WithCredentials_SetsCredentials()
    {
        // Arrange
        var settings = new ProxySettings
        {
            Address = "http://proxy.example.com:8080",
            Username = "testuser",
            Password = "testpass"
        };

        // Act
        var proxy = settings.CreateWebProxy();

        // Assert
        proxy.Should().NotBeNull();
        proxy!.Credentials.Should().NotBeNull();
        var creds = proxy.Credentials!.GetCredential(
            new Uri("http://proxy.example.com:8080"), "Basic");
        creds!.UserName.Should().Be("testuser");
        creds.Password.Should().Be("testpass");
    }

    [Fact]
    public void CreateWebProxy_RespectssBypassOnLocal()
    {
        // Arrange
        var settingsWithBypass = new ProxySettings
        {
            Address = "http://proxy:8080",
            BypassOnLocal = true
        };

        var settingsWithoutBypass = new ProxySettings
        {
            Address = "http://proxy:8080",
            BypassOnLocal = false
        };

        // Act
        var proxyWithBypass = settingsWithBypass.CreateWebProxy();
        var proxyWithoutBypass = settingsWithoutBypass.CreateWebProxy();

        // Assert
        proxyWithBypass!.BypassProxyOnLocal.Should().BeTrue();
        proxyWithoutBypass!.BypassProxyOnLocal.Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for ProxyEndpoint.
/// </summary>
public class ProxyEndpointTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var endpoint = new ProxyEndpoint();

        // Assert
        endpoint.Address.Should().Be(string.Empty);
        endpoint.Username.Should().BeNull();
        endpoint.Password.Should().BeNull();
    }

    [Fact]
    public void AllProperties_CanBeSet()
    {
        // Arrange
        var endpoint = new ProxyEndpoint
        {
            Address = "http://proxy:8080",
            Username = "user",
            Password = "pass"
        };

        // Assert
        endpoint.Address.Should().Be("http://proxy:8080");
        endpoint.Username.Should().Be("user");
        endpoint.Password.Should().Be("pass");
    }
}
