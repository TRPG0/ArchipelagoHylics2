# ArchipelagoHylics2

A client for connecting the game Hylics 2 to an Archipelago randomizer.

## Required Software

- Hylics 2 from: [Steam](https://store.steampowered.com/app/1286710/Hylics_2/) or [itch.io](https://mason-lindroth.itch.io/hylics-2)
- BepInEx from: [GitHub](https://github.com/BepInEx/BepInEx/releases)

## Instructions (Windows)

1. Download and install BepInEx 5 (32-bit, version 5.4.20 or newer) to your Hylics 2 root folder. Do not use any versions of BepInEx 6.

2. Start Hylics 2 once so that BepInEx can create its required configuration files.

3. Download the latest version of ArchipelagoHylics2 from the [Releases]() page and extract the contents of the zip file into `BepInEx\plugins`.

4. Start Hylics 2 again. To verify that the mod is working, begin a new game or load a save file, and then open the console. (default key: `/`)

## Connecting

To connect to an Archipelago server, open the in-game console and use the command `/connect [address:port] [name] [password]`. The port and password are both optional arguments - if no port is provided then the default port of 38281 is used.

## Other Commands

There are a few additional commands that can be used while playing Hylics 2 randomizer:

- `/popups` - Enables or disables in-game messages when an item is found or recieved.
- `/airship` - Resummons the airship at the dock above New Muldul and teleports Wayne to it, in case the player gets stuck. Player must have the DOCK KEY to use this command.
- `/deathlink` - Enables or disables DeathLink.
- `![command]` - Entering any command with an `!` at the beginning allows for remotely sending commands to the server.

# Building from source

If for any reason you are interested in building the mod from source, you will need a copy of the `ORKFramework.dll` and `ORKFrameworkCore.dll ` files. **These files will not be provided for you and must be acquired on your own.**