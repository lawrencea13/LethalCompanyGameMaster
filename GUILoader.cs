using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LethalCompanyTestMod.Component
{
    internal class GUILoader : MonoBehaviour
    {
        private static Rect tempRect;

        private void Awake()
        {
            TestMod.mls.LogInfo("GUILoader loaded.");
            tempRect.x = 0;
            tempRect.y = 0;
            tempRect.width = 0;
            tempRect.height = 0;
            //new Rect((Screen.width / 2f) - 7, (Screen.height / 2f) - 7, 25f, 25f)
            //OnGUI();
            
        }

        public void OnGUI()
        {
            TestMod.mls.LogInfo("Frame update");
            TestMod.mls.LogInfo(tempRect.x);
            GUI.Box(tempRect, string.Empty);
        }
    }
}
