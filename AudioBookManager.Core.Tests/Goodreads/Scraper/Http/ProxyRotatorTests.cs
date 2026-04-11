using System.Net;
using FluentAssertions;
using Goodreads.Scraper.Configuration;
using Goodreads.Scraper.Http;

namespace AudioBookManager.Core.Tests.Goodreads.Scraper.Http;

/// <summary>
/// Unit tests for ProxyRotator.
/// </summary>
public class ProxyRotatorTests
{
    [Fact]
    public void HasProxies_WithNoProxies_ReturnsFalse()
    {
        // Arrange
        var rotator = new ProxyRotator();

        // Assert
        rotator.HasProxies.Should().BeFalse();
    }

    [Fact]
    public void HasProxies_WithProxies_ReturnsTrue()
    {
        // Arrange
        var proxies = new List<ProxyEndpoint>
        {
            new() { Address = "http://proxy1:8080" }
        };
        var rotator = new ProxyRotator(proxies);

        // Assert
        rotator.HasProxies.Should().BeTrue();
    }

    [Fact]
    public void GetNextProxy_WithNoProxies_ReturnsNull()
    {
        // Arrange
        var rotator = new ProxyRotator();

        // Act
        var proxy = rotator.GetNextProxy();

        // Assert
        proxy.Should().BeNull();
    }

    [Fact]
    public void GetNextProxy_WithProxies_ReturnsWebProxy()
    {
        // Arrange
        var proxies = new List<ProxyEndpoint>
        {
            new() { Address = "http://proxy1.example.com:8080" }
        };
        var rotator = new ProxyRotator(proxies);

        // Act
        var proxy = rotator.GetNextProxy();

        // Assert
        proxy.Should().NotBeNull();
        proxy!.Address.Should().NotBeNull();
        proxy.Address!.Host.Should().Be("proxy1.example.com");
        proxy.Address.Port.Should().Be(8080);
    }

    [Fact]
    public void GetNextProxy_RotatesThroughAllProxies()
    {
        // Arrange
        var proxies = new List<ProxyEndpoint>
        {
            new() { Address = "http://proxy1:8080" },
            new() { Address = "http://proxy2:8080" },
            new() { Address = "http://proxy3:8080" }
        };
        var rotator = new ProxyRotator(proxies);

        // Act
        var usedAddresses = new HashSet<string>();
        for (int i = 0; i < 6; i++) // 2 full rotations
        {
            var proxy = rotator.GetNextProxy();
            usedAddresses.Add(proxy!.Address!.Host);
        }

        // Assert
        usedAddresses.Should().HaveCount(3);
        usedAddresses.Should().Contain("proxy1");
        usedAddresses.Should().Contain("proxy2");
        usedAddresses.Should().Contain("proxy3");
    }

    [Fact]
    public void GetNextProxy_WithCredentials_SetsCredentials()
    {
        // Arrange
        var proxies = new List<ProxyEndpoint>
        {
            new()
            {
                Address = "http://proxy1:8080",
                Username = "user",
                Password = "pass"
            }
        };
        var rotator = new ProxyRotator(proxies);

        // Act
        var proxy = rotator.GetNextProxy();

        // Assert
        proxy.Should().NotBeNull();
        proxy!.Credentials.Should().NotBeNull();
        var credentials = proxy.Credentials!.GetCredential(new Uri("http://proxy1:8080"), "Basic");
        credentials!.UserName.Should().Be("user");
        credentials.Password.Should().Be("pass");
    }

    [Fact]
    public void GetRandomProxy_WithNoProxies_ReturnsNull()
    {
        // Arrange
        var rotator = new ProxyRotator();

        // Act
        var proxy = rotator.GetRandomProxy();

        // Assert
        proxy.Should().BeNull();
    }

    [Fact]
    public void GetRandomProxy_WithProxies_ReturnsValidProxy()
    {
        // Arrange
        var proxies = new List<ProxyEndpoint>
        {
            new() { Address = "http://proxy1:8080" },
            new() { Address = "http://proxy2:8080" }
        };
        var rotator = new ProxyRotator(proxies);

        // Act
        var proxy = rotator.GetRandomProxy();

        // Assert
        proxy.Should().NotBeNull();
        proxy!.Address!.Host.Should().BeOneOf("proxy1", "proxy2");
    }

    [Fact]
    public void AddProxy_AddsNewProxy()
    {
        // Arrange
        var rotator = new ProxyRotator();
        rotator.HasProxies.Should().BeFalse();

        // Act
        rotator.AddProxy(new ProxyEndpoint { Address = "http://newproxy:8080" });

        // Assert
        rotator.HasProxies.Should().BeTrue();
        var proxy = rotator.GetNextProxy();
        proxy!.Address!.Host.Should().Be("newproxy");
    }

    [Fact]
    public void RemoveProxy_RemovesExistingProxy()
    {
        // Arrange
        var proxies = new List<ProxyEndpoint>
        {
            new() { Address = "http://proxy1:8080" },
            new() { Address = "http://proxy2:8080" }
        };
        var rotator = new ProxyRotator(proxies);

        // Act
        var removed = rotator.RemoveProxy("http://proxy1:8080");

        // Assert
        removed.Should().BeTrue();

        // Verify only proxy2 remains
        var usedAddresses = new HashSet<string>();
        for (int i = 0; i < 3; i++)
        {
            var proxy = rotator.GetNextProxy();
            usedAddresses.Add(proxy!.Address!.Host);
        }
        usedAddresses.Should().HaveCount(1);
        usedAddresses.Should().Contain("proxy2");
    }

    [Fact]
    public void RemoveProxy_WithNonExistentAddress_ReturnsFalse()
    {
        // Arrange
        var proxies = new List<ProxyEndpoint>
        {
            new() { Address = "http://proxy1:8080" }
        };
        var rotator = new ProxyRotator(proxies);

        // Act
        var removed = rotator.RemoveProxy("http://nonexistent:8080");

        // Assert
        removed.Should().BeFalse();
        rotator.HasProxies.Should().BeTrue();
    }

    [Fact]
    public void Operations_AreThreadSafe()
    {
        // Arrange
        var proxies = new List<ProxyEndpoint>
        {
            new() { Address = "http://proxy1:8080" },
            new() { Address = "http://proxy2:8080" },
            new() { Address = "http://proxy3:8080" }
        };
        var rotator = new ProxyRotator(proxies);
        var exceptions = new List<Exception>();
        var results = new System.Collections.Concurrent.ConcurrentBag<WebProxy?>();

        // Act
        Parallel.For(0, 100, _ =>
        {
            try
            {
                results.Add(rotator.GetNextProxy());
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(p => p.Should().NotBeNull());
    }
}
