using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Plugins;
namespace SimpleAdvertisement;

public class PluginConfig
{
  public bool Enabled { get; set; } = true;
  public float Interval { get; set; } = 60f;
  public bool ReloadOnMapChange { get; set; } = true;
}

public class Advertisement
{
  public string? Chat { get; set; }
  public string? CenterHtml { get; set; }
  public int? Duration { get; set; }
}

[PluginMetadata(
  Id = "SimpleAdvertisement", 
  Version = "1.1.1", 
  Name = "SimpleAdvertisement", 
  Author = "SyntX34", 
  Description = "A simple advertisements plugin for SwiftlyS2 CS2 servers."
)]
public partial class SimpleAdvertisement : BasePlugin
{
  private const string ConfigFile = "config.jsonc";
  private const string AdvertisementsFile = "advertisements.jsonc";
  private const int DefaultCenterHtmlDuration = 10000;
  private static readonly string DefaultAdvertisements =
    "{\n" +
    "  \"Rules\": {\n" +
    "    \"1\": {\n" +
    "      \"chat\": \"{green}Welcome to our server!{white} Check the rules and have fun.\"\n" +
    "    },\n" +
    "    \"2\": {\n" +
    "      \"centerhtml\": \"<font color='#FFD700'>Follow us on social media!</font>\"\n" +
    "    }\n" +
    "  }\n" +
    "}\n";

  private PluginConfig _config = new();
  private List<Advertisement> _rules = new();
  private int _currentIndex;
  private CancellationTokenSource? _timer;

  public SimpleAdvertisement(ISwiftlyCore core) : base(core) {}

  public override void ConfigureSharedInterface(IInterfaceManager interfaceManager) {}

  public override void UseSharedInterface(IInterfaceManager interfaceManager) {}

  public override void Load(bool hotReload)
  {
    _config = LoadConfig();
    Core.Event.OnMapLoad += OnMapLoad;
    StartAdvertisements();
  }

  public override void Unload()
  {
    Core.Event.OnMapLoad -= OnMapLoad;
    StopAdvertisements();
  }

  private void OnMapLoad(IOnMapLoadEvent @event)
  {
    if (_config.ReloadOnMapChange) StartAdvertisements();
  }

  [Command("reloadadvertisement", permission: "z", helpText: "Reloads the advertisements file.")]
  public void ReloadAdvertisementCommand(ICommandContext context)
  {
    StartAdvertisements();
    context.Reply(_rules.Count == 0 ? "No advertisements loaded." : "Advertisements reloaded.");
  }

  private PluginConfig LoadConfig()
  {
    try {
      Core.Configuration.InitializeJsonWithModel<PluginConfig>(ConfigFile, "config");
      var path = Core.Configuration.GetConfigPath(ConfigFile);
      var root = new ConfigurationBuilder().AddJsonFile(path, false, false).Build();
      return root.GetSection("config").Get<PluginConfig>() ?? new PluginConfig();
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to load config: {Error}", ex.Message);
      return new PluginConfig();
    }
  }

  private void StartAdvertisements()
  {
    StopAdvertisements();
    LoadAdvertisements();
    if (!_config.Enabled || _rules.Count == 0) return;
    var interval = Math.Max(1f, _config.Interval);
    _timer = Core.Scheduler.DelayAndRepeatBySeconds(interval, interval, ShowNext);
    if (_config.ReloadOnMapChange) Core.Scheduler.StopOnMapChange(_timer);
    ShowNext();
  }

  private void StopAdvertisements()
  {
    _timer?.Cancel();
    _timer = null;
    _currentIndex = 0;
  }

  private void LoadAdvertisements()
  {
    _rules.Clear();
    try {
      var path = Core.Configuration.GetConfigPath(AdvertisementsFile);
      if (!File.Exists(path))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, DefaultAdvertisements);
      }
      var root = new ConfigurationBuilder().AddJsonFile(path, false, false).Build();
      var rules = root.GetSection("Rules").Get<Dictionary<string, Advertisement>>();
      if (rules == null || rules.Count == 0)
      {
        Core.Logger.LogWarning("SimpleAdvertisement: no advertisement rules found in {Path}", path);
        return;
      }
      _rules = rules.OrderBy(kv => int.TryParse(kv.Key, out var key) ? key : int.MaxValue).ThenBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Value).ToList();
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to load advertisements: {Error}", ex.Message);
    }
  }

  private void ShowNext()
  {
    if (_rules.Count == 0) return;
    var rule = _rules[_currentIndex];
    _currentIndex = (_currentIndex + 1) % _rules.Count;
    if (!string.IsNullOrWhiteSpace(rule.Chat)) Core.PlayerManager.SendChat(Helper.Colored(rule.Chat));
    else if (!string.IsNullOrWhiteSpace(rule.CenterHtml)) Core.PlayerManager.SendCenterHTML(rule.CenterHtml, rule.Duration ?? DefaultCenterHtmlDuration);
  }
}
