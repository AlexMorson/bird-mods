using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TasBird
{
    public class Util : MonoBehaviour
    {
        public static event UnityAction SceneLoaded;
        public static event UnityAction<int> PlayerUpdate;

        private static readonly Harmony Harmony = new Harmony("com.alexmorson.tasbird.util");

        private static bool sceneLoaded;

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
