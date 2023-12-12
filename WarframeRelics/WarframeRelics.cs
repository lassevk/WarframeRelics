using System.Diagnostics;
using System.Text.Json.Serialization;

namespace WarframeRelics;

public class WarframeRelic
{
    [JsonPropertyName("tier")]
    public string Era { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("rewards")]
    public WarframeRelicRewards Rewards { get; set; }
}

public class WarframeRelicRewards
{
    public WarframeRelicReward[] Intact { get; set; }
    public WarframeRelicReward[] Exceptional { get; set; }
    public WarframeRelicReward[] Flawless { get; set; }
    public WarframeRelicReward[] Radiant { get; set; }
}

[DebuggerDisplay("{Name}: {Chance} %")]
public class WarframeRelicReward
{
    [JsonPropertyName("_id")]
    public string Id { get; set; }

    [JsonPropertyName("itemName")]
    public string Name { get; set; }

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }

    [JsonPropertyName("chance")]
    public decimal Chance { get; set; }
}