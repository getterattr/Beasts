# Beasts

> [!NOTE]
> Fork of [poplug/Beasts](https://github.com/poplug/Beasts) — originally released by PoThePlug on [ownedcore](https://www.ownedcore.com/forums/mmo/path-of-exile/poe-bots-programs/1001716-exileapi-poehelper-poehud-plugin-beasts-easily-find-profitable-beasts.html).

An ExileApi overlay plugin for Path of Exile. Highlights valuable Bestiary beasts as you clear maps.

- Draws in-world labels and ground circles on tracked beasts.
- Annotates each beast with its current poe.ninja value.
- Shows prices on captured beast items in your inventory, stash, and the Bestiary panel.
- Copies a search regex to your clipboard when you open the Bestiary panel, so matching captures are pre-filtered - optionally auto-pastes it into the search field.

## Install

### Requirements

- Path of Exile should be running in Windowed or Windowed Fullscreen mode.
- [ExileApi](https://github.com/exApiTools/ExileApi-Compiled).
- .NET 10 SDK.

### Install With PluginUpdater

1. Open [ExileApi](https://github.com/exApiTools/ExileApi-Compiled).
2. Open the `PluginUpdater` plugin.
3. Click the Add tab.
4. Paste `https://github.com/getterattr/Beasts` into Repository URL.
5. Click Clone.
6. Either restart ExileApi, or open ExileApi Core settings, scroll down, and press Reload Plugins.

### Install From Source Folder

1. Download or clone this repository.
2. Place the `Beasts` folder inside your `Plugins/Source/` directory.
3. Launch ExileApi.
4. Let the host compile the plugin.
5. Enable Beasts in the plugin settings.

## Setup

1. Open the Beasts settings panel in the ExileApi menu and toggle **Enabled**.
2. Under **Price Data**, set your current league name (must match exactly, e.g. `Mirage`) and press **Refresh Prices** to pull the latest values from poe.ninja.
3. Under **Price Data**, curate your **Tracked Beasts** list. Use **Select 15c+** for a sensible default, or hand-pick.
4. Under **Markers & Prices**, tune overlay colours, capture-status text, and layout.
5. Under **Bestiary Clipboard**, decide whether the generated regex just copies to the clipboard or also auto-pastes into the Bestiary search field when the panel opens.
6. Toggle **Auto-Hide Overlays** at the top level if you want the overlays to hide automatically in hideout/town or when any UI panel (inventory, stash, atlas, Bestiary) is open.
