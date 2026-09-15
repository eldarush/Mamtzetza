using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmegaSolider.Messages;
using Prometheus;

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
        using var timer = MamtzetzaMetrics.ProcessingDurationSeconds.NewTimer();

        _logger.LogInformation("Transforming OmegaSolider: Id={SoldierId}, Name={Name}, Rank={Rank}, Age={Age}, Planet={OriginPlanet}",
            input.SoldierId, input.Name, input.Rank, input.Age, input.OriginPlanet);

        int bonusGlow = 0;
        string comedicBuff = "Unbuffed Normal Firefly (No API)";

        if (_options.EnableExternalApi)
        {
            _logger.LogInformation("External API enrichment enabled. Requesting mock endpoints for soldier {SoldierId}...", input.SoldierId);
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
                    Name = input.Name,
                    FavoriteFood = input.FavoriteFood,
                    Codename = input.Name,
                    FavoriteSnack = input.FavoriteFood
                };

                var postResponse = await _httpClient.PostAsJsonAsync("/api/v1/funny-title", titleRequest, cancellationToken);
                postResponse.EnsureSuccessStatusCode();

                var titleResponse = await postResponse.Content.ReadFromJsonAsync<ExternalTitleResponse>(cancellationToken: cancellationToken);

                var buffName = buffResponse?.BuffName ?? "Quantum Disco Sparkles";
                bonusGlow = buffResponse?.BonusGlow ?? 50;
                var title = titleResponse?.Title ?? "Supreme Commander of Crispy Snacks";

                comedicBuff = $"{buffName} - {title}";
                MamtzetzaMetrics.ExternalApiCallsTotal.WithLabels("success").Inc();

                _logger.LogInformation("Successfully enriched soldier {SoldierId}: Buff='{BuffName}', BonusGlow={BonusGlow}, Title='{Title}'",
                    input.SoldierId, buffName, bonusGlow, title);
            }
            catch (Exception ex)
            {
                MamtzetzaMetrics.ExternalApiCallsTotal.WithLabels("failure").Inc();
                _logger.LogError(ex, "External API call failed for soldier {SoldierId}. Using graceful fallback buff.", input.SoldierId);
                comedicBuff = $"API Error Fallback: {ex.Message}";
            }
        }

        int calculatedGlow = (int)(input.Height + (input.Weight * 0.5f)) + (Math.Abs(input.LuckyNumber) % 10) + bonusGlow;

        var firefly = new FireflyExpert
        {
            SoldierId = input.SoldierId,
            Name = input.Name,
            Rank = input.Rank,
            Age = input.Age,
            FavoriteFood = input.FavoriteFood,
            FavoriteTvShow = input.FavoriteTvShow,
            ShoeSize = input.ShoeSize,
            Height = input.Height,
            Weight = input.Weight,
            LuckyNumber = input.LuckyNumber,
            Hobby = input.Hobby,
            OriginPlanet = input.OriginPlanet,
            FavoriteTechnology = CalculateFavoriteTechnology(input.FavoriteTvShow, input.Hobby),
            FavoriteTeam = CalculateFavoriteTeam(input.OriginPlanet),
            FavoriteCommander = CalculateFavoriteCommander(input.Rank),
            FavoriteWoman = CalculateFavoriteWoman(input.LuckyNumber),
            FavoriteCodingLanguage = CalculateFavoriteCodingLanguage(input.Age),
            GlowIntensity = calculatedGlow,
            ComedicBuff = comedicBuff,
            ProcessedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        return firefly;
    }

    public static string CalculateFavoriteTechnology(string tvShow, string hobby)
    {
        var show = tvShow ?? string.Empty;
        if (show.Contains("Trek", StringComparison.OrdinalIgnoreCase) || show.Contains("Star Wars", StringComparison.OrdinalIgnoreCase))
            return "Antimatter Warp Core";
        if (show.Contains("Expanse", StringComparison.OrdinalIgnoreCase))
            return "Epstein Fusion Drive";
        if (show.Contains("Matrix", StringComparison.OrdinalIgnoreCase))
            return "Neural Direct Link";
        if (show.Contains("Doctor Who", StringComparison.OrdinalIgnoreCase) || show.Contains("TARDIS", StringComparison.OrdinalIgnoreCase))
            return "TARDIS Chrono-Engine";
        if (show.Contains("Cyberpunk", StringComparison.OrdinalIgnoreCase))
            return "Sandevistan Neural Implant";
        if (show.Contains("Firefly", StringComparison.OrdinalIgnoreCase))
            return "Serenity Gravity Rotor";

        return !string.IsNullOrWhiteSpace(hobby) ? $"Quantum {hobby} Disruptor" : "Quantum Tachyon Disruptor";
    }

    public static string CalculateFavoriteTeam(string originPlanet)
    {
        return (originPlanet?.Trim()?.ToLowerInvariant()) switch
        {
            "mars" => "Martian Dust Devils",
            "earth" => "Terran Cyber Knights",
            "jupiter" => "Great Red Spot Cyclones",
            "moon" => "Lunar Eclipse Titans",
            "venus" => "Venusian Storm Chasers",
            "saturn" => "Saturnian Ring Walkers",
            _ => $"{originPlanet} Galactic Starfighters"
        };
    }

    public static string CalculateFavoriteCommander(string rank)
    {
        var r = rank ?? string.Empty;
        if (r.Contains("General", StringComparison.OrdinalIgnoreCase) || r.Contains("Commander", StringComparison.OrdinalIgnoreCase))
            return "General Kenobi";
        if (r.Contains("Captain", StringComparison.OrdinalIgnoreCase))
            return "Captain Jean-Luc Picard";
        if (r.Contains("Sergeant", StringComparison.OrdinalIgnoreCase) || r.Contains("Major", StringComparison.OrdinalIgnoreCase))
            return "Sergeant Avery Johnson";
        if (r.Contains("Admiral", StringComparison.OrdinalIgnoreCase))
            return "Admiral William Adama";
        if (r.Contains("Colonel", StringComparison.OrdinalIgnoreCase))
            return "Colonel Jack O'Neill";

        return "Commander Shepard";
    }

    public static string CalculateFavoriteWoman(int luckyNumber)
    {
        return (Math.Abs(luckyNumber) % 5) switch
        {
            0 => "Ada Lovelace",
            1 => "Marie Curie",
            2 => "Grace Hopper",
            3 => "Margaret Hamilton",
            4 => "Hedy Lamarr",
            _ => "Ada Lovelace"
        };
    }

    public static string CalculateFavoriteCodingLanguage(int age)
    {
        if (age < 25) return "Rust";
        if (age < 35) return "C#";
        if (age < 45) return "Python";
        if (age < 55) return "C++";
        return "LISP";
    }
}
