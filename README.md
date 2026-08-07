<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" />
  <h2><strong><a href="https://github.com/SyntX34/CS2-SwiftlyS2_SimpleAdvertisements">SimpleAdvertisement</a></strong></h2>
  <h3>A simple advertisements plugin for SwiftlyS2 CS2 servers.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build Status">
  <img src="https://img.shields.io/github/downloads/SyntX34/CS2-SwiftlyS2_SimpleAdvertisements/total" alt="Downloads">
  <img src="https://img.shields.io/github/stars/SyntX34/CS2-SwiftlyS2_SimpleAdvertisements?style=flat&logo=github" alt="Stars">
  <img src="https://img.shields.io/github/license/SyntX34/CS2-SwiftlyS2_SimpleAdvertisements" alt="License">
</p>

## Features

- Colored chat advertisements
- Center HTML advertisements
- Configurable advertisement interval
- Optional advertisement reload on map change
- Admin command to reload advertisements on demand

## Installation

1. Download the latest release.
2. Extract the contents into the `addons/swiftly/` directory of your server.
3. Start the server. The plugin will create `config.jsonc` and `advertisements.jsonc` automatically.

## Configuration

The plugin configuration is stored at `addons/swiftly/configs/plugins/SimpleAdvertisement/config.jsonc`:

```jsonc
{
  "config": {
    "enabled": true,
    "interval": 60,
    "reloadOnMapChange": true,
    "order": "forward",
    "skipDuplicate": true
  }
}
```

- `enabled` - enables or disables the plugin.
- `interval` - seconds between each advertisement.
- `reloadOnMapChange` - reloads `advertisements.jsonc` on every map change.
- `order` - advertisement selection order. `"forward"` cycles from the first entry to the last, `"reverse"` cycles from the last to the first, and `"random"` picks a random entry each time.
- `skipDuplicate` - only applies when `order` is `"random"`. When `true`, the same advertisement is never shown twice in a row. When `false`, repeats are allowed.

## Advertisements

The advertisements file is stored at `addons/swiftly/configs/plugins/SimpleAdvertisement/advertisements.jsonc`:

```jsonc
{
  "Rules": {
    "1": {
      "chat": "{green}this is first advertisement"
    },
    "2": {
      "centerhtml": "second rule in english"
    }
  }
}
```

- `chat` - a colored chat message sent to all players.
- `centerhtml` - an HTML message displayed in the center of the screen. Color tags are not supported here.
- `duration` - optional display time in milliseconds for `centerhtml` rules, defaults to 10000.

## Commands

- `sw_reloadadvertisement` - reloads `advertisements.jsonc` and restarts the advertisement loop. Requires the `z` (root) flag in `addons/swiftly/configs/permissions.jsonc`. Can also be executed from the server console.

## Supported Colors

`chat` messages support the following color tags:

- `{green}`
- `{red}`
- `{blue}`
- `{yellow}`
- `{purple}`
- `{white}`

The SwiftlyS2 framework also supports `{default}`, `{grey}`, `{orange}`, `{olive}`, `{lightyellow}` and `{darkred}`.

## Building

- Use the `dotnet publish -c Release` command to build and package the plugin.
- The output DLL is placed in the `build/` directory and a zip file is created for distribution.


## LICENSE
SimpleAdvertisements is released under the MIT License. You can use it, change it and share it on your own server. The one thing you must keep is the copyright notice with the original author name. See the LICENSE file for the full text.