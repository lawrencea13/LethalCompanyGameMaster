# LethalCompanyGameMaster
Essentially a gamemode for lethal company that allows the host to control how dangerous the game is by spawning enemies.

*I know the code is disorganized and could be 10x better. Unfortunately, I won't be making it cleaner, the important parts are efficient.

# Features
- built-in menu, opened with the Insert key
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
Baboon Bird

# How to use the buy function
1. Once in-game, open chat
2. type in buy then the name of the item you'd like to buy, and optionally how many. e.g. /buy flash 4 will buy 4 pro flashlights
3. These items will be assigned to their own drop pod, not added to the next one coming in.
4. You can also use the item id in place of the name for single purchases. e.g. /buy 4 will buy 1 pro flashlight

Here is the item list alongside their IDs:
  { "Walkie-Talkie", 0 },
  { "Pro Flashlight", 4 },
  { "Normal Flashlight", 1 },
  { "Shovel", 2 },
  { "Lockpicker", 3 },
  { "Stun Grenade", 5 },
  { "Boom Box", 6 },
  { "Inhaler", 7 },
  { "Stun Gun", 8 },
  { "Jet Pack", 9 },
  {"Extension Ladder", 10 },
  {"Radar Booster", 11 }


# Installation
1. Install BepInEx
2. Run game once with BepInEx installed to generate folders/files
3. Drop the DLL inside of the BepInEx/plugins folder
4. No further steps needed

# Commands

- /spawn {enemy} {amount}
- -Example: /spawn dog 5
- /buy {item} {amount}
- -Example: /buy pro 3
- /togglelights
- /nightvision
- -can use /night or /vision
- /speed
- /god








