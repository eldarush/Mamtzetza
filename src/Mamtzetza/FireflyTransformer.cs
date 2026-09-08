using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmegaSolider.Messages;

namespace Mamtzetza;

public interface IFireflyTransformer
{
    Task<FireflyExpert> TransformAsync(OmegaSolider.Messages.OmegaSolider input, CancellationToken cancellationToken = default);
}

public class FireflyTransformer : IFireflyTransformer
{
    private readonly HttpClient _httpClient;
    private readonly ComponentOptions _options;
    private readonly ILogger<FireflyTransformer> _logger;

    public FireflyTransformer(
        HttpClient httpClient,
        IOptions<ComponentOptions> options,
        ILogger<FireflyTransformer> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FireflyExpert> TransformAsync(OmegaSolider.Messages.OmegaSolider input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Transforming OmegaSolider: Id={SoldierId}, Codename={Codename}", input.SoldierId, input.Codename);

        int baseGlow = (input.RankLevel * 10) + (input.BraveryPoints * 2);
        string comedicBuff = "Unbuffed Normal Firefly (No API)";

        if (_options.EnableExternalApi)
        {
            _logger.LogInformation("External API feature flag enabled. Calling external endpoints for soldier {SoldierId}...", input.SoldierId);
            try
            {
                // Endpoint 1: GET /api/v1/buff/{soldierId}
                var buffResponse = await _httpClient.GetFromJsonAsync<ExternalBuffResponse>(
                    $"/api/v1/buff/{Uri.EscapeDataString(input.SoldierId)}",
                    cancellationToken);

                // Endpoint 2: POST /api/v1/funny-title
                var titleRequest = new ExternalTitleRequest
                {
                    SoldierId = input.SoldierId,
                    Codename = input.Codename,
                    FavoriteSnack = input.FavoriteSnack
                };

                var postResponse = await _httpClient.PostAsJsonAsync("/api/v1/funny-title", titleRequest, cancellationToken);
                postResponse.EnsureSuccessStatusCode();

                var titleResponse = await postResponse.Content.ReadFromJsonAsync<ExternalTitleResponse>(cancellationToken: cancellationToken);

                var buffName = buffResponse?.BuffName ?? "Quantum Disco Sparkles";
                var bonusGlow = buffResponse?.BonusGlow ?? 50;
                var title = titleResponse?.Title ?? "Supreme Commander of Crispy Snacks";

                baseGlow += bonusGlow;
                comedicBuff = $"{buffName} - {title}";

                _logger.LogInformation("Successfully enhanced soldier {SoldierId}: Buff='{BuffName}', BonusGlow={BonusGlow}, Title='{Title}'",
                    input.SoldierId, buffName, bonusGlow, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call external API for soldier {SoldierId}. Falling back to degraded buff.", input.SoldierId);
                comedicBuff = $"API Error Fallback: {ex.Message}";
            }
        }

        var firefly = new FireflyExpert
        {
            SoldierId = input.SoldierId,
            Codename = input.Codename,
            RankLevel = input.RankLevel,
            GlowIntensity = baseGlow,
            Expertise = $"Expert in {input.FavoriteSnack} Logistics",
            ComedicBuff = comedicBuff,
            SecretMission = $"Operation Glow-{input.SoldierId}",
            ProcessedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        return firefly;
    }
}
