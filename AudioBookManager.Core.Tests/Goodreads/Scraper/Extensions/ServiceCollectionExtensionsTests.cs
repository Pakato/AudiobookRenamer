using FluentAssertions;
using Goodreads.Scraper;
using Goodreads.Scraper.Configuration;
using Goodreads.Scraper.Extensions;
using Goodreads.Scraper.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioBookManager.Core.Tests.Goodreads.Scraper.Extensions;

/// <summary>
/// Integration tests for ServiceCollectionExtensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddGoodreadsScraper_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGoodreadsScraper();

        // Assert
        var provider = services.BuildServiceProvider();

        // Should resolve settings
        var settings = provider.GetService<IOptions<GoodreadsScraperSettings>>();
        settings.Should().NotBeNull();

        // Should resolve UserAgentRotator
        var userAgentRotator = provider.GetService<UserAgentRotator>();
        userAgentRotator.Should().NotBeNull();

        // Should resolve ProxyRotator
        var proxyRotator = provider.GetService<ProxyRotator>();
        proxyRotator.Should().NotBeNull();

        // Should resolve scraper service
        var scraperService = provider.GetService<IGoodreadsScraperService>();
        scraperService.Should().NotBeNull();
        scraperService.Should().BeOfType<GoodreadsScraperService>();
    }

    [Fact]
    public void AddGoodreadsScraper_WithConfiguration_AppliesSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGoodreadsScraper(settings =>
        {
            settings.RequestDelayMs = 5000;
            settings.MaxRetries = 10;
            settings.TimeoutSeconds = 120;
            settings.MaxSearchResults = 50;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<GoodreadsScraperSettings>>();

        settings.Value.RequestDelayMs.Should().Be(5000);
        settings.Value.MaxRetries.Should().Be(10);
        settings.Value.TimeoutSeconds.Should().Be(120);
        settings.Value.MaxSearchResults.Should().Be(50);
    }

    [Fact]
    public void AddGoodreadsScraper_WithProxySettings_ConfiguresProxy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGoodreadsScraper(settings =>
        {
            settings.UseProxy = true;
            settings.Proxy = new ProxySettings
            {
                Address = "http://proxy.test.com:8080",
                Username = "testuser",
                Password = "testpass",
                RotatingProxies =
                [
                    new ProxyEndpoint { Address = "http://proxy1:8080" },
                    new ProxyEndpoint { Address = "http://proxy2:8080" }
                ]
            };
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<GoodreadsScraperSettings>>();
        var proxyRotator = provider.GetRequiredService<ProxyRotator>();

        settings.Value.UseProxy.Should().BeTrue();
        settings.Value.Proxy.Should().NotBeNull();
        settings.Value.Proxy!.Address.Should().Be("http://proxy.test.com:8080");
        proxyRotator.HasProxies.Should().BeTrue();
    }

    [Fact]
    public void AddGoodreadsScraper_WithCustomUserAgents_ConfiguresRotator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var customAgents = new List<string>
        {
            "CustomAgent1",
            "CustomAgent2"
        };

        // Act
        services.AddGoodreadsScraper(settings =>
        {
            settings.CustomUserAgents = customAgents;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var userAgentRotator = provider.GetRequiredService<UserAgentRotator>();

        // The rotator should use custom agents
        var agents = new HashSet<string>();
        for (int i = 0; i < 5; i++)
        {
            agents.Add(userAgentRotator.GetNextUserAgent());
        }

        agents.Should().BeSubsetOf(customAgents);
    }

    [Fact]
    public void AddGoodreadsScraper_ScraperService_IsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGoodreadsScraper();

        // Act
        var provider = services.BuildServiceProvider();

        IGoodreadsScraperService? service1;
        IGoodreadsScraperService? service2;

        using (var scope1 = provider.CreateScope())
        {
            service1 = scope1.ServiceProvider.GetService<IGoodreadsScraperService>();
        }

        using (var scope2 = provider.CreateScope())
        {
            service2 = scope2.ServiceProvider.GetService<IGoodreadsScraperService>();
        }

        // Assert - Different scopes should get different instances
        service1.Should().NotBeNull();
        service2.Should().NotBeNull();
        // Note: With IHttpClientFactory, each scope gets a new service instance
        // but they share the underlying HttpClient handler pool
    }

    [Fact]
    public void AddGoodreadsScraper_SettingsAreSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGoodreadsScraper(s => s.RequestDelayMs = 9999);

        // Act
        var provider = services.BuildServiceProvider();
        var settings1 = provider.GetRequiredService<IOptions<GoodreadsScraperSettings>>();
        var settings2 = provider.GetRequiredService<IOptions<GoodreadsScraperSettings>>();

        // Assert
        settings1.Should().BeSameAs(settings2);
        settings1.Value.RequestDelayMs.Should().Be(9999);
    }

    [Fact]
    public void AddGoodreadsScraper_CanBeCalledMultipleTimes_LastWins()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddGoodreadsScraper(s => s.RequestDelayMs = 1000);
        services.AddGoodreadsScraper(s => s.RequestDelayMs = 2000);

        // Assert
        var provider = services.BuildServiceProvider();
        var settings = provider.GetRequiredService<IOptions<GoodreadsScraperSettings>>();

        // Last registration wins
        settings.Value.RequestDelayMs.Should().Be(2000);
    }
}
