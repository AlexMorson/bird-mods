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
            Harmony.PatchAll(typeof(InputUpdatePatch));
            Harmony.PatchAll(typeof(PlayerStartPatch));
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
    internal static class InputUpdatePatch
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
}
