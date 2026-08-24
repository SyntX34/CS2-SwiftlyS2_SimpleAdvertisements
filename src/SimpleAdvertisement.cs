using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlaceholderAPI.Contract;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

namespace SimpleAdvertisement;

public class PluginConfig
{
  public bool Enabled { get; set; } = true;
  public float Interval { get; set; } = 60f;
  public bool ReloadOnMapChange { get; set; } = true;
  public string Order { get; set; } = "forward";
  public bool SkipDuplicate { get; set; } = true;
  public bool WelcomeEnabled { get; set; } = false;
  public float WelcomeDelay { get; set; } = 3f;
  public string WelcomeLocation { get; set; } = "chat";
  public string WelcomeMessage { get; set; } = "{green}Welcome {PLAYERNAME} to our server!";
  public string WelcomeCenterHtml { get; set; } = "<font color='#FFD700'>Welcome {PLAYERNAME} to our server!</font>";
  public int WelcomeHtmlDuration { get; set; } = 10000;
  public string? WelcomePermission { get; set; }
}

public class Advertisement
{
  public string? Chat { get; set; }
  public string? CenterHtml { get; set; }
  public Dictionary<string, string>? Messages { get; set; }
  public string? DisplayType { get; set; }
  public int? Duration { get; set; }
  public List<string>? Permissions { get; set; }
  public List<string>? TriggerAds { get; set; }
  public string PlayerFilter { get; set; } = "all";
  public string Phase { get; set; } = "any";
}

[PluginMetadata(
  Id = "SimpleAdvertisement",
  Version = "1.3.1",
  Name = "SimpleAdvertisement",
  Author = "SyntX34",
  Description = "A simple advertisements plugin for SwiftlyS2 CS2 servers."
)]
public partial class SimpleAdvertisement : BasePlugin
{
  private const string ConfigFile = "config.jsonc";
  private const string AdvertisementsFile = "advertisements.jsonc";
  private const int DefaultCenterHtmlDuration = 10000;
  private const string PlaceholderApiKey = "PlaceholderAPI.v1";
  private static readonly string DefaultAdvertisements =
    "{\n" +
    "  // Rules: key can be any unique string or number.\n" +
    "  \"Rules\": {\n" +
    "    \"1\": {\n" +
    "      \"chat\": \"{green}Welcome to our server!{white} Check the rules and have fun.\",\n" +
    "      // permissions: optional flag(s). Send only to players holding one of them (e.g. from addons/swiftly/configs/permissions.jsonc).\n" +
    "      // \"permissions\": \"vip\",\n" +
    "      // triggerad: optional command(s) players can run (e.g. !buyvip) to view this ad on demand (string or array).\n" +
    "      // \"triggerad\": [\"buyvip\", \"vip\"],\n" +
    "      // playerfilter: all | alive | dead | spectators | players (players = not spectating).\n" +
    "      // \"playerfilter\": \"all\",\n" +
    "      // phase: any | warmup | live (live = not warmup).\n" +
    "      // \"phase\": \"any\"\n" +
    "    },\n" +
    "    \"2\": {\n" +
    "      \"message\": {\n" +
    "        \"en\": \"<font color='#FFD700'>Follow us on social media!</font>\",\n" +
    "        \"pt-BR\": \"<font color='#FFD700'>Siga-nos nas redes sociais!</font>\"\n" +
    "      },\n" +
    "      \"displaytype\": \"centerhtml\",\n" +
    "      \"duration\": 10000\n" +
    "    }\n" +
    "  }\n" +
    "}\n";

