# Show Precise Accuracy Text

This mod for UNBEATABLE fixes some bugs with Assist Mode, and allows you to change the offset that Assist Mode will hit notes with.

- Fixes the bug where Assist Mode will fail to dodge when there are multiple spikes on-screen in a lane.
- Makes Assist Mode not try to dodge spikes that it is already going to dodge (e.g. spikes are coming in the top lane and Beat is standing still in the bottom lane).
- Allows you to change when Assist Mode will hit notes and dodge spikes.

## Compatible game versions

- UNBEATABLE Demo
- UNBEATABLE \[white label\]

## Requirements

- [BepInEx](https://github.com/BepInEx/BepInEx)

## Installation

1. Download and install BepInEx into your game directory (if you use [CustomBeatmaps](https://github.com/gold-me/CustomBeatmapsV4), you have this installed already)
2. Run the game, then close it
3. [Download this mod](https://github.com/Zachava96/SmarterAssistMode/releases)
4. Merge the BepInEx folder from this mod with the BepInEx folder in your game directory
5. Run the game

## Configuration

You can change these options in the `BepInEx/config/net.zachava.smarterassistmode.cfg` file:

- `BetterSpikeVisionEnabled`
    - Sets if the mod will patch the issue with failing to dodge when there are multiple spikes on-screen in a lane
- `BetterSpikeInputsEnabled`
    - Sets if the mod will patch Assist Mode to not try to dodge spikes that it is already going to dodge.
- `NoteTargetOffsetValue`
    - Sets the offset at which Assist Mode will start to try to hit notes.
    - Depending on game conditions, Assist Mode may be late to the hit by some milliseconds.
- `SpikeTargetOffsetValue`
    - Sets the offset at which Assist Mode will start to try to dodge spikes.
    - Depending on game conditions, Assist Mode may be late to the dodge by some milliseconds.