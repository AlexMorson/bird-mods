using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TasBird
{
    public class Util : MonoBehaviour
    {
        private static ConfigEntry<bool> autoExitLevels;

        public static event UnityAction SceneLoaded;
        public static event UnityAction<int> PlayerUpdate;

        private static readonly Harmony Harmony = new Harmony("com.alexmorson.tasbird.util");

        private static bool sceneLoaded;

        public static bool AutoExitLevels => autoExitLevels.Value;

        private Util()
        {
            var config = Plugin.Instance.Config;
            autoExitLevels = config.Bind("Util", "AutoExitLevels", false, "Automatically exit levels when reaching the end points");
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Harmony.PatchAll(typeof(InputFixedUpdatePatch));
            Harmony.PatchAll(typeof(PlayerStartPatch));
            Harmony.PatchAll(typeof(EndPointPatch));
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Harmony.UnpatchSelf();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene != LevelManager.ManagementScene)
                sceneLoaded = true;
        }

        private void FixedUpdate()
        {
            if (!sceneLoaded) return;
            sceneLoaded = false;

            SceneLoaded?.Invoke();
        }

        public static void OnPlayerUpdate(int frame)
        {
            PlayerUpdate?.Invoke(frame);
        }
    }

    [HarmonyPatch(typeof(InputManager), "OnFixedUpdate")]
    internal static class InputFixedUpdatePatch
    {
        private static void Postfix(InputManager __instance)
        {
            Util.OnPlayerUpdate((int)__instance.timeCount);
        }
    }

    [HarmonyPatch(typeof(PhysicsObject), "Start")]
    internal static class PlayerStartPatch
    {
        private static void Postfix(PhysicsObject __instance)
        {
            // Force the InputManager to be created to avoid non-determinism
            if (__instance is Player player)
                MasterController.GetInput();
        }
    }

    [HarmonyPatch(typeof(EndPoint), "GoToNext")]
    internal static class EndPointPatch
    {
        private static void Postfix(EndPoint __instance)
        {
            if (__instance.waitingForInfoDisplay && Util.AutoExitLevels)
                EndPoint.LevelInfoAction();
        }
    }
}
