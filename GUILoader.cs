using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Windows;

namespace LethalCompanyTestMod.Component
{
    internal class GUILoader : MonoBehaviour
    {
        private KeyboardShortcut openCloseMenu;
        private bool isMenuOpen;
        public bool noClipButton;

        internal bool wasKeyDown;

        private int toolbarInt = 0;
        private string[] toolbarStrings = { "AI Modification/Spawning", "Host Settings", "Time Settings", "Scrap Settings" };

        private int MENUWIDTH = 600;
        private int MENUHEIGHT = 800;
        private int MENUX;
        private int MENUY;
        private int ITEMWIDTH = 300;
        private int CENTERX;

        #region Public Values
        // all vars I'd like my "backend" to access
        public bool guiHideCommandMessage;
        public bool guiHideEnemySpawnMessages;
        public bool guiShouldEnemiesSpawnNaturally;
        public bool guiEnableAIModifiers;
        public bool guiEnableInfiniteSprint;
        public bool guiEnableInfiniteCredits;
        public bool guiEnableGod;
        public bool guiEnableNightVision;
        public bool guiEnableSpeedHack;
        public bool guiEnableScrapModifiers;
        public bool guiEnableCustomBuyRate;
        public bool guiUseRandomBuyRate;
        public bool guiUseCustomTimeScale;
        public bool guiUseRandomTimeScale;
        public bool guiEnableInfiniteDeadline;
        public bool guiEnableCustomDeadline;
        public bool guiUseRandomDeadline;

        // only used to determine if button is down
        public bool guiSpawnButtonPressed;
        

        public string guiServerName;
        public string guiRoundPost;
        public string guiXPChange;
        public string guiSelectedEnemy;
        public string guiPrefix;

        public int guiMinScrap;
        public int guiMaxScrap;
        public int guiMinScrapValue;
        public int guiMaxScrapValue;
        public int guiMinimumDeadline;
        public int guiMaximumDeadline;

        public float guiSpringSpeed;
        public float guiPopupTimer;
        public float guiMaxJesterSpeed;
        public float guiCrankingTimer;
        public float guiJesterResetTimer;
        public float guiMinTimeScale;
        public float guiMaxTimeScale;
        public float guiMinBuyRate;
        public float guiMaxBuyRate;

        #endregion

        private GUIStyle menuStyle;
        private GUIStyle buttonStyle;
        private GUIStyle labelStyle;
        private GUIStyle toggleStyle;
        private GUIStyle hScrollStyle;

        public bool guiIsHost;

