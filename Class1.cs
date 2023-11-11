using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using DunGen;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using Steamworks;
using Steamworks.Data;
using System.Collections;
using System.Security.AccessControl;
using GameNetcodeStuff;
using BepInEx.Configuration;
using System.Reflection;
using Unity.Netcode;

namespace LethalCompanyTestMod
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class TestMod : BaseUnityPlugin
    {
        // Mod Details
        private const string modGUID = "Posiedon.GameMaster";
        private const string modName = "Lethal Company GameMaster";
        private const string modVersion = "1.0.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);


        public static Dictionary<SelectableLevel, List<SpawnableEnemyWithRarity>> levelEnemySpawns;
        public static Dictionary<SpawnableEnemyWithRarity, int> enemyRaritys;
        public static Dictionary<SpawnableEnemyWithRarity, AnimationCurve> enemyPropCurves;
        public static ManualLogSource mls;

        #region Settings

        private static ConfigEntry<string> ServerName;
        private static ConfigEntry<string> RoundPost;
        private static ConfigEntry<float> SpringSpeed;
        private static ConfigEntry<float> SpringAnimSpeed;
        private static ConfigEntry<float> PopUpTimer;
        private static ConfigEntry<float> MaxJesterSpeed;
        private static ConfigEntry<float> CrankingTimer;
        private static ConfigEntry<float> JesterResetTimer;

        private static ConfigEntry<int> MinScrap;
        private static ConfigEntry<int> MaxScrap;
        private static ConfigEntry<int> MinScrapValue;
        private static ConfigEntry<int> MaxScrapValue;


        private static ConfigEntry<bool> HideCommandMessages;
        private static ConfigEntry<bool> HideEnemySpawnMessages;
        private static ConfigEntry<bool> EnableInfiniteSprint;
        private static ConfigEntry<bool> EnableInfiniteCredits;
        private static ConfigEntry<bool> EnableInfiniteDeadline;

        #endregion

        private static SelectableLevel currentLevel;
        private static EnemyVent[] currentLevelVents;
        private static RoundManager currentRound;

        void Awake()
        {
            mls = BepInEx.Logging.Logger.CreateLogSource("GameMaster");
            // Plugin startup logic
            mls.LogInfo("Loaded GameMaster. Patching.");
            harmony.PatchAll(typeof(TestMod));
            mls = Logger;
            enemyRaritys = new Dictionary<SpawnableEnemyWithRarity, int>();
            levelEnemySpawns = new Dictionary<SelectableLevel, List<SpawnableEnemyWithRarity>>();
            enemyPropCurves = new Dictionary<SpawnableEnemyWithRarity, AnimationCurve>();
            
            ServerName = Config.Bind("Server Settings", "Server Name", "<color=red>G</color><color=blue>a</color><color=yellow>m</color><color=green>e</color><color=orange>M</color><color=purple>a</color><color=red>s</color><color=blue>t</color><color=yellow>e</color><color=green>r</color>", "Set the server name when creating a server");
            RoundPost = Config.Bind("Server Settings", "Round Comment", "<color=white>Custom game by </color><color=blue>Poseidon</color>", "A message that the server sends every round");
            SpringSpeed = Config.Bind("AI Settings", "Spring Head - Speed", 100f, new ConfigDescription("Base speed for springhead", new AcceptableValueRange<float>(0.1f, 150f)));
            SpringAnimSpeed = Config.Bind("AI Settings", "Spring Head - Speed Multiplier", 4f, new ConfigDescription("A speed multiplier for the springhead", new AcceptableValueRange<float>(0.1f, 10f)));
            PopUpTimer = Config.Bind("AI Settings", "Jester - PopUp", 0.5f, new ConfigDescription("How long it takes the jester to popup", new AcceptableValueRange<float>(0.1f, 100f)));
            MaxJesterSpeed = Config.Bind("AI Settings", "Jester - Max Speed Multiplier", 5f, new ConfigDescription("The maximum speed the jester can go, multiplier", new AcceptableValueRange<float>(0.1f, 10f)));
            CrankingTimer = Config.Bind("AI Settings", "Jester - Cranking", 0.5f, new ConfigDescription("The time it takes the jester to begin cranking", new AcceptableValueRange<float>(0.1f, 100f)));
            JesterResetTimer = Config.Bind("AI Settings", "Jester - Reset", 5000f, new ConfigDescription("The time it takes for the jester to reset when no one is in the building", new AcceptableValueRange<float>(5f, 5000f)));
            HideCommandMessages = Config.Bind("Command Settings", "Hide Spawn Messages", true, "Should the server hide your commands? true will hide, false will show");
            HideEnemySpawnMessages = Config.Bind("Command Settings", "Hide Enemy Spawn Messages", true, "Should the server hide messages an enemy may send when it spawns? true will hide, false will show.");
            EnableInfiniteSprint = Config.Bind("Host Settings", "Enable infinite sprint", true, "If true, stamina will never deplete, if false, stamina will work as normal.");
            EnableInfiniteCredits = Config.Bind("Host Settings", "Enable infinite credits", true, "If true, credits will always revert back to the preset value.  If false, they will work as normal.");
            EnableInfiniteDeadline = Config.Bind("Host Settings", "Enable infinite deadline", true, "If true, the deadline will never go down and will always stay at 8 days. If false, it will drop like normal.");

            MinScrap = Config.Bind("Scrap Settings", "Minimum Scap", 20, "Set the minimum pieces of scrap in the level");
            MaxScrap = Config.Bind("Scrap Settings", "Minimum Scap", 45, "Set the minimum pieces of scrap in the level");
            MinScrapValue = Config.Bind("Scrap Settings", "Minimum Scap", 3000, "Set the minimum pieces of scrap in the level");
            MaxScrapValue = Config.Bind("Scrap Settings", "Minimum Scap", 8000, "Set the minimum pieces of scrap in the level");
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.RunTerminalEvents))]
        [HarmonyPostfix]
        static void NeverLoseCredits(ref int ___groupCredits)
        {
            if(EnableInfiniteCredits.Value) { ___groupCredits = 50000; }
        }

        [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyCreated")]
        [HarmonyPrefix]
        static void UpdateServerSettings(ref HostSettings ___lobbyHostSettings, ref Lobby lobby)
        {
            if (ServerName.Value != "")
            {
                ___lobbyHostSettings.lobbyName = ServerName.Value;
            }
            //lobby.MaxMembers = 8; // doesn't do anything
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.MoveGlobalTime))]
        [HarmonyPostfix]
        static void InfiniteDeadline(ref float ___timeUntilDeadline)
        {
            if(EnableInfiniteDeadline.Value) { ___timeUntilDeadline = 10000; }
            
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevel))]
        [HarmonyPrefix]
        static bool ModifyLevel(ref SelectableLevel newLevel)
        {
            currentRound = RoundManager.Instance;
            if (!levelEnemySpawns.ContainsKey(newLevel))
            {
                List<SpawnableEnemyWithRarity> spawns = new List<SpawnableEnemyWithRarity>();
                foreach (var item in newLevel.Enemies)
                {
                    spawns.Add(item);
                }
                levelEnemySpawns.Add(newLevel, spawns);
            }
            List<SpawnableEnemyWithRarity> spawnableEnemies;
            levelEnemySpawns.TryGetValue(newLevel, out spawnableEnemies);
            newLevel.Enemies = spawnableEnemies;

            // make a dictionary of the inside enemy rarities
            foreach (var enemy in newLevel.Enemies)
            {
                if (!enemyRaritys.ContainsKey(enemy))
                {
                    enemyRaritys.Add(enemy, enemy.rarity);
                }
                int rare = 0;
                enemyRaritys.TryGetValue(enemy, out rare);
                enemy.rarity = rare;
            }

            // make a dictionary of the outside enemy rarities
            foreach (var enemy in newLevel.OutsideEnemies)
            {
                if (!enemyRaritys.ContainsKey(enemy))
                {
                    enemyRaritys.Add(enemy, enemy.rarity);
                }
                int rare = 0;
                enemyRaritys.TryGetValue(enemy, out rare);
                enemy.rarity = rare;
            }

            foreach (var enemy in newLevel.Enemies)
            {
                if (!enemyPropCurves.ContainsKey(enemy))
                {
                    enemyPropCurves.Add(enemy, enemy.enemyType.probabilityCurve);
                }
                AnimationCurve prob = new AnimationCurve();
                enemyPropCurves.TryGetValue(enemy, out prob);
                enemy.enemyType.probabilityCurve = prob;
            }
            HUDManager.Instance.AddTextToChatOnServer("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n");
            if(RoundPost.Value != "")
            {
                HUDManager.Instance.AddTextToChatOnServer(RoundPost.Value);
            }
            




            // remove rarity for all enemies
            foreach (var enemy in newLevel.Enemies)
            {
                enemy.rarity = 0;

            }

            foreach (var enemy in newLevel.OutsideEnemies)
            {
                enemy.rarity = 0;
            }

            // create temporary version of level to modify
            SelectableLevel n = newLevel;


            // adjust scrap values
            // not necessarry for this particular mode
            n.minScrap = MinScrap.Value;
            n.maxScrap = MaxScrap.Value;
            n.minTotalScrapValue = MinScrapValue.Value;
            n.maxTotalScrapValue = MaxScrapValue.Value;

            foreach(var item in n.spawnableMapObjects)
            {
                if(item.prefabToSpawn.GetComponentInChildren<Landmine>() != null)
                {
                    //item.numberToSpawn = new UnityEngine.AnimationCurve(new UnityEngine.Keyframe(0f, 300f), new UnityEngine.Keyframe(1f, 170f));
                }
            }


            // set actual current level to the modified version
            newLevel = n;

            return true;
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.PlotOutEnemiesForNextHour))]
        [HarmonyPrefix]
        static void autoManualSpawn(ref EnemyVent[] ___allEnemyVents, ref SelectableLevel ___currentLevel)
        {
            // remove for testing
            return;
            /*
            // theoretically adds the jester to maps where the jester doesn't exist
            // doesn't work, tosses error, gonna leave it alone for now.
            
            JesterAI jester = new JesterAI();
            SpawnableEnemyWithRarity jesterSpawnable = new SpawnableEnemyWithRarity
            {
                enemyType = jester.enemyType,
                rarity = 100
            };

            List<SpawnableEnemyWithRarity> Enemies = new List<SpawnableEnemyWithRarity>
            {
                jesterSpawnable
            };
            Console.WriteLine(Enemies[0].enemyType.ToString());

            //___currentLevel.Enemies = new List<SpawnableEnemyWithRarity>();


            
            bool foundJester = false;
            foreach (var enemy in ___currentLevel.Enemies)
            {

                if (enemy.enemyType = jester.enemyType)
                {
                    foundJester = true;
                }
            }
            if (!foundJester) 
            {
                ___currentLevel.Enemies.Add(jesterSpawnable);
                mls.LogInfo("Attempting to add jester to spawn table");
            }
            else
            {
                mls.LogInfo("Jester already in spawn table");
            }
            */
            // define a limit for each thing we'd like to spawn
            Dictionary<string, int> spawnLimits = new Dictionary<string, int>
            {
                { "Spring", 3 },
                { "Flower", 0 },
                { "Jester", 1 },
                { "Crawler", 0 },
                { "Blob", 0 },
                { "Centipede", 0 },
                { "Bug", 0 },
                { "Lasso", 0 },
                { "Spider", 0 },
                { "Girl", 1 }
            };

            mls.LogInfo("Current Level: [ID:" + ___currentLevel.levelID.ToString() + "] " + ___currentLevel.name);

            // adjust spawn limits based on level since different enemies can spawn in different levels
            switch (___currentLevel.levelID)
            {
                case 0:
                    // experimentation
                    spawnLimits["Spring"] = 3;
                    spawnLimits["Flower"] = 1;
                    spawnLimits["Jester"] = 0;
                    spawnLimits["Crawler"] = 3;
                    spawnLimits["Blob"] = 0;
                    spawnLimits["Centipede"] = 0;
                    spawnLimits["Bug"] = 0;
                    spawnLimits["Lasso"] = 0;
                    spawnLimits["Spider"] = 0;
                    spawnLimits["Girl"] = 1;
                    break;
                case 1:
                    // Assurance
                    spawnLimits["Spring"] = 3;
                    spawnLimits["Flower"] = 1;
                    spawnLimits["Jester"] = 0;
                    spawnLimits["Crawler"] = 3;
                    spawnLimits["Blob"] = 0;
                    spawnLimits["Centipede"] = 0;
                    spawnLimits["Bug"] = 0;
                    spawnLimits["Lasso"] = 0;
                    spawnLimits["Spider"] = 0;
                    spawnLimits["Girl"] = 1;
                    break;
                case 2:
                    // Vow
                    spawnLimits["Spring"] = 3;
                    spawnLimits["Flower"] = 1;
                    spawnLimits["Jester"] = 0;
                    spawnLimits["Crawler"] = 3;
                    spawnLimits["Blob"] = 0;
                    spawnLimits["Centipede"] = 0;
                    spawnLimits["Bug"] = 0;
                    spawnLimits["Lasso"] = 0;
                    spawnLimits["Spider"] = 0;
                    spawnLimits["Girl"] = 1;
                    break;
                case 3:
                    // Company Building
                    break;
                case 4:
                    // March
                    spawnLimits["Spring"] = 3;
                    spawnLimits["Flower"] = 1;
                    spawnLimits["Jester"] = 0;
                    spawnLimits["Crawler"] = 3;
                    spawnLimits["Blob"] = 0;
                    spawnLimits["Centipede"] = 0;
                    spawnLimits["Bug"] = 0;
                    spawnLimits["Lasso"] = 0;
                    spawnLimits["Spider"] = 0;
                    spawnLimits["Girl"] = 1;
                    break;
                case 5:
                    // Rend
                    spawnLimits["Spring"] = 2;
                    spawnLimits["Flower"] = 0;
                    spawnLimits["Jester"] = 5;
                    spawnLimits["Crawler"] = 0;
                    spawnLimits["Blob"] = 0;
                    spawnLimits["Centipede"] = 0;
                    spawnLimits["Bug"] = 0;
                    spawnLimits["Lasso"] = 0;
                    spawnLimits["Spider"] = 0;
                    spawnLimits["Girl"] = 2;
                    break;
                case 6:
                    // Dine
                    spawnLimits["Spring"] = 2;
                    spawnLimits["Flower"] = 0;
                    spawnLimits["Jester"] = 5;
                    spawnLimits["Crawler"] = 0;
                    spawnLimits["Blob"] = 0;
                    spawnLimits["Centipede"] = 0;
                    spawnLimits["Bug"] = 0;
                    spawnLimits["Lasso"] = 0;
                    spawnLimits["Spider"] = 0;
                    spawnLimits["Girl"] = 2;
                    break;
                case 7:
                    // Offense
                    spawnLimits["Spring"] = 3;
                    spawnLimits["Flower"] = 1;
                    spawnLimits["Jester"] = 0;
                    spawnLimits["Crawler"] = 3;
                    spawnLimits["Blob"] = 0;
                    spawnLimits["Centipede"] = 0;
                    spawnLimits["Bug"] = 0;
                    spawnLimits["Lasso"] = 0;
                    spawnLimits["Spider"] = 0;
                    spawnLimits["Girl"] = 1;
                    break;
                case 8:
                    // Titan
                    spawnLimits["Spring"] = 2;
                    spawnLimits["Flower"] = 0;
                    spawnLimits["Jester"] = 5;
                    spawnLimits["Crawler"] = 0;
                    spawnLimits["Blob"] = 0;
                    spawnLimits["Centipede"] = 0;
                    spawnLimits["Bug"] = 0;
                    spawnLimits["Lasso"] = 0;
                    spawnLimits["Spider"] = 0;
                    spawnLimits["Girl"] = 2;
                    break;
            }

            Dictionary<string, int> spawnAmounts = new Dictionary<string, int>
            {
                { "Spring", 0 },
                { "Flower", 0 },
                { "Jester", 0 },
                { "Crawler", 0 },
                { "Blob", 0 },
                { "Centipede", 0 },
                { "Bug", 0 },
                { "Lasso", 0 },
                { "Spider", 0 },
                { "Girl", 0 }
            };

            for (int i = 0; i < ___allEnemyVents.Length; i++)
            {
                // asign enemies to each vent in the game
                foreach(var enemy in ___currentLevel.Enemies)
                {
                    /*
                     * AI Name contains:
                     * Spring
                     * Girl
                     * Flower
                     * Jester
                     * Crawler
                     * Blob
                     * Centipede 
                     * Forest Giant
                     * Hoarder bug
                     * LassoMan
                     * Dog
                     * Puffer
                     * Spider
                     * Sandworm // only spawned outside
                     */
                    

                    foreach (KeyValuePair<string, int> entry in spawnLimits)
                    {
                        if (enemy.enemyType.enemyName.Contains(entry.Key) && spawnAmounts[entry.Key] < entry.Value)
                        {
                            enemy.rarity = 100;
                            enemy.enemyType.canDie = false;
                            enemy.enemyType.canBeStunned = false;
                            enemy.enemyType.canSeeThroughFog = true;
                            enemy.enemyType.probabilityCurve = new AnimationCurve(new Keyframe(0, 100f));
                            ___allEnemyVents[i].enemyType = enemy.enemyType;
                            spawnAmounts[entry.Key] += 1;
                            mls.LogInfo($"Added {entry.Key} to vent");
                        }
                    }

                    /*
                     * // base example, get an enemy type, if the amount of it that is spawned is < the limit, add it to a vent
                    if (enemy.enemyType.enemyName.Contains("Spring") && spawnAmounts["Spring"] < spawnLimits["Spring"])
                    {
                        enemy.rarity = 100;
                        enemy.enemyType.probabilityCurve = new AnimationCurve(new Keyframe(0, 100f));
                        ___allEnemyVents[i].enemyType = enemy.enemyType;
                        spawnAmounts["Spring"] += 1;
                        mls.LogInfo("Added spring head to vent");
                    }

                    */
                }
                // tell vents to spawn immediately
                ___allEnemyVents[i].spawnTime = 0;
            }
        }

        [HarmonyPatch(typeof(RoundManager), "AdvanceHourAndSpawnNewBatchOfEnemies")]
        [HarmonyPrefix]
        static void updateCurrentLevelInfo(ref EnemyVent[] ___allEnemyVents, ref SelectableLevel ___currentLevel)
        {
            currentLevel = ___currentLevel;
            currentLevelVents = ___allEnemyVents;
        }

        [HarmonyPatch(typeof(HUDManager), "SubmitChat_performed")]
        [HarmonyPrefix]
        static void GameMasterCommands(HUDManager __instance)
        {
            
            string text = __instance.chatTextField.text;

            mls.LogInfo(text);

            if (text.ToLower().StartsWith("spawn"))
            {
                string[] enteredText = text.Split(' ');
                if (enteredText.Length > 1)
                {
                    if (enteredText.Length > 2)
                    {
                        if (int.TryParse(enteredText[2], out int amountToSpawn))
                        {
                            bool foundEnemyMulti = false;
                            foreach (var enemy in currentLevel.Enemies)
                            {
                                if (enemy.enemyType.enemyName.ToLower().Contains(enteredText[1].ToLower()))
                                {
                                    foundEnemyMulti = true;
                                    try
                                    {
                                        for (int i = 0; i <= amountToSpawn; i++)
                                        {
                                            currentRound.SpawnEnemyOnServer(currentRound.allEnemyVents[Random.Range(0, currentRound.allEnemyVents.Length)].floorNode.position, currentRound.allEnemyVents[i].floorNode.eulerAngles.y, currentLevel.Enemies.IndexOf(enemy));
                                            mls.LogInfo("Spawned another " + enemy.enemyType.enemyName);
                                        }    
                                    }
                                    catch
                                    {
                                        mls.LogInfo("Failed to spawn enemies, check your command.");
                                    }
                                    if (foundEnemyMulti) { break; }
                                }
                            }

                            if (foundEnemyMulti) 
                            {
                                if (HideCommandMessages.Value)
                                {
                                    __instance.chatTextField.text = "";
                                }
                                return;
                            }

                            foreach (var outsideEnemy in currentLevel.OutsideEnemies)
                            {
                                if (outsideEnemy.enemyType.enemyName.ToLower().Contains(enteredText[1].ToLower()))
                                {
                                    foundEnemyMulti = true;
                                    try
                                    {
                                        for (int i = 0; i < amountToSpawn; i++)
                                        {
                                            mls.LogInfo("The index of " + outsideEnemy.enemyType.enemyName + " is " + currentLevel.OutsideEnemies.IndexOf(outsideEnemy));
                                            GameObject obj = UnityEngine.Object.Instantiate(currentLevel
                                            .OutsideEnemies[currentLevel.OutsideEnemies.IndexOf(outsideEnemy)]
                                            .enemyType.enemyPrefab, GameObject.FindGameObjectsWithTag("OutsideAINode")[Random.Range(0, GameObject.FindGameObjectsWithTag("OutsideAINode").Length - 1)].transform.position, Quaternion.Euler(Vector3.zero));
                                            obj.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
                                            mls.LogInfo("Spawned another " + outsideEnemy.enemyType.enemyName);
                                        }
                                            
                                    }
                                    catch
                                    {
                                        mls.LogInfo("Failed to spawn enemies, check your command.");
                                    }
                                    if (foundEnemyMulti) { break; }
                                }
                            }

                        }
                        else
                        {
                            mls.LogInfo("Failed to spawn enemies, check your command.");
                        }

                        mls.LogInfo("Length of input array: " + enteredText.Length);
                    }
                    mls.LogInfo("Got your message, trying to find the enemy");
                    // spawn 1 enemy
                    bool foundEnemy = false;
                    foreach (var enemy in currentLevel.Enemies)
                    {

                        // jester
                        // lasso
                        // spider
                        // centipede
                        // blob
                        // flowerman
                        // spring
                        // puffer

                        if (enemy.enemyType.enemyName.ToLower().Contains(enteredText[1].ToLower()))
                        {
                            try
                            {
                                foundEnemy = true;
                                currentRound.SpawnEnemyOnServer(currentRound.allEnemyVents[0].floorNode.position, currentRound.allEnemyVents[0].floorNode.eulerAngles.y, currentLevel.Enemies.IndexOf(enemy));

                                mls.LogInfo("Spawned " + enemy.enemyType.enemyName);
                            }
                            catch
                            {
                                mls.LogInfo("Could not spawn enemy");
                            }
                        }
                    }
                    // return
                    if (foundEnemy)
                    {
                        if (HideCommandMessages.Value)
                        {
                            __instance.chatTextField.text = "";
                        }
                        return;
                    }


                    foreach (var outsideEnemy in currentLevel.OutsideEnemies)
                    {
                        
                        if (outsideEnemy.enemyType.enemyName.ToLower().Contains(enteredText[1].ToLower()))
                        {
                            try
                            {
                                foundEnemy = true;
                                mls.LogInfo(outsideEnemy.enemyType.enemyName);

                                //random ai node index Random.Range(0, GameObject.FindGameObjectsWithTag("OutsideAINode").Length) - 1

                                mls.LogInfo("The index of " + outsideEnemy.enemyType.enemyName + " is " + currentLevel.OutsideEnemies.IndexOf(outsideEnemy));
                                //currentLevel.Enemies.IndexOf(enemy)
                                GameObject obj = UnityEngine.Object.Instantiate(currentLevel
                                                .OutsideEnemies[currentLevel.OutsideEnemies.IndexOf(outsideEnemy)]
                                                .enemyType.enemyPrefab, GameObject.FindGameObjectsWithTag("OutsideAINode")[Random.Range(0, GameObject.FindGameObjectsWithTag("OutsideAINode").Length) - 1].transform.position, Quaternion.Euler(Vector3.zero));




                                obj.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
                                mls.LogInfo("Spawned " + outsideEnemy.enemyType.enemyName);
                            }
                            catch(Exception e)
                            {
                                mls.LogInfo("Could not spawn enemy");
                                mls.LogInfo("The game tossed an error: " + e.Message);
                            }
                        }
                    }

                }

                if (HideCommandMessages.Value)
                {
                    __instance.chatTextField.text = "";
                }
                return;
            }
            if (text.ToLower().StartsWith("weather"))
            {
                string[] enteredText = text.Split(' ');
                if (enteredText.Length > 1)
                {
                    if (enteredText[1].ToLower().Contains("rain"))
                    {
                        currentRound.timeScript.currentLevelWeather = LevelWeatherType.Rainy;
                        mls.LogInfo("tried to change the weather to " + enteredText[1]);
                    }
                    if (enteredText[1].ToLower().Contains("eclipse"))
                    {
                        currentRound.timeScript.currentLevelWeather = LevelWeatherType.Eclipsed;
                        mls.LogInfo("tried to change the weather to " + enteredText[1]);
                    }
                    if (enteredText[1].ToLower().Contains("flood"))
                    {
                        currentRound.timeScript.currentLevelWeather = LevelWeatherType.Flooded;
                        mls.LogInfo("tried to change the weather to " + enteredText[1]);
                    }
                    if (enteredText[1].ToLower().Contains("dust") || enteredText[1].ToLower().Contains("fog") || enteredText[1].ToLower().Contains("mist"))
                    {
                        currentRound.timeScript.currentLevelWeather = LevelWeatherType.DustClouds;
                        mls.LogInfo("tried to change the weather to " + enteredText[1]);
                    }
                    if (enteredText[1].ToLower().Contains("storm"))
                    {
                        currentRound.timeScript.currentLevelWeather = LevelWeatherType.Stormy;
                        mls.LogInfo("tried to change the weather to " + enteredText[1]);
                    }
                    if (enteredText[1].ToLower().Contains("none"))
                    {
                        currentRound.timeScript.currentLevelWeather = LevelWeatherType.None;
                        mls.LogInfo("tried to change the weather to " + enteredText[1]);
                    }
                }
                if (HideCommandMessages.Value)
                {
                    __instance.chatTextField.text = "";
                }
                return;

            }


        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SpawnEnemyFromVent))]
        [HarmonyPrefix]
        static void logSpawnEnemyFromVent()
        {
            mls.LogInfo("Attempting to spawn an enemy");
        }
        
        [HarmonyPatch(typeof(RoundManager), "EnemyCannotBeSpawned")]
        [HarmonyPrefix]
        static bool OverrideCannotSpawn() 
        {
            return false;
        }

        [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.Start))]
        [HarmonyPrefix]
        static void IncreaseHaunt(ref float ___hauntInterval)
        {
            // this is called but haunting interval doesn't do as much as hoped
            // haunt interval is based on how soon will it go to the next person to haunt, instead of waiting a long time, we wait 2 seconds

            ___hauntInterval = 2f;
            if(HideEnemySpawnMessages.Value) { HUDManager.Instance.AddTextToChatOnServer("<color=red>Demon spawned.</color>"); }
            //HUDManager.Instance.AddTextToChatOnServer("<color=red>Demon spawned.</color>");
        }

        [HarmonyPatch(typeof(DressGirlAI), "BeginChasing")]
        [HarmonyPostfix]
        static void IncreaseChaseTimer(ref int ___currentBehaviourStateIndex, ref float ___chaseTimer)
        {
            HUDManager.Instance.AddTextToChatOnServer("<color=red>I'm chasing you.</color>");
            ___chaseTimer = 60f;
        }

        [HarmonyPatch(typeof(SpringManAI), nameof(SpringManAI.Update))]
        [HarmonyPrefix]
        static void IncreaseSpring(ref float ___currentChaseSpeed, ref float ___currentAnimSpeed)
        {
            ___currentChaseSpeed = SpringSpeed.Value;
            ___currentAnimSpeed = SpringAnimSpeed.Value;
            // instant hit, should not be able to survive if attacked
            //___timeSinceHittingPlayer = 0.1f;
        }

        [HarmonyPatch(typeof(JesterAI), "SetJesterInitialValues")]
        [HarmonyPostfix]
        static void JesterDangerous(ref float ___popUpTimer, ref float ___beginCrankingTimer, ref float ___noPlayersToChaseTimer, ref float ___maxAnimSpeed)
        {
            ___popUpTimer = PopUpTimer.Value;
            ___beginCrankingTimer = CrankingTimer.Value;
            ___noPlayersToChaseTimer = JesterResetTimer.Value;
            ___maxAnimSpeed = MaxJesterSpeed.Value;
            if (HideEnemySpawnMessages.Value) { HUDManager.Instance.AddTextToChatOnServer("<color=blue>Boing.</color>"); }
        }


        [HarmonyPatch(typeof(JesterAI), nameof(JesterAI.Update))]
        [HarmonyPrefix]
        static void RemoveRewind(ref float ___noPlayersToChaseTimer)
        {
            ___noPlayersToChaseTimer = JesterResetTimer.Value;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        static void InfiniteSprint(ref float ___sprintMeter)
        {
            if(EnableInfiniteSprint.Value) { Mathf.Clamp(___sprintMeter += 0.02f, 0f, 1f); }
            
        }

        [HarmonyPatch(typeof(CrawlerAI), nameof(CrawlerAI.HitEnemy))]
        [HarmonyPrefix]
        static void PatchThumperDeath()
        {
            //sets thumper to not take damage, it will not update or check health on hit
            return;
        }

        [HarmonyPatch(typeof(CrawlerAI), nameof(CrawlerAI.Start))]
        [HarmonyPrefix]
        static void ThumperSpeed(ref float ___agentSpeedWithNegative, ref float ___maxSearchAndRoamRadius)
        {
            ___maxSearchAndRoamRadius = 300f;
            if (HideEnemySpawnMessages.Value) { HUDManager.Instance.AddTextToChatOnServer("<color=red> >:) </color>"); }
        }

    }

        
}
