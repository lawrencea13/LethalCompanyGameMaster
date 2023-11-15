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
using static System.Net.Mime.MediaTypeNames;
using LethalCompanyTestMod.Component;
using Steamworks.Ugc;

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
        private static ConfigEntry<string> PrefixSetting;
        private static ConfigEntry<bool> ShouldEnemiesSpawnNaturally;
        private static ConfigEntry<float> SpringSpeed;
        //private static ConfigEntry<float> SpringAnimSpeed;
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
        internal static ConfigEntry<bool> EnableInfiniteSprint;
        private static ConfigEntry<bool> EnableInfiniteCredits;
        private static ConfigEntry<bool> EnableInfiniteDeadline;
        private static ConfigEntry<int> XPChange;
        private static ConfigEntry<bool> CustomCompanyBuyRate;
        private static ConfigEntry<bool> UseRandomBuyRate;
        private static ConfigEntry<float> MinimumCompanyBuyRate;
        private static ConfigEntry<float> MaximumCompanyBuyRate;

        private static ConfigEntry<bool> CustomTimeScale;
        private static ConfigEntry<bool> UseRandomTimeScale;
        private static ConfigEntry<float> MinimumTimeScale;
        private static ConfigEntry<float> MaximumTimeScale;

        private static ConfigEntry<bool> CustomDeadline;
        private static ConfigEntry<bool> UseRandomDeadline;
        private static ConfigEntry<int> MinimumDeadline;
        private static ConfigEntry<int> MaximumDeadline;


        // ONLY use with config manager
        private static ConfigEntry<bool> SpawnSelectedEnemy;
        private static ConfigEntry<string> SelectedEnemy;

        private static ConfigEntry<bool> cfgNightVision;
        private static ConfigEntry<bool> cfgSpeedHack;
        private static ConfigEntry<bool> cfgGodMode;

        private static ConfigEntry<bool> EnableAIModifiers;
        private static ConfigEntry<bool> EnableScrapModifiers;

        #endregion

        private static SelectableLevel currentLevel;
        private static EnemyVent[] currentLevelVents;
        private static RoundManager currentRound;

        // plan for more in the future
        private static SpawnableEnemyWithRarity jesterRef;

        internal static GUILoader myGUI;
        private static bool noClipEnabled;
        internal static bool enableGod;
        internal static bool nightVision;
        internal static PlayerControllerB playerRef;
        private static bool speedHack;
        internal static float nightVisionIntensity;
        internal static float nightVisionRange;
        internal static UnityEngine.Color nightVisionColor;

        private static bool hasGUISynced = false;
        internal static bool isHost;

        internal static TestMod Instance;

        void Awake()
        {
            Instance = this;
            mls = BepInEx.Logging.Logger.CreateLogSource("GameMaster");
            // Plugin startup logic
            mls.LogInfo($"Loaded {modGUID}. Patching.");
            harmony.PatchAll(typeof(TestMod));
            mls = Logger;
            enemyRaritys = new Dictionary<SpawnableEnemyWithRarity, int>();
            levelEnemySpawns = new Dictionary<SelectableLevel, List<SpawnableEnemyWithRarity>>();
            enemyPropCurves = new Dictionary<SpawnableEnemyWithRarity, AnimationCurve>();

            var gameObject = new UnityEngine.GameObject("GUILoader");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.AddComponent<GUILoader>();
            myGUI = (GUILoader)gameObject.GetComponent("GUILoader");
            SetBindings();
            setGUIVars();
            noClipEnabled = false;
            enableGod = false;

        }

        void setGUIVars()
        {
            // load from config on start

            // bools
            myGUI.guiHideCommandMessage = HideCommandMessages.Value;
            myGUI.guiHideEnemySpawnMessages = HideEnemySpawnMessages.Value;
            myGUI.guiShouldEnemiesSpawnNaturally = ShouldEnemiesSpawnNaturally.Value;
            myGUI.guiEnableAIModifiers = EnableAIModifiers.Value;
            myGUI.guiEnableInfiniteSprint = EnableInfiniteSprint.Value;
            myGUI.guiEnableInfiniteCredits = EnableInfiniteCredits.Value;
            myGUI.guiEnableGod = cfgGodMode.Value;
            myGUI.guiEnableNightVision = cfgNightVision.Value;
            myGUI.guiEnableSpeedHack = cfgSpeedHack.Value;
            myGUI.guiEnableScrapModifiers = EnableScrapModifiers.Value;
            myGUI.guiEnableCustomBuyRate = CustomCompanyBuyRate.Value;
            myGUI.guiUseRandomBuyRate = UseRandomBuyRate.Value;
            myGUI.guiUseCustomTimeScale = CustomTimeScale.Value;
            myGUI.guiUseRandomTimeScale = UseRandomTimeScale.Value;
            myGUI.guiEnableInfiniteDeadline = EnableInfiniteDeadline.Value;
            myGUI.guiEnableCustomDeadline = CustomDeadline.Value;
            myGUI.guiUseRandomDeadline = UseRandomDeadline.Value;

            // strings
            myGUI.guiServerName = ServerName.Value;
            myGUI.guiRoundPost = RoundPost.Value;
            myGUI.guiXPChange = XPChange.Value.ToString();
            myGUI.guiSelectedEnemy = SelectedEnemy.Value;
            myGUI.guiPrefix = PrefixSetting.Value;

            // ints
            myGUI.guiMinScrap = MinScrap.Value;
            myGUI.guiMaxScrap = MaxScrap.Value;
            myGUI.guiMinScrapValue = MinScrapValue.Value;
            myGUI.guiMaxScrapValue = MaxScrapValue.Value;
            myGUI.guiMinimumDeadline = MinimumDeadline.Value;
            myGUI.guiMaximumDeadline = MaximumDeadline.Value;

            // floats
            myGUI.guiSpringSpeed = SpringSpeed.Value;
            myGUI.guiPopupTimer = PopUpTimer.Value;
            myGUI.guiMaxJesterSpeed = MaxJesterSpeed.Value;
            myGUI.guiCrankingTimer = CrankingTimer.Value;
            myGUI.guiJesterResetTimer = JesterResetTimer.Value;
            myGUI.guiMinTimeScale = MinimumTimeScale.Value;
            myGUI.guiMaxTimeScale = MaximumTimeScale.Value;
            myGUI.guiMinBuyRate = MinimumCompanyBuyRate.Value;
            myGUI.guiMaxBuyRate = MaximumCompanyBuyRate.Value;
            hasGUISynced = true;
        }

        internal void UpdateCFGVarsFromGUI()
        {
            if(!hasGUISynced) { setGUIVars(); }
            // bools
            HideCommandMessages.Value = myGUI.guiHideCommandMessage;
            HideEnemySpawnMessages.Value = myGUI.guiHideEnemySpawnMessages;
            ShouldEnemiesSpawnNaturally.Value = myGUI.guiShouldEnemiesSpawnNaturally;
            EnableAIModifiers.Value = myGUI.guiEnableAIModifiers;
            EnableInfiniteSprint.Value = myGUI.guiEnableInfiniteSprint;
            EnableInfiniteCredits.Value = myGUI.guiEnableInfiniteCredits;
            cfgGodMode.Value = myGUI.guiEnableGod;
            cfgNightVision.Value = myGUI.guiEnableNightVision;
            cfgSpeedHack.Value = myGUI.guiEnableSpeedHack;
            EnableScrapModifiers.Value = myGUI.guiEnableScrapModifiers;
            CustomCompanyBuyRate.Value = myGUI.guiEnableCustomBuyRate;
            UseRandomBuyRate.Value = myGUI.guiUseRandomBuyRate;
            CustomTimeScale.Value = myGUI.guiUseCustomTimeScale;
            UseRandomTimeScale.Value = myGUI.guiUseRandomTimeScale;
            EnableInfiniteDeadline.Value = myGUI.guiEnableInfiniteDeadline;
            CustomDeadline.Value = myGUI.guiEnableCustomDeadline;
            UseRandomDeadline.Value = myGUI.guiUseRandomDeadline;

            // strings
            ServerName.Value = myGUI.guiServerName;
            RoundPost.Value = myGUI.guiRoundPost;
            // not accounting for dingbats putting strings in here...
            XPChange.Value = int.Parse(myGUI.guiXPChange);
            SelectedEnemy.Value = myGUI.guiSelectedEnemy;
            PrefixSetting.Value = myGUI.guiPrefix;

            // ints
            MinScrap.Value = myGUI.guiMinScrap;
            MaxScrap.Value = myGUI.guiMaxScrap;
            MinScrapValue.Value = myGUI.guiMinScrapValue;
            MaxScrapValue.Value = myGUI.guiMaxScrapValue;
            MinimumDeadline.Value = myGUI.guiMinimumDeadline;
            MaximumDeadline.Value = myGUI.guiMaximumDeadline;

            // floats
            SpringSpeed.Value = myGUI.guiSpringSpeed;
            PopUpTimer.Value = myGUI.guiPopupTimer;
            MaxJesterSpeed.Value = myGUI.guiMaxJesterSpeed;
            CrankingTimer.Value = myGUI.guiCrankingTimer;
            JesterResetTimer.Value = myGUI.guiJesterResetTimer;
            MinimumTimeScale.Value = myGUI.guiMinTimeScale;
            MaximumTimeScale.Value = myGUI.guiMaxTimeScale;
            MinimumCompanyBuyRate.Value = myGUI.guiMinBuyRate;
            MaximumCompanyBuyRate.Value = myGUI.guiMaxBuyRate;
        }

        void Update()
        {
            

            if(myGUI.guiSpawnButtonPressed)
            {
                SpawnEnemyWithConfigManager(myGUI.guiSelectedEnemy);
            }

        }



        private void SetBindings()
        {
            ServerName = Config.Bind("Server Settings", "Server Name", "<color=red>G</color><color=blue>a</color><color=yellow>m</color><color=green>e</color><color=orange>M</color><color=purple>a</color><color=red>s</color><color=blue>t</color><color=yellow>e</color><color=green>r</color>", "Set the server name when creating a server");
            RoundPost = Config.Bind("Server Settings", "Round Comment", "<color=white>Custom game by </color><color=blue>Poseidon</color>", "A message that the server sends every round");
            //DisableIfNotHost = Config.Bind("Server Settings", "Disable mods if not host", true, "If true, mods will not do anything if you are not the host of the lobby. Can be used to avoid conflicts with other mods.");

            HideCommandMessages = Config.Bind("Command Settings", "Hide Spawn Messages", true, "Should the server hide your commands? true will hide, false will show");
            HideEnemySpawnMessages = Config.Bind("Command Settings", "Hide Enemy Spawn Messages", true, "Should the server hide messages an enemy may send when it spawns? true will hide, false will show.");
            PrefixSetting = Config.Bind("Command Settings", "Command Prefix", "/", "An optional prefix for chat commands");

            ShouldEnemiesSpawnNaturally = Config.Bind("AI Settings", "Natural enemy spawn", false, "If true, enemies will spawn naturally. If false, enemies will spawn only when told to by this script.");
            SpringSpeed = Config.Bind("AI Settings", "Spring Head - Speed", 100f, new ConfigDescription("Base speed for springhead", new AcceptableValueRange<float>(0.1f, 150f)));
            //SpringAnimSpeed = Config.Bind("AI Settings", "Spring Head - Speed Multiplier", 4f, new ConfigDescription("A speed multiplier for the springhead", new AcceptableValueRange<float>(0.1f, 10f)));
            PopUpTimer = Config.Bind("AI Settings", "Jester - PopUp", 0.5f, new ConfigDescription("How long it takes the jester to popup", new AcceptableValueRange<float>(0.1f, 100f)));
            MaxJesterSpeed = Config.Bind("AI Settings", "Jester - Max Speed Multiplier", 5f, new ConfigDescription("The maximum speed the jester can go, multiplier", new AcceptableValueRange<float>(0.1f, 10f)));
            CrankingTimer = Config.Bind("AI Settings", "Jester - Cranking", 0.5f, new ConfigDescription("The time it takes the jester to begin cranking", new AcceptableValueRange<float>(0.1f, 100f)));
            JesterResetTimer = Config.Bind("AI Settings", "Jester - Reset", 5000f, new ConfigDescription("The time it takes for the jester to reset when no one is in the building", new AcceptableValueRange<float>(5f, 5000f)));
            EnableAIModifiers = Config.Bind("AI Settings", "Enable AI modifiers", true, "If enabled, the server will override the AI settings, if disabled, it will use default settings.");

            SpawnSelectedEnemy = Config.Bind("Enemy Spawning", "Spawn Selected enemy", false, "On click, this will spawn the selected enemy then get set back to false. Only to be used with configuration manager.");
            SelectedEnemy = Config.Bind("Enemy Spawning", "Name of Enemy", "Spring", "Enter the name of the enemy you'd like to spawn. Can be partial, use the github or web to find a list of names. Only to be used with configuration manager.");


            XPChange = Config.Bind("Host Settings", "XP Change", 25, "Sets the amount of XP to additionally gain or lose. Can be negative.");
            EnableInfiniteSprint = Config.Bind("Host Settings", "Enable infinite sprint", true, "If true, stamina will never deplete, if false, stamina will work as normal.");
            EnableInfiniteCredits = Config.Bind("Host Settings", "Enable infinite credits", true, "If true, credits will always revert back to the preset value.  If false, they will work as normal.");
            cfgGodMode = Config.Bind("Host Settings", "Enable God Mode", false, "If true, you cannot die. *note if you fall down into a pit you may not be able to get out. Disabling will not kill you immediately.");
            cfgNightVision = Config.Bind("Host Settings", "Enable Night Vision", false, "If true, you will be able to see in the dark");
            cfgSpeedHack = Config.Bind("Host Settings", "Enable Speed Hack", false, "If true, will enable the built in speed hack, it's very fast.");

            cfgGodMode.SettingChanged += godModeCFGChanged;
            cfgNightVision.SettingChanged += nightVisionCFGChanged;
            cfgSpeedHack.SettingChanged += speedHackCFGChanged;

            EnableScrapModifiers = Config.Bind("Scrap Settings", "Enable Scrap mofifiers", true, "If enabled, will use our scrap modifiers, if disabled, will use the game generated scrap settings");
            MinScrap = Config.Bind("Scrap Settings", "Minimum Scap", 20, "Set the minimum pieces of scrap in the level");
            MaxScrap = Config.Bind("Scrap Settings", "Maximum Scap", 45, "Set the maximum pieces of scrap in the level");
            MinScrapValue = Config.Bind("Scrap Settings", "Minimum Scap value", 3000, "Set the minimum value of scrap in the level");
            MaxScrapValue = Config.Bind("Scrap Settings", "Maximum Scap value", 8000, "Set the maximum value of scrap in the level");

            CustomCompanyBuyRate = Config.Bind("Company Scrap Buying Settings", "Override Company Buying Rate", true, "Recommended if using infinite deadline, overrides in game calculation of the company buy rate if true to use our method.");
            UseRandomBuyRate = Config.Bind("Company Scrap Buying Settings", "Make custom buying rate Random", true, "If true, will pick a random value in between your minimum and maximum buy rate every day. If false, will use your MAXIMUM buying rate settting.");
            MinimumCompanyBuyRate = Config.Bind("Company Scrap Buying Settings", "Minimum buying rate", 0.1f, new ConfigDescription("Minimum buy rate for random", new AcceptableValueRange<float>(-1f, 10f)));
            MaximumCompanyBuyRate = Config.Bind("Company Scrap Buying Settings", "Maximum buying rate", 1f, new ConfigDescription("Maximum buy rate for random, OR if random is off, the buy rate used every day", new AcceptableValueRange<float>(-1f, 10f)));

            CustomTimeScale = Config.Bind("Time Settings", "Override day speed", true, "If true, will use either random speed multiplier between min and max, or if random is turned off, the maximum");
            UseRandomTimeScale = Config.Bind("Time Settings", "Make day speed random", false, "If true, will pick a random multiplier between the minimum and maximum, if false, will use maximum");
            MinimumTimeScale = Config.Bind("Time Settings", "Minimum day speed", 0.1f, new ConfigDescription("Minimum speed for random day length", new AcceptableValueRange<float>(0.1f, 10f)));
            MaximumTimeScale = Config.Bind("Time Settings", "Maximum day speed", 1f, new ConfigDescription("Maximum speed for random day length, or the set value if not random", new AcceptableValueRange<float>(0.1f, 10f)));

            EnableInfiniteDeadline = Config.Bind("Deadline settings", "Enable infinite deadline", true, "If true, the deadline will never go down and will always stay at 8 days. If false, it will drop like normal. Overrides custom deadline.");
            CustomDeadline = Config.Bind("Deadline settings", "Override deadline", true, "If true, will use either a random deadline between the min and max, or if random is truned off, the maximum.");
            UseRandomDeadline = Config.Bind("Deadline settings", "Use random deadline", false, "If true, will use a randomy deadline between the minimum and maximum. If false, will use maximum.");
            MinimumDeadline = Config.Bind("Deadline settings", "Minimum deadline", 1, new ConfigDescription("Minimum amount of days for random deadline", new AcceptableValueRange<int>(1, 20)));
            MaximumDeadline = Config.Bind("Deadline settings", "Maximum deadline", 3, new ConfigDescription("Maximum amount of days for random deadline, or the set value if not random", new AcceptableValueRange<int>(1, 20)));



            
            

        }

        private void speedHackCFGChanged(object sender, EventArgs e)
        {
            if(!isHost) { return; }
            toggleSpeedHack();
        }

        private void nightVisionCFGChanged(object sender, EventArgs e)
        {
            if(!isHost)
            {
                return;
            }
            nightVision = cfgNightVision.Value;
        }

        private void godModeCFGChanged(object sender, EventArgs e)
        {
            if(!isHost) { return; }
            enableGod = cfgGodMode.Value;
        }

        public void EnableNoClip()
        {
            if (!isHost) { return; }
            noClipEnabled = !noClipEnabled;
            mls.LogInfo("noclip function called");

            if (noClipEnabled)
            {
                Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var collider in colliders)
                {
                    collider.enabled = false;
                }

            }
        }

        private static bool toggleNightVision()
        {
            if (isHost)
            {
                nightVision = !nightVision;
                cfgNightVision.Value = nightVision;
            }

            return nightVision;
        }

        private static bool toggleGodMode()
        {
            if(isHost)
            {
                enableGod = !enableGod;
                cfgGodMode.Value = enableGod;
            }

            return enableGod;
        }

        private static void toggleSpeedHack()
        {
            if(!isHost) { return; }
            speedHack = !playerRef.isSpeedCheating;
            playerRef.isSpeedCheating = speedHack;
            //cfgSpeedHack.Value = speedHack;
            //return speedHack;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPrefix]
        static void patchControllerUpdate()
        {
            myGUI.guiIsHost = isHost;
            Instance.UpdateCFGVarsFromGUI();
        }

        [HarmonyPatch(typeof(PlayerControllerB), "Start")]
        [HarmonyPrefix]
        static void getNightVision(ref PlayerControllerB __instance)
        {
            playerRef = __instance;
            nightVision = playerRef.nightVision.enabled;
            // store nightvision values
            nightVisionIntensity = playerRef.nightVision.intensity;
            nightVisionColor = playerRef.nightVision.color;
            nightVisionRange = playerRef.nightVision.range;

            playerRef.nightVision.color = UnityEngine.Color.green;
            playerRef.nightVision.intensity = 1000f;
            playerRef.nightVision.range = 10000f;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "SetNightVisionEnabled")]
        [HarmonyPostfix]
        static void updateNightVision()
        {
            //instead of enabling/disabling nightvision, set the variables

            if (nightVision)
            {
                playerRef.nightVision.color = UnityEngine.Color.green;
                playerRef.nightVision.intensity = 1000f;
                playerRef.nightVision.range = 10000f;
            }
            else
            {
                playerRef.nightVision.color = nightVisionColor;
                playerRef.nightVision.intensity = nightVisionIntensity;
                playerRef.nightVision.range = nightVisionRange;
            }

            // should always be on
            playerRef.nightVision.enabled = true;
        }


        [HarmonyPatch(typeof(PlayerControllerB), "AllowPlayerDeath")]
        [HarmonyPrefix]
        static bool OverrideDeath()
        {
            if (!isHost) { return true; }
            return !enableGod;
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.RunTerminalEvents))]
        [HarmonyPostfix]
        static void NeverLoseCredits(ref int ___groupCredits)
        {
            if (!isHost) { return; }
            if (EnableInfiniteCredits.Value) { ___groupCredits = 50000; }
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
            if (!isHost) { return; }
            if (EnableInfiniteDeadline.Value) { ___timeUntilDeadline = 10000; }

        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.SetBuyingRateForDay))]
        [HarmonyPostfix]
        static void FixScrapBuyingRate()
        {
            // check if custom buying rate is enabled
            // if not don't do anything
            // if so, setting for random OR set value
            if (!isHost) { return; }
            if (CustomCompanyBuyRate.Value)
            {
                // set value to max, if random is turned off, this will be the value we use
                float buyRate = MaximumCompanyBuyRate.Value;

                if (UseRandomBuyRate.Value)
                {
                    // overide the maximum if using random value
                    buyRate = Random.Range(MinimumCompanyBuyRate.Value, MaximumCompanyBuyRate.Value);
                }
                // set buy rate for day
                StartOfRound.Instance.companyBuyingRate = buyRate;
                return;
            }
        }

        
        [HarmonyPatch(typeof(StartOfRound), "Start")]
        [HarmonyPrefix]
        static bool patchDeadline()
        {
            if (isHost)
            {
                if (CustomDeadline.Value)
                {
                    TimeOfDay.Instance.quotaVariables.deadlineDaysAmount = MaximumDeadline.Value;

                    if (UseRandomDeadline.Value)
                    {
                        TimeOfDay.Instance.quotaVariables.deadlineDaysAmount = Random.Range(MinimumDeadline.Value, MaximumDeadline.Value);
                    }
                }
                return true;
            }

            return false;
            
        }

        [HarmonyPatch(typeof(TimeOfDay), "Start")]
        [HarmonyPostfix]
        static void customizableTimeScale(TimeOfDay __instance)
        {
            if (isHost)
            {
                if (CustomTimeScale.Value)
                {
                    __instance.globalTimeSpeedMultiplier = MaximumTimeScale.Value;

                    if (UseRandomTimeScale.Value)
                    {
                        __instance.globalTimeSpeedMultiplier = Random.Range(MinimumTimeScale.Value, MaximumTimeScale.Value);
                    }
                }
            }
        }


        [HarmonyPatch(typeof(HUDManager), "SetPlayerLevelSmoothly")]
        [HarmonyPrefix]
        static void OverrideXPGain(ref int XPGain)
        {
            // doesn't get called unless xp is actually had.
            // e.g. go in and immediately leaving will not call this function
            if (isHost) { XPGain += XPChange.Value; }
            
        }

        [HarmonyPatch(typeof(RoundManager), "Start")]
        [HarmonyPrefix]
        static void setIsHost()
        {
            mls.LogInfo("Host Status: " + RoundManager.Instance.NetworkManager.IsHost.ToString());
            isHost = RoundManager.Instance.NetworkManager.IsHost;
        }

        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.LoadNewLevel))]
        [HarmonyPrefix]
        static bool ModifyLevel(ref SelectableLevel newLevel)
        {
            // only called if you are host
            // avoid setting manually in case there is a missed path that executes even if not host
            //isHost = true;
            // doesn't need to be returned early as a result of above mentioned

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
                mls.LogInfo("Inside: " + enemy.enemyType.enemyName);
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
                mls.LogInfo("Outside: " + enemy.enemyType.enemyName);
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




            if (!ShouldEnemiesSpawnNaturally.Value)
            {
                bool foundJester = false;
                // remove rarity for all enemies
                foreach (var enemy in newLevel.Enemies)
                {
                    enemy.rarity = 0;
                    if (enemy.enemyType.enemyName.ToLower().Contains("jester"))
                    {
                        mls.LogInfo("Found a jester, yoinking that reference");
                        //todo add more enemies, likely utilizing a list that will ultimately result in a list of enemies that replaces the existing list the server uses
                        foundJester = true;
                        jesterRef = enemy;
                    }
                }
                if (!foundJester)
                {
                    if (jesterRef != null)
                    {
                        mls.LogInfo("Didn't find a jester, but we can use the ref");
                        newLevel.Enemies.Add(jesterRef);
                    }
                    else
                    {
                        mls.LogInfo("We couldn't add a jester to this level");
                    }
                }

                foreach (var enemy in newLevel.OutsideEnemies)
                {
                    enemy.rarity = 0;
                }
            }
            



            // create temporary version of level to modify
            SelectableLevel n = newLevel;


            // adjust scrap values
            // not necessarry for this particular mode
            if (EnableScrapModifiers.Value)
            {
                n.minScrap = MinScrap.Value;
                n.maxScrap = MaxScrap.Value;
                n.minTotalScrapValue = MinScrapValue.Value;
                n.maxTotalScrapValue = MaxScrapValue.Value;
            }


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
            // make prefix even if one doesn't exist
            string tempPrefix = "/";
            mls.LogInfo(text);

            if(PrefixSetting.Value != "")
            {
                // if prefix exists, use that instead
                tempPrefix = PrefixSetting.Value;
            }

            // check if prefix is utilized
            if(text.ToLower().StartsWith(tempPrefix.ToLower()))
            {
                string noticeTitle = "Default Title";
                string noticeBody = "Default Body";

                if(!isHost)
                {
                    noticeTitle = "Command";
                    noticeBody = "Unable to send command since you are not host.";
                    HUDManager.Instance.DisplayTip(noticeTitle, noticeBody);
                    if (HideCommandMessages.Value)
                    {
                        __instance.chatTextField.text = "";
                    }
                    return;
                }

                //works if host
                if (text.ToLower().StartsWith(tempPrefix + "spawn"))
                {
                    noticeTitle = "Spawned Enemies";
                    string[] enteredText = text.Split(' ');
                    if (enteredText.Length == 2)
                    {
                        bool foundEnemy = false;
                        string foundEnemyName = "";
                        foreach (var enemy in currentLevel.Enemies)
                        {

                            if (enemy.enemyType.enemyName.ToLower().Contains(enteredText[1].ToLower()))
                            {
                                try
                                {
                                    foundEnemy = true;
                                    foundEnemyName = enemy.enemyType.enemyName;
                                    SpawnEnemy(enemy, 1, true);
                                    mls.LogInfo("Spawned " + enemy.enemyType.enemyName);
                                }
                                catch
                                {
                                    mls.LogInfo("Could not spawn enemy");
                                }
                                noticeBody = "Spawned: " + foundEnemyName;
                                break;
                            }
                        }
                        if (!foundEnemy)
                        {
                            foreach (var outsideEnemy in currentLevel.OutsideEnemies)
                            {

                                if (outsideEnemy.enemyType.enemyName.ToLower().Contains(enteredText[1].ToLower()))
                                {
                                    try
                                    {
                                        foundEnemy = true;
                                        foundEnemyName = outsideEnemy.enemyType.enemyName;
                                        mls.LogInfo(outsideEnemy.enemyType.enemyName);

                                        //random ai node index Random.Range(0, GameObject.FindGameObjectsWithTag("OutsideAINode").Length) - 1

                                        mls.LogInfo("The index of " + outsideEnemy.enemyType.enemyName + " is " + currentLevel.OutsideEnemies.IndexOf(outsideEnemy));

                                        SpawnEnemy(outsideEnemy, 1, false);


                                        mls.LogInfo("Spawned " + outsideEnemy.enemyType.enemyName);
                                    }
                                    catch (Exception e)
                                    {
                                        mls.LogInfo("Could not spawn enemy");
                                        mls.LogInfo("The game tossed an error: " + e.Message);
                                    }
                                    noticeBody = "Spawned: " + foundEnemyName;
                                    break;
                                }
                            }

                        }
                        
                       
                    }

                    
                    if (enteredText.Length > 2)
                    {
                        bool foundEnemyMulti = false;
                        if (int.TryParse(enteredText[2], out int amountToSpawn))
                        {

                            string foundEnemyNameMulti = "";
                            foreach (var enemy in currentLevel.Enemies)
                            {
                                if (enemy.enemyType.enemyName.ToLower().Contains(enteredText[1].ToLower()))
                                {
                                    foundEnemyMulti = true;
                                    foundEnemyNameMulti = enemy.enemyType.enemyName;

                                    SpawnEnemy(enemy, amountToSpawn, true);

                                    if (foundEnemyMulti)
                                    {
                                        noticeBody = "Spawned " + amountToSpawn + " " + foundEnemyNameMulti + "s";
                                        break;
                                    }
                                }
                            }

                            if (!foundEnemyMulti)
                            {
                                foreach (var outsideEnemy in currentLevel.OutsideEnemies)
                                {
                                    if (outsideEnemy.enemyType.enemyName.ToLower().Contains(enteredText[1].ToLower()))
                                    {
                                        foundEnemyMulti = true;
                                        foundEnemyNameMulti = outsideEnemy.enemyType.enemyName;
                                        try
                                        {

                                            mls.LogInfo("The index of " + outsideEnemy.enemyType.enemyName + " is " + currentLevel.OutsideEnemies.IndexOf(outsideEnemy));

                                            SpawnEnemy(outsideEnemy, amountToSpawn, false);


                                            mls.LogInfo("Spawned another " + outsideEnemy.enemyType.enemyName);

                                        }
                                        catch
                                        {
                                            mls.LogInfo("Failed to spawn enemies, check your command.");
                                        }
                                        if (foundEnemyMulti)
                                        {
                                            noticeBody = "Spawned " + amountToSpawn + " " + foundEnemyNameMulti + "s";
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            mls.LogInfo("Failed to spawn enemies, check your command.");
                        }

                        mls.LogInfo("Length of input array: " + enteredText.Length);
                    }
                    //mls.LogInfo("Got your message, trying to find the enemy");
                    // spawn 1 enemy
                }
                // client only
                if (text.ToLower().StartsWith(tempPrefix + "weather"))
                {
                    noticeTitle = "Weather Change";
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
                        noticeBody = "tried to change the weather to " + enteredText[1];
                    }
                }
                // works if host
                if (text.ToLower().StartsWith(tempPrefix + "togglelights"))
                {
                    BreakerBox breakerBox = UnityEngine.Object.FindObjectOfType<BreakerBox>();
                    if (breakerBox != null)
                    {
                        noticeTitle = "Light Change";

                        if (breakerBox.isPowerOn)
                        {
                            currentRound.TurnBreakerSwitchesOff();
                            currentRound.TurnOnAllLights(false);
                            breakerBox.isPowerOn = false;
                            noticeBody = "Turned the lights off";
                        }
                        else
                        {
                            //currentRound.TurnBreakerSwitchesOn();

                            currentRound.PowerSwitchOnClientRpc();
                            //breakerBox.isPowerOn = true;
                            noticeBody = "Turned the lights on";
                        }
                    }
                }
                //buy items
                if (text.ToLower().StartsWith(tempPrefix + "buy"))
                {
                    noticeTitle = "Item Buying";
                    Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
                    if (terminal != null)
                    {
                        // put pro first to avoid issues, if you want a normal flashlight, you must call normal otherwise pro will take precedent
                        List<string> itemList = new List<string>
                    {
                        "Walkie-Talkie",
                        "Pro Flashlight",
                        "Normal Flashlight",
                        "Shovel",
                        "Lockpicker",
                        "Stun Grenade",
                        "Boom Box",
                        "Inhaler",
                        "Stun Gun",
                        "Jet Pack",
                        "Extension Ladder",
                        "Radar Booster" 
                    };

                    Dictionary<string, int> itemID = new Dictionary<string, int>
                    {
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
                    };

                        string[] enteredText = text.Split(' ');
                        if (enteredText.Length > 1)
                        {
                            bool foundItemMulti = false;
                            if (enteredText.Length > 2)
                            {
                                // parse if buying multiple items
                                if (int.TryParse(enteredText[2], out int c))
                                {
                                    
                                    foreach (string item in itemList)
                                    {
                                        if (item.ToLower().Contains(enteredText[1]))
                                        {
                                            foundItemMulti = true;
                                            List<int> a = new List<int>();
                                            for (int i = 0; i < c; i++)
                                            {
                                                a.Add(itemID[item]);
                                            }
                                            mls.LogInfo(a.Count());
                                            terminal.BuyItemsServerRpc(a.ToArray(), terminal.groupCredits, 0);
                                            noticeBody = "Bought " + c + " " + item + "s";
                                            break;
                                        }
                                    }
                                    if (!foundItemMulti)
                                    {
                                        mls.LogInfo("Couldn't figure out what [ " + enteredText[1] + " ] was.");
                                        if (HideCommandMessages.Value)
                                        {
                                            __instance.chatTextField.text = "";
                                        }
                                        return;
                                    }
                                }
                                else
                                {
                                    mls.LogInfo("Couldn't parse command [ " + enteredText[2] + " ]");
                                    if (HideCommandMessages.Value)
                                    {
                                        __instance.chatTextField.text = "";
                                    }
                                    return;
                                }
                            }

                            if(!foundItemMulti)
                            {
                                bool foundItem = false;
                                // parse if buying 1 item by name
                                foreach (string item in itemList)
                                {
                                    if (item.ToLower().Contains(enteredText[1]))
                                    {
                                        foundItem = true;
                                        int[] a = { itemID[item] };
                                        terminal.BuyItemsServerRpc(a, terminal.groupCredits, 0);
                                        noticeBody = "Bought " + 1 + " " + item;
                                    }
                                }
                                if (!foundItem) { mls.LogInfo("Couldn't figure out what [ " + enteredText[1] + " ] was. Trying via int parser."); }

                                // parse if buying by index
                                if (int.TryParse(enteredText[1], out int b))
                                {
                                    int[] a = { b };
                                    terminal.BuyItemsServerRpc(a, terminal.groupCredits, 0);

                                }
                                else
                                {
                                    mls.LogInfo("Couldn't figure out what [ " + enteredText[1] + " ] was. Int parser failed, please try again.");
                                    if (HideCommandMessages.Value)
                                    {
                                        __instance.chatTextField.text = "";
                                    }
                                    return;
                                }
                                noticeBody = "Bought item with ID [" + b.ToString() + "]";
                            }
                        }
                    }
                }
                // toggle godmode works if server or client
                if (text.ToLower().Contains("god"))
                {
                    cfgGodMode.Value = !cfgGodMode.Value;
                    hasGUISynced = false;
                    noticeTitle = "God Mode";
                    noticeBody = "God Mode set to: " + enableGod.ToString();
                }
                // toggle night vision works if server or client
                if(text.ToLower().Contains("night") || text.ToLower().Contains("vision"))
                {
                    if (toggleNightVision())
                    {
                        noticeBody = "Enabled Night Vision";
                    }
                    else
                    {
                        noticeBody = "Disabled Night Vision";
                    }
                    noticeTitle = "Night Vision";
                    hasGUISynced = false;
                }
                // toggle speedhack works if server
                //Some how breaks the game, not sure why. Just gonna get rid of the command cfg only
                if (text.ToLower().Contains("speed"))
                {
                    cfgSpeedHack.Value = !cfgSpeedHack.Value;
                    hasGUISynced = false;

                    noticeBody = "Speed hack set to: " + speedHack.ToString();

                    noticeTitle = "Speed hack";
                }
                

                // sends notice to user about what they have done
                HUDManager.Instance.DisplayTip(noticeTitle, noticeBody);

                // ensures value is hidden if set but path doesn't hide it
                if (HideCommandMessages.Value)
                {
                    __instance.chatTextField.text = "";
                }
                return;
            }
        }


        private static void SpawnEnemyWithConfigManager(string enemyName)
        {
            if (!isHost) { return; }
            mls.LogInfo("CFGMGR tried to spawn an enemy");
            bool foundEnemy = false;
            string foundEnemyName = "";
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

                if (enemy.enemyType.enemyName.ToLower().Contains(enemyName.ToLower()))
                {
                    try
                    {
                        foundEnemy = true;
                        foundEnemyName = enemy.enemyType.enemyName;
                        SpawnEnemy(enemy, 1, true);
                        mls.LogInfo("Spawned " + enemy.enemyType.enemyName);
                    }
                    catch
                    {
                        mls.LogInfo("Could not spawn enemy");
                    }
                    break;
                }
            }
            if (!foundEnemy)
            {
                foreach (var outsideEnemy in currentLevel.OutsideEnemies)
                {

                    if (outsideEnemy.enemyType.enemyName.ToLower().Contains(enemyName.ToLower()))
                    {
                        try
                        {
                            foundEnemy = true;
                            foundEnemyName = outsideEnemy.enemyType.enemyName;
                            mls.LogInfo(outsideEnemy.enemyType.enemyName);

                            //random ai node index Random.Range(0, GameObject.FindGameObjectsWithTag("OutsideAINode").Length) - 1

                            mls.LogInfo("The index of " + outsideEnemy.enemyType.enemyName + " is " + currentLevel.OutsideEnemies.IndexOf(outsideEnemy));

                            SpawnEnemy(outsideEnemy, 1, false);


                            mls.LogInfo("Spawned " + outsideEnemy.enemyType.enemyName);
                        }
                        catch (Exception e)
                        {
                            mls.LogInfo("Could not spawn enemy");
                            mls.LogInfo("The game tossed an error: " + e.Message);
                        }
                        break;
                    }
                }

            }
        }

        private static void SpawnEnemy(SpawnableEnemyWithRarity enemy, int amount, bool inside)
        {
            // doesn't work regardless if not host but just in case
            if (!isHost) { return; }
            mls.LogInfo("Got to the main SpawnEnemy function");
            if (inside)
            {
                try
                {
                    for (int i = 0; i < amount; i++)
                    {
                        currentRound.SpawnEnemyOnServer(currentRound.allEnemyVents[Random.Range(0, currentRound.allEnemyVents.Length)].floorNode.position, currentRound.allEnemyVents[i].floorNode.eulerAngles.y, currentLevel.Enemies.IndexOf(enemy));
                    }
                }
                catch
                {
                    mls.LogInfo("Failed to spawn enemies, check your command.");
                }
            }
            else
            {
                for (int i = 0; i < amount; i++)
                {
                    mls.LogInfo("Spawned an enemy. Total Spawned: " + i.ToString());
                    GameObject obj = UnityEngine.Object.Instantiate(currentLevel
                                    .OutsideEnemies[currentLevel.OutsideEnemies.IndexOf(enemy)]
                                    .enemyType.enemyPrefab, GameObject.FindGameObjectsWithTag("OutsideAINode")[Random.Range(0, GameObject.FindGameObjectsWithTag("OutsideAINode").Length - 1)].transform.position, Quaternion.Euler(Vector3.zero));
                    obj.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
                }

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
            // ignored if not host, no need to check
            return false;
        }

        [HarmonyPatch(typeof(DressGirlAI), nameof(DressGirlAI.Start))]
        [HarmonyPrefix]
        static void IncreaseHaunt(ref float ___hauntInterval)
        {
            // this is called but haunting interval doesn't do as much as hoped
            // haunt interval is based on how soon will it go to the next person to haunt, instead of waiting a long time, we wait 2 seconds
            if (isHost)
            {
                if (EnableAIModifiers.Value)
                {
                    ___hauntInterval = 2f;
                    if (!HideEnemySpawnMessages.Value) { HUDManager.Instance.AddTextToChatOnServer("<color=red>Demon spawned.</color>"); }
                }
            }


            //HUDManager.Instance.AddTextToChatOnServer("<color=red>Demon spawned.</color>");

        }

        [HarmonyPatch(typeof(DressGirlAI), "BeginChasing")]
        [HarmonyPostfix]
        static void IncreaseChaseTimer(ref int ___currentBehaviourStateIndex, ref float ___chaseTimer)
        {
            if(isHost) { if (EnableAIModifiers.Value) { ___chaseTimer = 60f; } }

        }

        [HarmonyPatch(typeof(SpringManAI), nameof(SpringManAI.Update))]
        [HarmonyPrefix]
        static void IncreaseSpring(ref float ___currentChaseSpeed, ref float ___currentAnimSpeed)
        {
            if(isHost && !EnableAIModifiers.Value)
            {
                ___currentChaseSpeed = SpringSpeed.Value;
            }
        }

        [HarmonyPatch(typeof(JesterAI), "SetJesterInitialValues")]
        [HarmonyPostfix]
        static void JesterDangerous(ref float ___popUpTimer, ref float ___beginCrankingTimer, ref float ___noPlayersToChaseTimer, ref float ___maxAnimSpeed)
        {
            if (EnableAIModifiers.Value && isHost)
            {
                ___popUpTimer = PopUpTimer.Value;
                ___beginCrankingTimer = CrankingTimer.Value;
                ___noPlayersToChaseTimer = JesterResetTimer.Value;
                ___maxAnimSpeed = MaxJesterSpeed.Value;
                if (!HideEnemySpawnMessages.Value) { HUDManager.Instance.AddTextToChatOnServer("<color=blue>Boing.</color>"); }
            }

        }


        [HarmonyPatch(typeof(JesterAI), nameof(JesterAI.Update))]
        [HarmonyPrefix]
        static void RemoveRewind(ref float ___noPlayersToChaseTimer)
        {
            if (EnableAIModifiers.Value && isHost) { ___noPlayersToChaseTimer = JesterResetTimer.Value; }
            

        }

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        static void InfiniteSprint(ref float ___sprintMeter)
        {
            
            if (EnableInfiniteSprint.Value && isHost) { Mathf.Clamp(___sprintMeter += 0.02f, 0f, 1f); }
        }

        [HarmonyPatch(typeof(CrawlerAI), nameof(CrawlerAI.HitEnemy))]
        [HarmonyPrefix]
        static void PatchThumperDeath()
        {
            //if (isHost) { return; }
            //sets thumper to not take damage, it will not update or check health on hit
            // removed implementation temporarily
        }

        [HarmonyPatch(typeof(CrawlerAI), nameof(CrawlerAI.Start))]
        [HarmonyPrefix]
        static void ThumperSpeed(ref float ___agentSpeedWithNegative, ref float ___maxSearchAndRoamRadius)
        {
            if (EnableAIModifiers.Value && isHost) {
                ___maxSearchAndRoamRadius = 300f;
                if (!HideEnemySpawnMessages.Value) { HUDManager.Instance.AddTextToChatOnServer("<color=red> >:) </color>"); }
            }

        }

    }
        
}
