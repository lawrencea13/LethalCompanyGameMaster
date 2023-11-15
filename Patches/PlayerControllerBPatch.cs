namespace LethalCompanyTestMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    public class PlayerControllerBPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static void patchControllerUpdate()
        {
            TestMod.myGUI.guiIsHost = TestMod.isHost;
            TestMod.Instance.UpdateCFGVarsFromGUI();
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void getNightVision(ref PlayerControllerB __instance)
        {
            TestMod.playerRef = __instance;
            TestMod.nightVision = TestMod.playerRef.nightVision.enabled;
            // store nightvision values
            TestMod.nightVisionIntensity = TestMod.playerRef.nightVision.intensity;
            TestMod.nightVisionColor = TestMod.playerRef.nightVision.color;
            TestMod.nightVisionRange = TestMod.playerRef.nightVision.range;

            TestMod.playerRef.nightVision.color = UnityEngine.Color.green;
            TestMod.playerRef.nightVision.intensity = 1000f;
            TestMod.playerRef.nightVision.range = 10000f;
        }

        [HarmonyPatch("SetNightVisionEnabled")]
        [HarmonyPostfix]
        static void updateNightVision()
        {
            //instead of enabling/disabling nightvision, set the variables

            if (TestMod.nightVision)
            {
                TestMod.playerRef.nightVision.color = UnityEngine.Color.green;
                TestMod.playerRef.nightVision.intensity = 1000f;
                TestMod.playerRef.nightVision.range = 10000f;
            }
            else
            {
                TestMod.playerRef.nightVision.color = TestMod.nightVisionColor;
                TestMod.playerRef.nightVision.intensity = TestMod.nightVisionIntensity;
                TestMod.playerRef.nightVision.range = TestMod.nightVisionRange;
            }

            // should always be on
            TestMod.playerRef.nightVision.enabled = true;
        }
        
        [HarmonyPatch("AllowPlayerDeath")]
        [HarmonyPrefix]
        static bool OverrideDeath()
        {
            if (!TestMod.isHost) { return true; }
            return !TestMod.enableGod;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void InfiniteSprint(ref float ___sprintMeter)
        {
            
            if (TestMod.EnableInfiniteSprint.Value && TestMod.isHost) { Mathf.Clamp(___sprintMeter += 0.02f, 0f, 1f); }
        }
    }
}