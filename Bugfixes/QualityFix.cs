using HarmonyLib;
using SerenityForge.KingsBird.UI;
using UnityEngine;

namespace Bugfixes
{
    internal class QualityFix : MonoBehaviour
    {
        private static readonly Harmony harmony = new Harmony("com.alexmorson.bugfixes.qualityfix");
        private void Awake() => harmony.PatchAll(typeof(QualityFix));
        private void OnDestroy() => harmony.UnpatchSelf();

        [HarmonyPatch(typeof(FlowShaderProcessor), "Start")]
        [HarmonyPrefix]
        private static void LoadSettings(FlowShaderProcessor __instance)
        {
            // When the level starts, the settings are loaded and applied to all
            // registered flow shaders in the scene. However, it turns out that
            // flow shaders only register themselves *after* this happens, so
            // none of the settings are actually applied.
            //
            // This fix loads and applies the settings each time a flow shader is
            // created.
            var settings = GameObject.Find("DDCanvas").GetComponentInChildren<PauseMenuSettings>();
            if (settings != null)
            {
                __instance.quality = 10.0f - settings.SimulatedEffectsQuality;
                __instance.autoAdjust = settings.SimulatedEffectsAutoAdjustment;
            }
        }
    }
}
