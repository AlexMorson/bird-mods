using BepInEx;
using HarmonyLib;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TasBird
{
    [BepInPlugin("com.alexmorson.tasbird.util", "TasBird.Util", "1.0")]
    public class Util : BaseUnityPlugin
    {
        public static event UnityAction<bool> LevelStart;
        public static event UnityAction<int> PlayerUpdate;

        private static readonly Harmony Harmony = new Harmony("com.alexmorson.tasbird.util");

        private static bool sceneLoaded;
        private static bool newScene;

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Harmony.PatchAll(typeof(PlayerUpdatePatch));
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Harmony.UnpatchSelf();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene == LevelManager.ManagementScene) return;

            sceneLoaded = true;
            newScene = true;
        }

        public static void OnLevelReload()
        {
            sceneLoaded = true;
            newScene = false;
        }

        private void FixedUpdate()
        {
            if (!sceneLoaded) return;
            sceneLoaded = false;

            LevelStart?.Invoke(newScene);
        }

        public static void OnPlayerUpdate(int frame)
        {
            PlayerUpdate?.Invoke(frame);
        }
    }

    [HarmonyPatch(typeof(PhysicsObject), "OnFixedUpdate")]
    internal static class PlayerUpdatePatch
    {
        private static void Postfix(PhysicsObject __instance)
        {
            if (__instance is Player player)
                Util.OnPlayerUpdate(player.framesInLevel);
        }
    }
}