  private static readonly Dictionary<string, string> ColorCodes = new(StringComparer.OrdinalIgnoreCase)
  {
    { "default", "\x01" },
    { "white", "\x01" },
    { "darkred", "\x02" },
    { "purple", "\x03" },
    { "green", "\x04" },
    { "lightyellow", "\x05" },
    { "lightgreen", "\x05" },
    { "lime", "\x06" },
    { "red", "\x07" },
    { "grey", "\x08" },
    { "gray", "\x08" },
    { "yellow", "\x09" },
    { "gold", "\x10" },
    { "silver", "\x0A" },
    { "blue", "\x0B" },
    { "darkblue", "\x0C" },
    { "bluegrey", "\x0A" },
    { "magenta", "\x0E" },
    { "lightred", "\x0F" },
    { "orange", "\x10" },
    { "olive", "\x06" },
  };

  private PluginConfig _config = new();
  private List<Advertisement> _rules = new();
  private int _currentIndex;
  private int _lastIndex = -1;
  private CancellationTokenSource? _timer;
  private IPlaceholderAPIv1? _placeholderApi;
  private readonly Dictionary<string, Guid> _triggerCommands = new(StringComparer.OrdinalIgnoreCase);
  private readonly List<CancellationTokenSource> _welcomeTimers = new();

  public SimpleAdvertisement(ISwiftlyCore core) : base(core) {}

  public override void ConfigureSharedInterface(IInterfaceManager interfaceManager) {}

  public override void UseSharedInterface(IInterfaceManager interfaceManager)
  {
    if (interfaceManager.TryGetSharedInterface<IPlaceholderAPIv1>(PlaceholderApiKey, out var api))
      _placeholderApi = api;
    else
      _placeholderApi = null;
  }

  public override void Load(bool hotReload)
  {
    _config = LoadConfig();
    Core.Event.OnMapLoad += OnMapLoad;
    Core.Event.OnClientPutInServer += OnClientPutInServer;
    StartAdvertisements();
  }

  public override void Unload()
  {
    Core.Event.OnMapLoad -= OnMapLoad;
    Core.Event.OnClientPutInServer -= OnClientPutInServer;
    CancelWelcomeTimers();
    UnregisterTriggerCommands();
    StopAdvertisements();
  }

  private void OnMapLoad(IOnMapLoadEvent @event)
  {
    if (_config.ReloadOnMapChange) StartAdvertisements();
  }

  private void OnClientPutInServer(IOnClientPutInServerEvent @event)
  {
    if (!_config.WelcomeEnabled) return;

    try {
      var playerId = @event.PlayerId;
      CancellationTokenSource cts = null!;
      cts = Core.Scheduler.DelayBySeconds(Math.Max(0f, _config.WelcomeDelay), () =>
      {
        _welcomeTimers.Remove(cts);
        SendWelcome(playerId);
      });
      _welcomeTimers.Add(cts);
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to schedule welcome message: {Error}", ex.Message);
    }
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
    if (IsOrder("reverse")) _currentIndex = _rules.Count - 1;
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
    _lastIndex = -1;
  }

  private void LoadAdvertisements()
  {
    _rules.Clear();
    UnregisterTriggerCommands();
    try {
      var path = Core.Configuration.GetConfigPath(AdvertisementsFile);
      if (!File.Exists(path))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, DefaultAdvertisements);
      }
      var root = new ConfigurationBuilder().AddJsonFile(path, false, false).Build();
      var rulesSection = root.GetSection("Rules");
      var children = rulesSection.GetChildren().ToList();
      if (children.Count == 0)
      {
        Core.Logger.LogWarning("SimpleAdvertisement: no advertisement rules found in {Path}", path);
        return;
      }

      var entries = new List<(string Key, Advertisement Rule)>();
      foreach (var section in children)
      {
        var rule = ParseRule(section);
        if (rule != null) entries.Add((section.Key, rule));
      }

      _rules = entries
        .OrderBy(e => int.TryParse(e.Key, out var key) ? key : int.MaxValue)
        .ThenBy(e => e.Key, StringComparer.Ordinal)
        .Select(e => e.Rule)
        .ToList();

