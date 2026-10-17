using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Mamtzetza.Tests;

public class MaintenanceRegressionTests
{
    [Theory]
    [InlineData(24, "Rust")]
    [InlineData(25, "C#")]
    [InlineData(34, "C#")]
    [InlineData(35, "Python")]
    [InlineData(44, "Python")]
    [InlineData(45, "C++")]
    [InlineData(54, "C++")]
    [InlineData(55, "LISP")]
    public void AgeBoundaries(int age, string expected) =>
        Assert.Equal(expected, FireflyTransformer.CalculateFavoriteCodingLanguage(age));

    [Fact]
    public async Task MinimumLuckyNumberDoesNotOverflow()
    {
        var result = await Create(false, _ => throw new Exception("API disabled")).TransformAsync(
            new() { SoldierId = "edge", Height = 180, Weight = 80, LuckyNumber = int.MinValue });
        Assert.Equal(228, result.GlowIntensity);
        Assert.Equal("Margaret Hamilton", result.FavoriteWoman);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApiFailurePreservesBaseGlowAndTimestamp(bool failPost)
    {
        var requests = 0;
        var transformer = Create(true, request =>
        {
            requests++;
            return failPost && request.Method == HttpMethod.Get
                ? new(HttpStatusCode.OK) { Content = new StringContent("{\"buffName\":\"test\",\"bonusGlow\":75}", System.Text.Encoding.UTF8, "application/json") }
                : new(HttpStatusCode.ServiceUnavailable);
        });
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = await transformer.TransformAsync(new() { SoldierId = "fallback", Height = 180, Weight = 80, LuckyNumber = 7 });
        Assert.Equal(failPost ? 2 : 1, requests);
        Assert.Equal(227, result.GlowIntensity);
        Assert.StartsWith("API Error Fallback:", result.ComedicBuff);
        Assert.InRange(result.ProcessedAtUnixMs, before, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task CallerCancellationPropagatesInsteadOfPublishingFallback()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var transformer = Create(true, _ => throw new OperationCanceledException(cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transformer.TransformAsync(new(), cancelled.Token));
    }

    private static FireflyTransformer Create(bool enabled, Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new Handler(respond)) { BaseAddress = new Uri("http://fixture/") },
            Options.Create(new ComponentOptions { EnableExternalApi = enabled }), NullLogger<FireflyTransformer>.Instance);

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(respond(request));
    }
}