        private void Awake()
        {
            TestMod.mls.LogInfo("GUILoader loaded.");      
            openCloseMenu = new KeyboardShortcut(KeyCode.Insert);
            isMenuOpen = false;
            // this isn't pygame.. only need the screenwidth and height
            MENUX = (Screen.width / 2); //- (MENUWIDTH / 2);
            MENUY = (Screen.height / 2); // - (MENUHEIGHT / 2);
            CENTERX = MENUX + ((MENUWIDTH / 2) - (ITEMWIDTH / 2));

        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void intitializeMenu()
        {
            if (menuStyle == null)
            {
                menuStyle = new GUIStyle(GUI.skin.box);
                buttonStyle = new GUIStyle(GUI.skin.button);
                labelStyle = new GUIStyle(GUI.skin.label);
                toggleStyle = new GUIStyle(GUI.skin.toggle);
                hScrollStyle = new GUIStyle(GUI.skin.horizontalSlider);

                menuStyle.normal.textColor = Color.white;
                menuStyle.normal.background = MakeTex(2, 2, new Color(0.01f, 0.01f, 0.1f, .9f));
                menuStyle.fontSize = 18;
                menuStyle.normal.background.hideFlags = HideFlags.HideAndDontSave;

                buttonStyle.normal.textColor = Color.white;
                buttonStyle.fontSize = 18;

                labelStyle.normal.textColor = Color.white;
                labelStyle.normal.background = MakeTex(2, 2, new Color(0.01f, 0.01f, 0.1f, .9f));
                labelStyle.fontSize = 18;
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.normal.background.hideFlags = HideFlags.HideAndDontSave;

                toggleStyle.normal.textColor = Color.white;
                toggleStyle.fontSize = 18;

                hScrollStyle.normal.textColor = Color.white;
                hScrollStyle.normal.background = MakeTex(2, 2, new Color(0.0f, 0.0f, 0.2f, .9f));
                hScrollStyle.normal.background.hideFlags = HideFlags.HideAndDontSave;

            }
        }


        public void OnDestroy()
        {
            TestMod.mls.LogInfo("The GUILoader was destroyed :(");
        }



        public void Update()
        {

            // Much better than onPressed
            // removes jitter, ensures menu always toggles when key is released
            if (openCloseMenu.IsDown())
            {
                if (!wasKeyDown)
                {
                    wasKeyDown = true;
                }
            }
            if(openCloseMenu.IsUp())
            {
                if (wasKeyDown)
                {
                    wasKeyDown = false;
                    isMenuOpen = !isMenuOpen;
                    if (isMenuOpen)
                    {
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.Confined;
                    }
                    else
                    {
                        Cursor.visible = false;
                        //Cursor.lockState = CursorLockMode.Locked;
                    }
                }
            }


        }

        public void OnGUI()
        {
            // oddly enough doesn't work here
            if (!guiIsHost) { return; }
            if (menuStyle == null) { intitializeMenu(); }

            if (isMenuOpen)
            {
                GUI.Box(new Rect(MENUX, MENUY, MENUWIDTH, MENUHEIGHT), "GameMaster", menuStyle);

                toolbarInt = GUI.Toolbar(new Rect(MENUX, MENUY - 30, MENUWIDTH, 30), toolbarInt, toolbarStrings, buttonStyle);

                switch(toolbarInt)
                {
                    case 0:
                        // level modification
                        // includes enemy spawning, AI modifiers, scrap modifiers
                        /*
                         * List of needs:
                         * Textbox or dropdown list of enemies
                         * Enemy spawn button
                         * Togglebox for natural spawning
                         * hslider for spring speed
                         * hslider for popuptimer
                         * hslider for jesterspeed
                         * hslider for crankingtimer
                         * hslider for jesterresettimer
                         * toggle box to enable/disable ai modifications
                         * 
                         */
                        guiSelectedEnemy = GUI.TextField(new Rect(MENUX + ((MENUWIDTH / 2) - ITEMWIDTH), MENUY + 30, ITEMWIDTH, 30), guiSelectedEnemy);
                        guiSpawnButtonPressed = GUI.Button(new Rect(MENUX + (MENUWIDTH / 2), MENUY + 30, ITEMWIDTH, 30), "Spawn Enemy", buttonStyle);
                        
                        // appears I'll need a label for these
                        // spring speed
                        guiSpringSpeed = GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 160, ITEMWIDTH, 30), guiSpringSpeed, 0.1f, 150.0f);
                        GUI.Label(new Rect(CENTERX, MENUY + 130, ITEMWIDTH, 30), "Speed of the CoilHead " + guiSpringSpeed.ToString(), labelStyle);
                        // popup timer
                        GUI.Label(new Rect(CENTERX, MENUY + 200, ITEMWIDTH, 30), "Jester Popup speed " + guiPopupTimer.ToString(), labelStyle);
                        guiPopupTimer = GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 230, ITEMWIDTH, 30), guiPopupTimer, 0.1f, 100.0f);
                        // cranking timer
                        GUI.Label(new Rect(CENTERX, MENUY + 270, ITEMWIDTH, 30), "Jester cranking speed " + guiCrankingTimer.ToString(), labelStyle);
                        guiCrankingTimer = GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 300, ITEMWIDTH, 30), guiCrankingTimer, 0.1f, 10.0f);
                        // reset timer
                        GUI.Label(new Rect(CENTERX, MENUY + 340, ITEMWIDTH, 30), "Jester reset speed " + guiJesterResetTimer.ToString(), labelStyle);
                        guiJesterResetTimer = GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 370, ITEMWIDTH, 30), guiJesterResetTimer, 5f, 5000f);

                        guiEnableAIModifiers = GUI.Toggle(new Rect(CENTERX, MENUY + 410, ITEMWIDTH, 30), guiEnableAIModifiers, "Enable AI Modifications", toggleStyle);
                        guiShouldEnemiesSpawnNaturally = GUI.Toggle(new Rect(CENTERX, MENUY + 440, ITEMWIDTH, 30), guiShouldEnemiesSpawnNaturally, "Should Enemies Spawn Naturally", toggleStyle);
                        break;
                    case 1:
                        // host settings
                        // includes all local controls e.g. godmode, nightvision, etc., servername and roundpost, command settings
                        /*
                         * List of needs
                         * textbox for server namer
                         * textbox for round post
                         * togglebox for hide command message
                         * togglebox for hide enemyspawn messages
                         * textbox for prefix
                         * text box for xpchange
                         * check box for infinite sprint
                         * check box for infinite credits
                         * checkbox for godmode
                         * checkbox for nightvision
                         * checkbox for speedhack
                         * 
                         */
                        GUI.Label(new Rect(CENTERX, MENUY + 30, ITEMWIDTH, 30), "Server Name", labelStyle);
                        guiServerName = GUI.TextField(new Rect(CENTERX, MENUY + 60, ITEMWIDTH, 30), guiServerName);

                        GUI.Label(new Rect(CENTERX, MENUY + 100, ITEMWIDTH, 30), "Server Message", labelStyle);
                        guiRoundPost = GUI.TextField(new Rect(CENTERX, MENUY + 130, ITEMWIDTH, 30), guiRoundPost);


                        guiHideCommandMessage = GUI.Toggle(new Rect(CENTERX, MENUY + 170, ITEMWIDTH, 30), guiHideCommandMessage, "Hide Command Messages", toggleStyle);
                        guiHideEnemySpawnMessages = GUI.Toggle(new Rect(CENTERX, MENUY + 200, ITEMWIDTH, 30), guiHideEnemySpawnMessages, "Hide Enemy Spawn Messages", toggleStyle);


                        GUI.Label(new Rect(CENTERX, MENUY + 230, ITEMWIDTH, 30), "Command Prefix", labelStyle);
                        guiPrefix = GUI.TextField(new Rect(CENTERX, MENUY + 260, ITEMWIDTH, 30), guiPrefix);

                        GUI.Label(new Rect(CENTERX, MENUY + 300, ITEMWIDTH, 30), "XP Change", labelStyle);
                        guiXPChange = GUI.TextField(new Rect(CENTERX, MENUY + 330, ITEMWIDTH, 30), guiXPChange);

                        guiEnableInfiniteSprint = GUI.Toggle(new Rect(CENTERX, MENUY + 360, ITEMWIDTH, 30), guiEnableInfiniteSprint, "Infinite Sprint", toggleStyle);
                        guiEnableInfiniteCredits = GUI.Toggle(new Rect(CENTERX, MENUY + 390, ITEMWIDTH, 30), guiEnableInfiniteCredits, "Infinite Credits", toggleStyle);
                        guiEnableGod = GUI.Toggle(new Rect(CENTERX, MENUY + 420, ITEMWIDTH, 30), guiEnableGod, "God Mode", toggleStyle);
                        guiEnableNightVision = GUI.Toggle(new Rect(CENTERX, MENUY + 450, ITEMWIDTH, 30), guiEnableNightVision, "NightVision", toggleStyle);
                        guiEnableSpeedHack = GUI.Toggle(new Rect(CENTERX, MENUY + 480, ITEMWIDTH, 30), guiEnableSpeedHack, "SpeedHack", toggleStyle);
                        break;
                    case 2:
                        //Time settings
                        // deadline, timescale, buyrate
                        /*
                         * List of needs:
                         * checkbox for infinite deadline
                         * checkbox for enable custom deadline
                         * checkbox for random deadline
                         * hslider for minimum deadline
                         * hslider for maximum deadline
                         * 
                         * checkbox for custom timescale
                         * checkbox for random timescale
                         * hslider for min timescale
                         * hslider for max timescale
                         * 
                         */
                        guiEnableInfiniteDeadline = GUI.Toggle(new Rect(CENTERX, MENUY + 30, ITEMWIDTH + 100, 30), guiEnableInfiniteDeadline, "Infinite Deadline(overrides all deadline settings)", toggleStyle);
                        guiEnableCustomDeadline = GUI.Toggle(new Rect(CENTERX, MENUY + 60, ITEMWIDTH, 30), guiEnableCustomDeadline, "Custom Deadline", toggleStyle);
                        guiUseRandomDeadline = GUI.Toggle(new Rect(CENTERX, MENUY + 90, ITEMWIDTH, 30), guiUseRandomDeadline, "Random Deadline", toggleStyle);
                        // minimum deadline
                        GUI.Label(new Rect(CENTERX, MENUY + 130, ITEMWIDTH, 30), "Minimum Deadline " + guiMinimumDeadline.ToString(), labelStyle);
                        guiMinimumDeadline = (int)GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 160, ITEMWIDTH, 30), guiMinimumDeadline, 1, 20);
                        // maximum deadline
                        GUI.Label(new Rect(CENTERX, MENUY + 200, ITEMWIDTH, 30), "Maximum Deadline " + guiMaximumDeadline.ToString(), labelStyle);
                        guiMaximumDeadline = (int)GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 230, ITEMWIDTH, 30), guiMaximumDeadline, 1, 20);

                        guiUseCustomTimeScale = GUI.Toggle(new Rect(CENTERX, MENUY + 270, ITEMWIDTH, 30), guiUseCustomTimeScale, "Custom Timescale", toggleStyle);
                        guiUseRandomTimeScale = GUI.Toggle(new Rect(CENTERX, MENUY + 300, ITEMWIDTH, 30), guiUseRandomTimeScale, "Random Timescale", toggleStyle);
                        // minimum timescale
                        GUI.Label(new Rect(CENTERX, MENUY + 330, ITEMWIDTH, 30), "Minimum Timescale " + guiMinTimeScale.ToString(), labelStyle);
                        guiMinTimeScale = GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 360, ITEMWIDTH, 30), guiMinTimeScale, 0.1f, 10.0f);
                        // maximum timescale
                        GUI.Label(new Rect(CENTERX, MENUY + 400, ITEMWIDTH, 30), "Maximum Timescale " + guiMaxTimeScale.ToString(), labelStyle);
                        guiMaxTimeScale = GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 430, ITEMWIDTH, 30), guiMaxTimeScale, 0.1f, 10.0f);
                        break;
                    case 3:
                        //scrap settings
                        // scrap in level, scrap sell value
                        /*
                         * List of needs:
                         * checkbox for custom buyrate
                         * checkbox for random buyrate
                         * hslider for min buyrate
                         * hslider for max buyrate
                         * 
                         * checkbox for scrap modification
                         * hslider for min scrap
                         * hslider for max scrap
                         * hslider for min scrap value
                         * hslider for max scrap value
                         * 
                         */

                        guiEnableCustomBuyRate = GUI.Toggle(new Rect(CENTERX, MENUY + 30, ITEMWIDTH, 30), guiEnableCustomBuyRate, "Custom Buyrate", toggleStyle);
                        guiUseRandomBuyRate = GUI.Toggle(new Rect(CENTERX, MENUY + 60, ITEMWIDTH, 30), guiUseRandomBuyRate, "Random Buyrate", toggleStyle);
                        // min buyrate
                        GUI.Label(new Rect(CENTERX, MENUY + 110, ITEMWIDTH, 30), "Min Buy Rate " + guiMinBuyRate.ToString(), labelStyle);
                        guiMinBuyRate = GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 140, ITEMWIDTH, 30), guiMinBuyRate, -1f, 10.0f);
                        // max buyrate
                        GUI.Label(new Rect(CENTERX, MENUY + 180, ITEMWIDTH, 30), "Max Buy Rate " + guiMaxBuyRate.ToString(), labelStyle);
                        guiMaxBuyRate = GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 210, ITEMWIDTH, 30), guiMaxBuyRate, -1f, 10.0f);


                        guiEnableScrapModifiers = GUI.Toggle(new Rect(CENTERX, MENUY + 260, ITEMWIDTH, 30), guiEnableScrapModifiers, "Enable Scrap Modifiers", toggleStyle);
                        // min scrap
                        GUI.Label(new Rect(CENTERX, MENUY + 310, ITEMWIDTH, 30), "Minimum Amount of Scrap " + guiMinScrap.ToString(), labelStyle);
                        guiMinScrap = (int)GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 340, ITEMWIDTH, 30), guiMinScrap, 0, 500);
                        // max scrap
                        GUI.Label(new Rect(CENTERX, MENUY + 380, ITEMWIDTH, 30), "Maximum Amount of Scrap " + guiMaxScrap.ToString(), labelStyle);
                        guiMaxScrap = (int)GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 410, ITEMWIDTH, 30), guiMaxScrap, 0, 500);
                        // min scrap value
                        GUI.Label(new Rect(CENTERX, MENUY + 450, ITEMWIDTH, 30), "Min Scrap Value " + guiMinScrapValue.ToString(), labelStyle);
                        guiMinScrapValue = (int)GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 480, ITEMWIDTH, 30), guiMinScrapValue, 0, 100000);
                        // max scrap value
                        GUI.Label(new Rect(CENTERX, MENUY + 520, ITEMWIDTH, 30), "Max Scrap Value " + guiMaxScrapValue.ToString(), labelStyle);
                        guiMaxScrapValue = (int)GUI.HorizontalSlider(new Rect(CENTERX, MENUY + 550, ITEMWIDTH, 30), guiMaxScrapValue, 0, 100000);
                        break;
                }

            }

        }
    }
}
