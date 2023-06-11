using HarmonyLib;
using UnityEngine;

namespace Bugfixes
{
    internal class CameraFix : MonoBehaviour
    {
        private static readonly Harmony harmony = new Harmony("com.alexmorson.bugfixes.camerafix");
        private void Awake() => harmony.PatchAll(typeof(CameraFix));
        private void OnDestroy() => harmony.UnpatchSelf();

        [HarmonyPatch(typeof(CameraZone.CameraPreModifier), "Reset")]
        [HarmonyPostfix]
        private static void ResetWarmup(CameraZone.CameraPreModifier __instance)
        {
            // If the level restarts while the camera modifier is cooling down
            // (ie. warmup < 0), then on the first frame the saved position will
            // be set to the player's position. This means that when the player
            // next touches a camera zone, the saved position is way off from what
            // it should be, and causes the camera to jump.
            //
            // This fix resets the warmup, so that the modifier is not cooling
            // down when the level starts.
            __instance.warmup = 0;
        }
    }
}
