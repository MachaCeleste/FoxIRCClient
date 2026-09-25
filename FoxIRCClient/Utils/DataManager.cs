using FoxIRCClient.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace FoxIRCClient.Utils;

public class DataManager
{
    public static void SaveServerConfigs(List<ServerViewModel> servers)
    {
        string filePath = GetFilePath();

        if (!Directory.Exists(filePath))
            Directory.CreateDirectory(filePath);

        List<ServerConfig> configs = [];
        foreach (ServerViewModel server in servers)
        {
            var config = new ServerConfig(
                server.ServerAddress,
                server.Port,
                server.Nick,
                server.User,
                server.Real,
                server.Pass
            );

            configs.Add(config);
        }

        string json = JsonSerializer.Serialize(configs, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(Path.Combine(filePath, "ServerConfig.json"), json);
    }

    public static List<ServerViewModel>? LoadServerConfigs()
    {
        string filePath = Path.Combine(GetFilePath(), "ServerConfig.json");

        if (!File.Exists(filePath)) return null;

        string json = File.ReadAllText(filePath);
        List<ServerConfig>? configs = JsonSerializer.Deserialize<List<ServerConfig>>(json);

        if (configs == null) return null;

        List<ServerViewModel> servers = [];
        foreach (ServerConfig config in configs)
            servers.Add(new ServerViewModel(
                config.ServerAddress,
                config.Nick,
                config.User,
                config.Real,
                config.Pass,
                config.Port
                ));

        if (servers.Count == 0) return null;

        return servers;
    }

    private static string GetFilePath()
    {
        FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileInfo.CompanyName, fileInfo.ProductName);
    }
}

public record ServerConfig(
    string ServerAddress,
    int Port,
    string Nick,
    string User,
    string Real,
    string? Pass
    );