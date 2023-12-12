using System.Text.RegularExpressions;

using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

using LVK.Extensions.Bootstrapping.Console;

using Microsoft.Extensions.Configuration;

namespace WarframeRelics;

public class MainEntrypoint : IMainEntrypoint
{
    private readonly HashSet<string> _frames = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "Ash",
        "Ash Prime",
        "Atlas",
        "Atlas Prime",
        "Banshee",
        "Banshee Prime",
        "Baruuk",
        "Baruuk Prime",
        "Caliban",
        "Chroma",
        "Chroma Prime",
        "Citrine",
        "Dagath",
        "Ember",
        "Ember Prime",
        "Equinox",
        "Equinox Prime",
        "Excalibur",
        "Excalibur Prime",
        "Excalibur Umbra",
        "Frost",
        "Frost Prime",
        "Gara",
        "Gara Prime",
        "Garuda",
        "Garuda Prime",
        "Gauss",
        "Grendel",
        "Grendel Prime",
        "Harrow",
        "Harrow Prime",
        "Hildryn",
        "Hildryn Prime",
        "Hydroid",
        "Hydroid Prime",
        "Inaros",
        "Inaros Prime",
        "Ivara",
        "Ivara Prime",
        "Khora",
        "Khora Prime",
        "Kullervo",
        "Lavos",
        "Limbo",
        "Limbo Prime",
        "Loki",
        "Loki Prime",
        "Mag",
        "Mag Prime",
        "Mesa",
        "Mesa Prime",
        "Mirage",
        "Mirage Prime",
        "Nekros",
        "Nekros Prime",
        "Nezha",
        "Nezha Prime",
        "Nidus",
        "Nidus Prime",
        "Nova",
        "Nova Prime",
        "Nyx",
        "Nyx Prime",
        "Oberon",
        "Oberon Prime",
        "Octavia",
        "Octavia Prime",
        "Protea",
        "Revenant",
        "Revenant Prime",
        "Rhino",
        "Rhino Prime",
        "Saryn",
        "Saryn Prime",
        "Sevagoth",
        "Styanax",
        "Titania",
        "Titania Prime",
        "Trinity",
        "Trinity Prime",
        "Valkyr",
        "Valkyr Prime",
        "Vauban",
        "Vauban Prime",
        "Volt",
        "Volt Prime",
        "Voruna",
        "Wisp",
        "Wisp Prime",
        "Wukong",
        "Wukong Prime",
        "Xaku",
        "Yareli",
        "Zephyr",
        "Zephyr Prime", };

    private readonly IConfiguration _configuration;
    private readonly SheetsService _sheetsService;
    private readonly SpreadsheetId _spreadsheetId;
    private readonly WriteLimiter _writeLimiter;
    private readonly WarframeRelicService _warframeRelicService;

    public MainEntrypoint(IConfiguration configuration, SheetsService sheetsService, SpreadsheetId spreadsheetId, WriteLimiter writeLimiter,
            WarframeRelicService warframeRelicService)
    {
        _configuration = configuration;
        _sheetsService = sheetsService;
        _spreadsheetId = spreadsheetId;
        _writeLimiter = writeLimiter;
        _warframeRelicService = warframeRelicService;
    }

    public async Task<int> RunAsync(CancellationToken stoppingToken)
    {
        await FixRelicLinks(stoppingToken);
        await AddRewardLinks(stoppingToken);
        await FixRewardLinks(stoppingToken);

        return 0;
    }

    private async Task FixRelicLinks(CancellationToken stoppingToken)
    {
        SpreadsheetsResource.ValuesResource.GetRequest request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId.Value, "Relics!A1:D");
        request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
        ValueRange? data = await request.ExecuteAsync(stoppingToken);

        for (var row = 7; row < data.Values.Count; row++)
        {
            var era = data.Values[row][0] as string;
            var relic = data.Values[row][1] as string;
            if (era == null || relic == null)
                continue;

            if (relic.StartsWith("=HYPERLINK"))
                continue;

            var formula = $"=HYPERLINK(\"https://warframe.fandom.com/wiki/{era}_{relic}\";\"{relic}\")";
            SpreadsheetsResource.ValuesResource.UpdateRequest? putRequest = _sheetsService.Spreadsheets.Values.Update(new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object>
                        {
                            formula,
                        },
                    },
                }, _spreadsheetId.Value, $"Relics!B{row + 1}");

            putRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            Console.WriteLine(formula);
            await _writeLimiter.Wait();
            await putRequest.ExecuteAsync(stoppingToken);
        }
    }

    private async Task AddRewardLinks(CancellationToken stoppingToken)
    {
        SpreadsheetsResource.ValuesResource.GetRequest request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId.Value, "Relics!A8:D");
        ValueRange? ids = await request.ExecuteAsync(stoppingToken);

        request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId.Value, "Relics!E8:J999");
        request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
        ValueRange? data = await request.ExecuteAsync(stoppingToken);

        for (var row = 0; row < data.Values.Count; row++)
        {
            if (data.Values[row].Count == 0)
            {
                var era = ids.Values[row][0] as string;
                var id = ids.Values[row][1] as string;
                if (era == null || id == null)
                    continue;

                WarframeRelic? relic = _warframeRelicService.Load(era, id);
                if (relic == null)
                    continue;

                var rewards = relic.Rewards.Intact.OrderByDescending(r => r.Chance).ThenBy(r => r.Name).ToList();

                SpreadsheetsResource.ValuesResource.UpdateRequest? putRequest = _sheetsService.Spreadsheets.Values.Update(new ValueRange
                    {
                        Values = new List<IList<object>>
                        {
                            rewards.Select(r => (object)ConvertRewardToHyperlink(r.Name)).ToList(),
                        },
                    }, _spreadsheetId.Value, $"Relics!E{8 + row}:J{8 + row}");

                putRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                await _writeLimiter.Wait();
                await putRequest.ExecuteAsync(stoppingToken);
            }
            else
            {
                for (var column = 0; column < data.Values[row].Count; column++)
                {
                    var reward = data.Values[row][column] as string;
                    if (reward == null)
                        continue;

                    if (reward.StartsWith("=HYPERLINK"))
                        continue;

                    string expression = ConvertRewardToHyperlink(reward);

                    SpreadsheetsResource.ValuesResource.UpdateRequest? putRequest = _sheetsService.Spreadsheets.Values.Update(new ValueRange
                        {
                            Values = new List<IList<object>>
                            {
                                new List<object>
                                {
                                    expression,
                                },
                            },
                        }, _spreadsheetId.Value, $"Relics!{(char)('E' + column)}{8 + row}");

                    putRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    await _writeLimiter.Wait();
                    await putRequest.ExecuteAsync(stoppingToken);
                }
            }
        }
    }

    private async Task FixRewardLinks(CancellationToken stoppingToken)
    {
        SpreadsheetsResource.ValuesResource.GetRequest request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId.Value, "Relics!A8:D");
        ValueRange? ids = await request.ExecuteAsync(stoppingToken);

        request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId.Value, "Relics!E8:J999");
        request.ValueRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.ValueRenderOptionEnum.FORMULA;
        ValueRange? data = await request.ExecuteAsync(stoppingToken);

        for (var row = 0; row < data.Values.Count; row++)
        {
            if (data.Values[row].Count == 0)
                continue;

            for (var column = 0; column < data.Values[row].Count; column++)
            {
                var reward = data.Values[row][column] as string;
                if (reward == null)
                    continue;

                if (!reward.StartsWith("=HYPERLINK"))
                    continue;

                Match ma = Regex.Match(reward, """=HYPERLINK\("(?<url>[^"]+)";"(?<name>[^"]+)"\)""");
                if (!ma.Success)
                    continue;

                string expression = ConvertRewardToHyperlink(ma.Groups["name"].Value);
                if (expression == reward)
                    continue;

                SpreadsheetsResource.ValuesResource.UpdateRequest? putRequest = _sheetsService.Spreadsheets.Values.Update(new ValueRange
                    {
                        Values = new List<IList<object>>
                        {
                            new List<object>
                            {
                                expression,
                            },
                        },
                    }, _spreadsheetId.Value, $"Relics!{(char)('E' + column)}{8 + row}");

                putRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                await _writeLimiter.Wait();
                await putRequest.ExecuteAsync(stoppingToken);
            }
        }
    }

    private string ConvertRewardToHyperlink(string name)
    {
        string slug = FixRewardUrlSlug(name);
        var url = $"https://warframe.fandom.com/wiki/{slug}";
        return $"=HYPERLINK(\"{url}\";\"{name}\")";
    }

    private string FixRewardUrlSlug(string name)
    {
        name = name.Replace(" ", "_");
        name = name.Replace("_Blueprint", "");
        name = name.Replace("_Neuroptics", "");
        name = name.Replace("_Chassis", "");
        name = name.Replace("_Systems", "");
        name = name.Replace("_Harness", "");
        name = name.Replace("_Wings", "");
        name = name.Replace("_Barrel", "");
        name = name.Replace("_Guard", "");
        name = name.Replace("_Stock", "");
        name = name.Replace("_Receiver", "");
        name = name.Replace("_Blade", "");
        name = name.Replace("_Upper_Limb", "");
        name = name.Replace("_Lower_Limb", "");
        name = name.Replace("_Pouch", "");
        name = name.Replace("_Hilt", "");
        name = name.Replace("_String", "");
        name = name.Replace("_Link", "");
        name = name.Replace("_Gauntlet", "");
        name = name.Replace("_Carapace", "");
        name = name.Replace("_Cerebrum", "");
        name = name.Replace("_Ornament", "");
        name = name.Replace("_Boot", "");
        name = name.Replace("_Handle", "");
        name = name.Replace("&", "%26");

        if (name.EndsWith("_Prime"))
        {
            name = name.Replace("_Prime", "");
            if (_frames.Contains(name))
                name = name + "/Prime";
            else
                name = name + "_Prime";
        }

        return name;
    }
}