using System.Text;
using System.Text.Json;

namespace WarframeRelics;

public class WarframeRelicService
{
    public WarframeRelic? Load(string era, string id)
    {
        string assemblyFolderPath = Path.GetDirectoryName(GetType().Assembly.Location)!;
        string rootFolderPath = Path.GetFullPath(Path.Combine(assemblyFolderPath, "..", "..", "..", ".."));
        string dataFolderPath = Path.GetFullPath(Path.Combine(rootFolderPath, "WarframeData", "data", "relics", era));
        string dataFilePath = Path.Combine(dataFolderPath, id + ".json");

        if (File.Exists(dataFilePath))
        {
            string json = File.ReadAllText(dataFilePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<WarframeRelic>(json);
        }

        return null;
    }
}