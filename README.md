<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" />
  <h2><strong><a href="https://github.com/SyntX34/CS2-SwiftlyS2_SimpleAdvertisements">SimpleAdvertisement</a></strong></h2>
  <h3>A simple advertisements plugin for SwiftlyS2 CS2 servers.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build Status">
  <img src="https://img.shields.io/github/downloads/SyntX34/CS2-SwiftlyS2_SimpleAdvertisements/total?label=downloads" alt="Downloads">
  <img src="https://img.shields.io/github/stars/SyntX34/CS2-SwiftlyS2_SimpleAdvertisements?style=flat&label=stars" alt="Stars">
  <img src="https://img.shields.io/github/license/SyntX34/CS2-SwiftlyS2_SimpleAdvertisements?label=license" alt="License">
</p>

## Features

- Colored chat advertisements
- Center HTML advertisements
- **Multi-language support** - send localized messages per player based on their client language (`"message": { "en": "...", "pt-BR": "..." }`) with configurable `displaytype` (`"chat"` or `"centerhtml"`)
- Configurable advertisement interval
- Optional advertisement reload on map change
- Admin command to reload advertisements on demand
- **PlaceholderAPI support** - resolve placeholders like `{PLAYERNAME}`, `{MAPNAME}`, `{PLAYERCOUNT}`, etc. per player (requires the [PlaceholderAPI](https://github.com/SwiftlyS2-Plugins/PlaceholderAPI) plugin)
- **Permission-targeted ads** - send an ad only to players holding a specific flag (e.g. `vip`, `premium`)
- **Welcome messages** - send a personalized message to every joining player, with delay and location options
- **Trigger commands** - players can view an announcement on demand (e.g. `!buyvip`)
- **Conditions** - send ads only to dead players, only to spectators, or only during map warmup

## Installation

1. Download the latest release.
2. Extract the contents into the `addons/swiftly/` directory of your server.
3. Start the server. The plugin will create `config.jsonc` and `advertisements.jsonc` automatically.

> **PlaceholderAPI (optional):** to use placeholders, install the [PlaceholderAPI plugin](https://github.com/SwiftlyS2-Plugins/PlaceholderAPI/releases) as well. Without it the plugin works normally and placeholders are left as-is.

## Configuration

The plugin configuration is stored at `addons/swiftly/configs/plugins/SimpleAdvertisement/config.jsonc`:

```jsonc
{
  "config": {
    "enabled": true,
    "interval": 60,
    "reloadOnMapChange": true,
    "order": "forward",
    "skipDuplicate": true,
    "welcomeEnabled": false,
    "welcomeDelay": 3,
    "welcomeLocation": "chat",
    "welcomeMessage": "{green}Welcome {PLAYERNAME} to our server!",
    "welcomeCenterHtml": "<font color='#FFD700'>Welcome {PLAYERNAME} to our server!</font>",
    "welcomeHtmlDuration": 10000,
    "welcomePermission": ""
  }
}
```

- `enabled` - enables or disables the plugin.
- `interval` - seconds between each advertisement.
- `reloadOnMapChange` - reloads `advertisements.jsonc` on every map change.
- `order` - advertisement selection order. `"forward"` cycles from the first entry to the last, `"reverse"` cycles from the last to the first, and `"random"` picks a random entry each time.
- `skipDuplicate` - only applies when `order` is `"random"`. When `true`, the same advertisement is never shown twice in a row. When `false`, repeats are allowed.
- `welcomeEnabled` - enables or disables the welcome message feature.
- `welcomeDelay` - delay in seconds before the welcome message is sent after a player connects.
- `welcomeLocation` - where the welcome message is displayed: `"chat"` or `"centerhtml"`.
- `welcomeMessage` - the chat message sent when `welcomeLocation` is `"chat"`. Supports colors and placeholders.
- `welcomeCenterHtml` - the HTML message sent when `welcomeLocation` is `"centerhtml"`. Supports placeholders.
- `welcomeHtmlDuration` - display time in milliseconds for the HTML welcome message.
- `welcomePermission` - optional flag. When set, only players holding this flag receive the welcome message. Leave empty to send to everyone.

## Advertisements

The advertisements file is stored at `addons/swiftly/configs/plugins/SimpleAdvertisement/advertisements.jsonc`:

```jsonc
{
  "Rules": {
    "1": {
      "chat": "{green}Buy VIP on www.buyvip.com",
      "permissions": "vip",
      "triggerad": ["buyvip", "vip"]
    },
    "2": {
      "message": {
        "en": "{green}Welcome to our server!",
        "pt-BR": "{green}Bem-vindo ao nosso servidor!"
      },
      "displaytype": "chat"
    },
    "3": {
      "message": {
        "en": "<font color='#FFD700'>Follow us on social media!</font>",
        "pt-BR": "<font color='#FFD700'>Siga-nos nas redes sociais!</font>"
      },
      "displaytype": "centerhtml",
      "permissions": ["vip", "premium"],
      "duration": 8000,
      "playerfilter": "dead",
      "phase": "live"
    },
    "4": {
      "chat": "{yellow}Server is warming up - get ready!",
      "phase": "warmup"
    }
  }
}
```

- `chat` - a colored chat message sent to players (single language format).
- `centerhtml` - an HTML message displayed in the center of the screen (single language format). Color tags are not supported here.
- `message` - a dictionary of localized messages per language code (e.g. `"en"`, `"pt-BR"`, `"de"`, etc.). The plugin automatically matches each player's game language (or falls back to `"en"` / the first available translation).
- `displaytype` (or `type`) - how the `message` should be displayed: `"chat"` or `"centerhtml"`.
- `duration` - optional display time in milliseconds for center HTML rules, defaults to 10000.
- `permissions` - optional. Restricts the ad to players holding at least one of the given flags. Accepts a single string (`"vip"`), a comma separated string (`"vip, premium"`) or an array (`["vip", "premium"]`). Flags are defined in `addons/swiftly/configs/permissions.jsonc`. Omit to send to everyone.
- `triggerad` - optional. Registers chat command(s) that let players view this announcement on demand, e.g. `"triggerad": "buyvip"` or `"triggerad": ["buyvip", "comprarvip", "vip"]` lets players type `!buyvip`, `!comprarvip`, or `!vip` (or with `/`) to see the ad. The response is sent privately only to the player who executed the command. The rule's `permissions`, `playerfilter` and `phase` conditions still apply.
- `playerfilter` - optional. Restricts which players see the ad:
  - `"all"` (default) - everyone.
  - `"alive"` - only alive players.
  - `"dead"` - only dead players.
  - `"spectators"` - only players spectating (not on a team).
  - `"players"` - only players in a team (not spectating).
- `phase` - optional. Restricts when the ad is shown:
  - `"any"` (default) - always.
  - `"warmup"` - only during map warmup.
  - `"live"` - only when the match is live (not warmup).

## Placeholders

If the [PlaceholderAPI](https://github.com/SwiftlyS2-Plugins/PlaceholderAPI) plugin is installed, placeholders in `chat`, `centerhtml`, `welcomeMessage` and `welcomeCenterHtml` are resolved per player. Some built-in placeholders:

- `{PLAYERNAME}` - the player's name
- `{STEAMID}` - the player's SteamID
- `{PLAYERIP}` - the player's IP address
- `{PLAYERCOUNT}` / `{MAXPLAYERS}` - current / max players
- `{HOSTNAME}` - the server's hostname
- `{MAPNAME}` - the current map
- `{SERVERIP}` / `{SERVERPORT}` - the server's IP / port
- `{DATE}` / `{TIME}` / `{DATETIME}` / `{UPTIME}` - current date and time

See the [PlaceholderAPI README](https://github.com/SwiftlyS2-Plugins/PlaceholderAPI) for the full list and how other plugins register custom placeholders.

## Commands

- `sw_reloadadvertisement` - reloads `advertisements.jsonc` (and re-registers trigger commands) and restarts the advertisement loop. Requires the `z` (root) flag in `addons/swiftly/configs/permissions.jsonc`. Can also be executed from the server console.
- `<triggerad>` - every command configured in `triggerad` (e.g. `!buyvip`, `!vip`). No `sw_` prefix is required.

## Supported Colors

`chat` messages support color tags using both `{color}` and `[color]` formats (case-insensitive):

- `{default}` / `[default]`, `{white}` / `[white]`
- `{darkred}` / `[darkred]`
- `{purple}` / `[purple]`
- `{green}` / `[green]`
- `{lightyellow}` / `[lightyellow]`, `{lightgreen}` / `[lightgreen]`
- `{lime}` / `[lime]`
- `{red}` / `[red]`
- `{grey}` / `[grey]`, `{gray}` / `[gray]`
- `{yellow}` / `[yellow]`
- `{gold}` / `[gold]`, `{orange}` / `[orange]`
- `{silver}` / `[silver]`
- `{blue}` / `[blue]`
- `{darkblue}` / `[darkblue]`
- `{bluegrey}` / `[bluegrey]`
- `{magenta}` / `[magenta]`
- `{lightred}` / `[lightred]`
- `{olive}` / `[olive]`

## Building

- Use the `dotnet publish -c Release` command to build and package the plugin.
- The output DLL is placed in the `build/` directory and a zip file is created for distribution.
- The release zip includes `PlaceholderAPI.Contract.dll` so the plugin keeps working even when the PlaceholderAPI plugin is not installed.

## LICENSE
SimpleAdvertisements is released under the MIT License. You can use it, change it and share it on your own server. The one thing you must keep is the copyright notice with the original author name. See the LICENSE file for the full text.
