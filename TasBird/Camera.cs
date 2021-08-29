using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TasBird
{
    [BepInPlugin("com.alexmorson.tasbird.camera", "TasBird.Camera", "1.0")]
    [BepInDependency("com.alexmorson.tasbird.invalidate", "1.0")]
    public class Camera : BaseUnityPlugin
    {
        public static bool IsFixed { get; private set; }
        public static Vector3 Position { get; private set; }
        public static float FOV { get; private set; }
        public static float HalfHeight { get; private set; }

        private static Vector3 prevMousePos;

        private static readonly Harmony Harmony = new Harmony("com.alexmorson.tasbird.camera");

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Harmony.PatchAll(typeof(CameraApplyPatch));
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Harmony.UnpatchSelf();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => IsFixed = false;

        private void Update()
        {
            Cursor.visible = true;

            var mousePos = Input.mousePosition;
            if (Input.GetMouseButton(0) && prevMousePos != mousePos)
            {
                if (!IsFixed) FixCamera();
                Position -= (mousePos - prevMousePos) * HalfHeight / 540;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (scroll != 0)
            {
                if (!IsFixed) FixCamera();
                HalfHeight *= Mathf.Pow(2, -scroll / 2);
                FOV = 2 * Mathf.Atan(HalfHeight / -Position.z) * 180 / Mathf.PI;
            }

            if (Input.GetMouseButtonDown(2))
                IsFixed = false;

            prevMousePos = mousePos;
        }

        private static void FixCamera()
        {
            var camera = MasterController.GetCamera().state.Camera;
            IsFixed = true;
            Position = camera.transform.position;
            FOV = camera.fieldOfView;
            HalfHeight = -Position.z * Mathf.Tan(FOV / 2 * Mathf.PI / 180);
        }
    }

    [HarmonyPatch(typeof(CameraController.CameraState), "Apply")]
    internal class CameraApplyPatch
    {
        private static bool Prefix(CameraController.CameraState __instance)
        {
            if (!Camera.IsFixed) return true;

            __instance.Camera.transform.position = Camera.Position;
            __instance.Camera.fieldOfView = Camera.FOV;
            foreach (var camera in __instance.Camera.transform.GetComponentsInImmediateChildren<UnityEngine.Camera>())
                camera.orthographicSize = Camera.HalfHeight;

            return false;
        }
    }
}
