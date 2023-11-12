# LethalCompanyGameMaster
Essentially a gamemode for lethal company that allows the host to control how dangerous the game is by spawning enemies.

*I know the code is disorganized and could be 10x better. Unfortunately, I won't be making it cleaner, the important parts are efficient.

# Features
- Customizable server name and round message
- Optional modifiers such as infinite sprint(you only), infinite credits, and infinite deadline
- Customizable AI tweaks, mainly focused on speed adjustments
- The ability to spawn enemies at will
- Removes all enemies normally
- Only the host needs to install it, no one else does
- Hides sent commands, but optionally can show them
- buy and togglelights commands
- customizable command prefix (default /)
- ability to toggle natural spawning via settings(not commands yet)
- ability to spawn jester on maps where he normally couldn't spawn after traveling to a map where he could spawn. *note: adding all enemies to this list later

# How to use the spawn function
1. Once in-game, open chat
2. type in spawn then the name of the enemy you'd like, and optionally how many. E.g. spawn spring 5 or spawn spring
3. Enemies will spawn randomly among the list of available spawn points, inside enemies spawn inside, and outside enemies spawn outside

# You may not be fully aware of the names, so here's a list of the enemy names:
*Note: You don't need to type the full name, for example, "spider" will work for the bunker spider.

Inside:
Girl
Lasso
Bunker Spider
Centipede
Blob
Flowerman
Spring
Crawler
Hoarding bug
Jester
Puffer

Outside:
ForestGiant
MouthDog
Earth Leviathan

# Installation
1. Install BepInEx
2. Run game once with BepInEx installed to generate folders/files
3. Drop the DLL inside of the BepInEx/plugins folder
4. Run game once to generate .cfg file
5. If you'd like to customize the mod, head to BepInEx/config and modify the config file

