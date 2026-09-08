using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OmegaSolider.Messages;
using Xunit;

namespace OmegaFireflyComponent.Tests;

public class FireflyTransformerTests
{
    [Fact]
    public async Task TransformAsync_WhenApiDisabled_ReturnsUnbuffedFireflyExpert()
    {
        // Arrange
        var options = Options.Create(new ComponentOptions
        {
            EnableExternalApi = false
        });

        var handler = new MockHttpMessageHandler((req) => throw new InvalidOperationException("API should not be called"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080/") };

        var transformer = new FireflyTransformer(httpClient, options, NullLogger<FireflyTransformer>.Instance);

        var input = new OmegaSolider.Messages.OmegaSolider
        {
            SoldierId = "SOL-001",
            Codename = "Sparky",
            RankLevel = 3,
            BraveryPoints = 15,
            FavoriteSnack = "Quantum Doritos"
        };

        // Act
        var result = await transformer.TransformAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SOL-001", result.SoldierId);
        Assert.Equal("Sparky", result.Codename);
        Assert.Equal(3, result.RankLevel);
        Assert.Equal(3 * 10 + 15 * 2, result.GlowIntensity); // 60
        Assert.Equal("Expert in Quantum Doritos Logistics", result.Expertise);
        Assert.Equal("Unbuffed Normal Firefly (No API)", result.ComedicBuff);
        Assert.Equal("Operation Glow-SOL-001", result.SecretMission);
        Assert.True(result.ProcessedAtUnixMs > 0);
    }

    [Fact]
    public async Task TransformAsync_WhenApiEnabled_AppliesExternalBuffsCorrectly()
    {
        // Arrange
        var options = Options.Create(new ComponentOptions
        {
            EnableExternalApi = true,
            ExternalApiBaseUrl = "http://localhost:8080"
        });

        var handler = new MockHttpMessageHandler((req) =>
        {
            if (req.Method == HttpMethod.Get && req.RequestUri?.AbsolutePath == "/api/v1/buff/SOL-002")
            {
                var responseJson = JsonSerializer.Serialize(new ExternalBuffResponse
                {
                    SoldierId = "SOL-002",
                    BuffName = "Supernova Glow",
                    BonusGlow = 75
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                };
            }

            if (req.Method == HttpMethod.Post && req.RequestUri?.AbsolutePath == "/api/v1/funny-title")
            {
                var responseJson = JsonSerializer.Serialize(new ExternalTitleResponse
                {
                    Title = "Grand Master of Spicy Nachos",
                    FunnyLore = "He who crunches through darkness."
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080/") };
        var transformer = new FireflyTransformer(httpClient, options, NullLogger<FireflyTransformer>.Instance);

        var input = new OmegaSolider.Messages.OmegaSolider
        {
            SoldierId = "SOL-002",
            Codename = "Blaze",
            RankLevel = 4,
            BraveryPoints = 20,
            FavoriteSnack = "Spicy Nachos"
        };

        // Act
        var result = await transformer.TransformAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SOL-002", result.SoldierId);
        Assert.Equal("Blaze", result.Codename);
        // Base glow = 4 * 10 + 20 * 2 = 80; Bonus glow = 75 => Total = 155
        Assert.Equal(155, result.GlowIntensity);
        Assert.Equal("Expert in Spicy Nachos Logistics", result.Expertise);
        Assert.Equal("Supernova Glow - Grand Master of Spicy Nachos", result.ComedicBuff);
        Assert.Equal("Operation Glow-SOL-002", result.SecretMission);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
