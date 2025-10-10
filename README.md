# Clustertruck Speedrun Mod
![release_shield](https://img.shields.io/github/v/release/noahkra/ClustertruckSpeedrunMod?include_prereleases&color=blue) ![totaldownloads_shield](https://img.shields.io/github/downloads/noahkra/ClustertruckSpeedrunMod/total?label=total%20downloads) ![latestdownloads_shield](https://img.shields.io/github/downloads-pre/noahkra/ClustertruckSpeedrunMod/latest/total) 

![bug shield](https://img.shields.io/github/issues-raw/noahkra/ClustertruckSpeedrunMod/bug) ![enhancements shield](https://img.shields.io/github/issues-raw/noahkra/ClustertruckSpeedrunMod/enhancement) ![closedissues shield](https://img.shields.io/github/issues-closed-raw/noahkra/ClustertruckSpeedrunMod?color=green)

A patcher for Clustertruck that adds quality of life improvements, mods for speedrunning and more!

> [!caution]
> ***PLEASE NOTE!*** The speedometer is for practice only and is ***NOT*** allowed on the SRC leaderboards!

If you have any feature requests or want to report a bug, make sure to submit an [issue](https://github.com/noahkra/ClustertruckSpeedrunMod/issues/new/choose) and it might get added in a future release!

# Contents
- [Features](#features)
- [How to use](#how-to-use)
- [FAQ](#faq)
***

# Features

## Essentials
- Unlock FPS
	- Up to 240fps, which is the maximum allowed by the SRC leaderboards.
- Enable FPS counter
- Fix audio bug caused by unlocked FPS
- Confine cursor to the game window
- Press escape to skip the credits
- Invert Sprint Button
- Fix in-game timer accuracy
- Enable Timer By Default

## Gameplay
- Randomiser
	- Play through all 90 levels in a random order with random abilities, ending in 9:10.
- Disable jump 
	- For jumpless categories.
- Two new abilities that were hidden in the game's code
	- Truck cannon: Press RMB to fire a truck.
	- Surfing shoes: Give a passive boost to movement.

## LiveSplit
- Enable LiveSplit Auto splitter
	- Split By World / Split By Level
	- Reset on previous level select

## Miscellaneous
- Custom Truck Colours
- Enable Speedometer
	- Configurable to show speed in m/s, km/h or mph.
    - Option to split the speedometer into horizontal and vertical values.

### Features wishlist:
- Airtime timer (for flying% category)
- Low grav patch (for LowGrav category)
- Multiplayer
- Preload 9:10

# How to use
1. Put the folder anywhere and run the exe.
2. Select your Clustertruck folder.
3. Select the desired patches.
4. Press the "Apply Patches" button.
5. Launch the game as normal.
6. To restore, simply press the "Remove All Patches" button.

### Auto splitter
To enable the auto splitter just enable it in the patches. Select "Split By World" or "Split By Level" depending on your splits.
Requirements:
- You must use LiveSplit 1.8.29 or above. Older versions are not supported.
- Make sure any other auto splitters, such as the one built into LiveSplit, are disabled.
> [!important]
> This mod is not compatible with happyrobot33's auto splitter. Instead, use the auto splitter patch provided.

# FAQ
**It's asking me to install something called .NET Desktop Runtime?**

That's correct. It's needed to run the application. (It's not a virus. Promise.)

**I inverted sprint but I'm not sprinting?**

You probably still have sprint bound to 'w' and now that Invert Sprint is on, you're always holding the 'walk' button. Set sprint back to left shift.

**Can I submit a run to the SRC leaderboards with the speedometer enabled?**

No.

**I set my FPS to 240 but I'm only getting x?**

Your pc probably can't render more than x.

**My music keeps cutting out when the points audio is playing?**

This can happen when unlocking the framerate. Make sure to enable the Points Audio Fix patch.

**My auto splitter isn't working?**

Make sure you are using the latest version of LiveSplit, have the auto splitter patch enabled and have deactivated the auto splitter option built into LiveSplit.

**Why does my auto splitter not split when I finish the level?**

You probably have it set to Split by World. Change the setting to Split by Level.

**My jump button doesn't work anymore!**

You've probably enabled the jumpless patch. Re-patch the game with it disabled.