      RegisterTriggerCommands();
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to load advertisements: {Error}", ex.Message);
    }
  }

  private static Advertisement? ParseRule(IConfigurationSection section)
  {
    var chat = section["chat"];
    var centerHtml = section["centerhtml"];
    var messages = ParseMessages(section.GetSection("message"));
    var displayType = NormalizeValue(section["displaytype"] ?? section["type"], null, ["chat", "centerhtml"]);

    if (string.IsNullOrWhiteSpace(chat) && string.IsNullOrWhiteSpace(centerHtml) && (messages == null || messages.Count == 0))
      return null;

    return new Advertisement
    {
      Chat = chat,
      CenterHtml = centerHtml,
      Messages = messages,
      DisplayType = displayType,
      Duration = int.TryParse(section["duration"], out var duration) ? duration : null,
      Permissions = ParseList(section.GetSection("permissions")),
      TriggerAds = ParseList(section.GetSection("triggerad")),
      PlayerFilter = NormalizeValue(section["playerfilter"], "all", ["all", "alive", "dead", "spectators", "players"]) ?? "all",
      Phase = NormalizeValue(section["phase"], "any", ["any", "warmup", "live"]) ?? "any",
    };
  }

  private static Dictionary<string, string>? ParseMessages(IConfigurationSection section)
  {
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var child in section.GetChildren())
    {
      if (!string.IsNullOrWhiteSpace(child.Value))
        dict[child.Key] = child.Value!;
    }
    return dict.Count == 0 ? null : dict;
  }

  private static List<string>? ParseList(IConfigurationSection section)
  {
    var list = new List<string>();
    var children = section.GetChildren().ToList();
    if (children.Count > 0)
    {
      foreach (var child in children)
        if (!string.IsNullOrWhiteSpace(child.Value)) list.Add(child.Value!.Trim());
    }
    else if (!string.IsNullOrWhiteSpace(section.Value))
    {
      foreach (var item in section.Value!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        list.Add(item);
    }
    return list.Count == 0 ? null : list;
  }

  private static string? NormalizeValue(string? value, string? fallback, string[] allowed)
  {
    if (!string.IsNullOrWhiteSpace(value))
    {
      var trimmed = value.Trim();
      if (allowed.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        return trimmed.ToLowerInvariant();
    }
    return fallback;
  }

  private void ShowNext()
  {
    if (_rules.Count == 0) return;
    var rule = _rules[PickIndex()];
    ShowRule(rule);
  }

  private void ShowRule(Advertisement rule)
  {
    if (!MeetsPhase(rule)) return;
    HashSet<ulong>? spectators = NeedsSpectatorCheck(rule) ? BuildSpectatorSet() : null;
    foreach (var player in Core.PlayerManager.GetAllPlayers())
    {
      if (!player.IsValid || player.IsFakeClient) continue;
      if (!MeetsPlayerConditions(rule, player, spectators)) continue;
      SendToPlayer(rule, player);
    }
  }

  private void SendToPlayer(Advertisement rule, IPlayer player)
  {
    try {
      var isCenter = string.Equals(rule.DisplayType, "centerhtml", StringComparison.OrdinalIgnoreCase) ||
                     (!string.IsNullOrWhiteSpace(rule.CenterHtml) && string.IsNullOrWhiteSpace(rule.Chat));

      string? rawMsg = null;
      if (rule.Messages is { Count: > 0 })
        rawMsg = ResolveLanguageMessage(rule.Messages, player);
      else if (!string.IsNullOrWhiteSpace(rule.Chat) && !isCenter)
        rawMsg = rule.Chat;
      else if (!string.IsNullOrWhiteSpace(rule.CenterHtml))
        rawMsg = rule.CenterHtml;
      else if (!string.IsNullOrWhiteSpace(rule.Chat))
        rawMsg = rule.Chat;

      if (string.IsNullOrWhiteSpace(rawMsg)) return;

      if (isCenter)
        player.SendCenterHTMLAsync(ProcessPlaceholders(player, rawMsg), rule.Duration ?? DefaultCenterHtmlDuration);
      else
        player.SendChatAsync(ProcessPlaceholders(player, ApplyColors(rawMsg)));
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to send advertisement: {Error}", ex.Message);
    }
  }

  private static string? ResolveLanguageMessage(Dictionary<string, string> messages, IPlayer player)
  {
    var lang = player.PlayerLanguage.ToString();
    if (!string.IsNullOrWhiteSpace(lang))
    {
      if (messages.TryGetValue(lang, out var exact)) return exact;
      var prefix = lang.Split('-')[0].Split('_')[0];
      var partial = messages.FirstOrDefault(kvp =>
      {
        var keyPrefix = kvp.Key.Split('-')[0].Split('_')[0];
        return string.Equals(keyPrefix, prefix, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(kvp.Key, lang, StringComparison.OrdinalIgnoreCase);
      });
      if (!string.IsNullOrEmpty(partial.Value)) return partial.Value;
    }
    if (messages.TryGetValue("en", out var en)) return en;
    return messages.Values.FirstOrDefault();
  }

  private bool MeetsPhase(Advertisement rule)
  {
    if (string.Equals(rule.Phase, "any", StringComparison.OrdinalIgnoreCase)) return true;
    var warmup = IsWarmup();
    return string.Equals(rule.Phase, "warmup", StringComparison.OrdinalIgnoreCase) ? warmup : !warmup;
  }

  private bool MeetsPlayerConditions(Advertisement rule, IPlayer player, HashSet<ulong>? spectators)
  {
    if (rule.Permissions is { Count: > 0 } && !HasAnyPermission(player, rule.Permissions)) return false;

    switch (rule.PlayerFilter)
    {
      case "alive": if (!player.IsAlive) return false; break;
      case "dead": if (player.IsAlive) return false; break;
      case "spectators": if (spectators == null || !spectators.Contains(player.SessionId)) return false; break;
      case "players": if (spectators != null && spectators.Contains(player.SessionId)) return false; break;
    }
    return true;
  }

  private static bool NeedsSpectatorCheck(Advertisement rule) =>
    rule.PlayerFilter is "spectators" or "players";

  private HashSet<ulong> BuildSpectatorSet()
  {
    try {
      return Core.PlayerManager.GetSpectators().Select(p => p.SessionId).ToHashSet();
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to read spectator list: {Error}", ex.Message);
      return new HashSet<ulong>();
    }
  }

  private bool HasAnyPermission(IPlayer player, List<string> permissions)
  {
    try {
      foreach (var permission in permissions)
        if (Core.Permission.PlayerHasPermission(player.SteamID, permission)) return true;
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to check permission: {Error}", ex.Message);
    }
    return false;
  }

  private bool IsWarmup()
  {
    try {
      var rules = Core.EntitySystem.GetGameRules();
      if (rules == null) return false;
      // WarmupPeriod directly reflects the game's warmup flag and works in every
      // game mode; GamePhaseEnum is checked as a fallback.
      return rules.WarmupPeriod || rules.GamePhaseEnum == GamePhase.GAMEPHASE_WARMUP_ROUND;
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to read game phase: {Error}", ex.Message);
      return false;
    }
  }

  private string ProcessPlaceholders(IPlayer? player, string message)
  {
    if (_placeholderApi == null || string.IsNullOrEmpty(message)) return message;
    try {
      return _placeholderApi.ProcessMessage(player, message);
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to process placeholders: {Error}", ex.Message);
      return message;
    }
  }

  private void SendWelcome(int playerId)
  {
    try {
      var player = Core.PlayerManager.GetPlayer(playerId);
      if (player == null || !player.IsValid || player.IsFakeClient) return;
      if (!string.IsNullOrWhiteSpace(_config.WelcomePermission) &&
          !Core.Permission.PlayerHasPermission(player.SteamID, _config.WelcomePermission)) return;

      if (IsWelcomeLocation("centerhtml"))
      {
        var html = !string.IsNullOrWhiteSpace(_config.WelcomeCenterHtml) ? _config.WelcomeCenterHtml : _config.WelcomeMessage;
        if (string.IsNullOrWhiteSpace(html)) return;
        player.SendCenterHTMLAsync(ProcessPlaceholders(player, html), Math.Max(1, _config.WelcomeHtmlDuration));
      }
      else
      {
        if (string.IsNullOrWhiteSpace(_config.WelcomeMessage)) return;
        player.SendChatAsync(ProcessPlaceholders(player, ApplyColors(_config.WelcomeMessage)));
      }
    } catch (Exception ex) {
      Core.Logger.LogWarning("SimpleAdvertisement: failed to send welcome message: {Error}", ex.Message);
    }
  }

  private bool IsWelcomeLocation(string location) =>
    string.Equals(_config.WelcomeLocation, location, StringComparison.OrdinalIgnoreCase);

  private void CancelWelcomeTimers()
  {
    foreach (var cts in _welcomeTimers) cts.Cancel();
    _welcomeTimers.Clear();
  }

  private void RegisterTriggerCommands()
  {
    foreach (var rule in _rules)
    {
      if (rule.TriggerAds == null || rule.TriggerAds.Count == 0) continue;
      foreach (var rawName in rule.TriggerAds)
      {
        if (string.IsNullOrWhiteSpace(rawName)) continue;
        var name = rawName.Trim();
        if (_triggerCommands.ContainsKey(name)) continue;
        try {
          var guid = Core.Command.RegisterCommand(name, context => TriggerAdCommand(name, context),
            registerRaw: true, permission: "", helpText: $"Shows the advertisement '{name}'.");
          _triggerCommands[name] = guid;
        } catch (Exception ex) {
          Core.Logger.LogWarning("SimpleAdvertisement: failed to register trigger command '{Name}': {Error}", name, ex.Message);
        }
      }
    }
  }

  private void UnregisterTriggerCommands()
  {
    foreach (var guid in _triggerCommands.Values) Core.Command.UnregisterCommand(guid);
    _triggerCommands.Clear();
  }

  private void TriggerAdCommand(string name, ICommandContext context)
  {
    if (!context.IsSentByPlayer || context.Sender == null) return;

    var rule = _rules.FirstOrDefault(r => r.TriggerAds != null && r.TriggerAds.Any(t => string.Equals(t, name, StringComparison.OrdinalIgnoreCase)));
    if (rule == null)
    {
      context.Reply("Unknown advertisement.");
      return;
    }
    if (!MeetsPhase(rule)) return;

    var spectators = NeedsSpectatorCheck(rule) ? BuildSpectatorSet() : null;
    if (!MeetsPlayerConditions(rule, context.Sender, spectators)) return;

    SendToPlayer(rule, context.Sender);
  }

  private int PickIndex()
  {
    if (IsOrder("random"))
    {
      var index = Random.Shared.Next(_rules.Count);
      if (_config.SkipDuplicate && _rules.Count > 1)
        while (index == _lastIndex)
          index = Random.Shared.Next(_rules.Count);
      _lastIndex = index;
      return index;
    }
    if (IsOrder("reverse"))
    {
      var index = _currentIndex;
      _currentIndex = (_currentIndex - 1 + _rules.Count) % _rules.Count;
      return index;
    }
    var forward = _currentIndex;
    _currentIndex = (_currentIndex + 1) % _rules.Count;
    return forward;
  }

  private bool IsOrder(string order) => string.Equals(_config.Order, order, StringComparison.OrdinalIgnoreCase);

  private static string ApplyColors(string message)
  {
    foreach (var code in ColorCodes)
    {
      message = message.Replace("{" + code.Key + "}", code.Value, StringComparison.OrdinalIgnoreCase);
      message = message.Replace("[" + code.Key + "]", code.Value, StringComparison.OrdinalIgnoreCase);
    }
    return message;
  }
}
