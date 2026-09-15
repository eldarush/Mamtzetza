using System.Text.Json.Serialization;

namespace Mamtzetza;

public record ExternalBuffResponse
{
    [JsonPropertyName("soldierId")]
    public string SoldierId { get; init; } = string.Empty;

    [JsonPropertyName("buffName")]
    public string BuffName { get; init; } = "Quantum Disco Sparkles";

    [JsonPropertyName("bonusGlow")]
    public int BonusGlow { get; init; } = 50;
}

public record ExternalTitleRequest
{
    [JsonPropertyName("soldierId")]
    public string SoldierId { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("favoriteFood")]
    public string FavoriteFood { get; init; } = string.Empty;

    // Backward compatibility aliases
    [JsonPropertyName("codename")]
    public string Codename { get; init; } = string.Empty;

    [JsonPropertyName("favoriteSnack")]
    public string FavoriteSnack { get; init; } = string.Empty;
}

public record ExternalTitleResponse
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = "Supreme Commander of Crispy Snacks";

    [JsonPropertyName("funnyLore")]
    public string FunnyLore { get; init; } = "Fights crime with crunch.";
}
