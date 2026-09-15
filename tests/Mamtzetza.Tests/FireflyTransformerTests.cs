using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OmegaSolider.Messages;
using Mamtzetza;
using Xunit;

namespace Mamtzetza.Tests;

public class FireflyTransformerTests
{
    [Fact]
    public async Task TransformAsync_WhenApiDisabled_ComputesAllFieldsAndUnbuffedFirefly()
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
            Name = "John Doe",
            Rank = "Captain",
            Age = 32,
            FavoriteFood = "Shawarma",
            FavoriteTvShow = "Star Trek: TNG",
            ShoeSize = 43.5f,
            Height = 180.0f,
            Weight = 80.0f,
            LuckyNumber = 7, // 7 % 5 = 2 => Grace Hopper
            Hobby = "Chess",
            OriginPlanet = "Mars"
        };

        // Act
        var result = await transformer.TransformAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SOL-001", result.SoldierId);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("Captain", result.Rank);
        Assert.Equal(32, result.Age);
        Assert.Equal("Shawarma", result.FavoriteFood);
        Assert.Equal("Star Trek: TNG", result.FavoriteTvShow);
        Assert.Equal(43.5f, result.ShoeSize);
        Assert.Equal(180.0f, result.Height);
        Assert.Equal(80.0f, result.Weight);
        Assert.Equal(7, result.LuckyNumber);
        Assert.Equal("Chess", result.Hobby);
        Assert.Equal("Mars", result.OriginPlanet);

        // Deterministic fields:
        Assert.Equal("Antimatter Warp Core", result.FavoriteTechnology); // From Star Trek
        Assert.Equal("Martian Dust Devils", result.FavoriteTeam);        // From Mars
        Assert.Equal("Captain Jean-Luc Picard", result.FavoriteCommander); // From Captain
        Assert.Equal("Grace Hopper", result.FavoriteWoman);              // 7 % 5 = 2
        Assert.Equal("C#", result.FavoriteCodingLanguage);               // Age 32 => C#

        // Glow: (int)(180 + 80 * 0.5) + (7 % 10) + 0 = 220 + 7 = 227
        Assert.Equal(227, result.GlowIntensity);
        Assert.Equal("Unbuffed Normal Firefly (No API)", result.ComedicBuff);
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
            Name = "Blaze Fire",
            Rank = "General",
            Age = 22,
            FavoriteFood = "Spicy Nachos",
            FavoriteTvShow = "The Expanse",
            ShoeSize = 44.0f,
            Height = 190.0f,
            Weight = 90.0f,
            LuckyNumber = 10, // 10 % 5 = 0 => Ada Lovelace
            Hobby = "Gaming",
            OriginPlanet = "Earth"
        };

        // Act
        var result = await transformer.TransformAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("SOL-002", result.SoldierId);
        Assert.Equal("Epstein Fusion Drive", result.FavoriteTechnology);
        Assert.Equal("Terran Cyber Knights", result.FavoriteTeam);
        Assert.Equal("General Kenobi", result.FavoriteCommander);
        Assert.Equal("Ada Lovelace", result.FavoriteWoman);
        Assert.Equal("Rust", result.FavoriteCodingLanguage); // Age 22 => Rust

        // Base glow: (int)(190 + 90 * 0.5) + (10 % 10) = 235; Bonus glow: 75 => Total: 310
        Assert.Equal(310, result.GlowIntensity);
        Assert.Equal("Supernova Glow - Grand Master of Spicy Nachos", result.ComedicBuff);
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
