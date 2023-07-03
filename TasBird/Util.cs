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
        public static event UnityAction FrameEnd;

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
            Harmony.PatchAll(typeof(CameraControllerOnFixedUpdatePatch));
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

        public static void OnFrameEnd()
        {
            FrameEnd?.Invoke();

            // The boss sometimes uses the position of its transform (instead of
            // just using its own position) to determine where certain attacks
            // should spawn in. But the transform's position is only updated in
            // OnUpdate (not OnFixedUpdate), which means that these attacks are
            // non-deterministic.
            // This fixes that by forcing the boss's transform's position to be
            // updated at the end of every frame.
            var boss = MasterController.GetPlayer().refs.boss;
            if (boss != null)
                boss.transform.position = boss.position.V3;
        }
    }

    [HarmonyPatch(typeof(CameraController), "OnFixedUpdate")]
    internal static class CameraControllerOnFixedUpdatePatch
    {
        private static void Postfix()
        {
            // The only objects that are after the CameraController in the script
            // execution order are the ParallaxMover and FlowShaderProcessor, so
            // this is a good time to do things at the "end" of the frame.
            // I chose the CameraController because it exists in every scene
            // exactly once.
            Util.OnFrameEnd();
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
