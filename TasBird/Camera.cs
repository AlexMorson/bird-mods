using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TasBird
{
    public class Camera : MonoBehaviour
    {
        public static bool IsFixed { get; private set; }
        public static Vector3 Position { get; private set; }
        public static float FOV { get; private set; }
        public static float HalfHeight { get; private set; }
        public static float HalfWidth => HalfHeight / Screen.height * Screen.width;
        public static float Zoom => Screen.height / 2f / HalfHeight;
        public static Vector3 MouseWorld => ScreenToWorld(Input.mousePosition);

        private readonly ConfigEntry<KeyboardShortcut> resetCamera;

        private static Vector3 prevMousePos;

        private static readonly Harmony Harmony = new Harmony("com.alexmorson.tasbird.camera");

        public static Vector3 ScreenToWorld(Vector3 pos)
        {
            return pos / Zoom + Position - new Vector3(HalfWidth, HalfHeight);
        }

        public static Vector3 WorldToScreen(Vector3 pos)
        {
            return (pos - (Position - new Vector3(HalfWidth, HalfHeight))) * Zoom;
        }

        private Camera()
        {
            resetCamera = Plugin.Instance.Config.Bind("Camera", "Reset", new KeyboardShortcut(KeyCode.Mouse2),
                "Reset the camera");
        }

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

        public static void SetParameters(Vector3 position, float fov)
        {
            Position = position;
            FOV = fov;
            HalfHeight = -Position.z * Mathf.Tan(FOV / 2 * Mathf.PI / 180);
        }

        private void Update()
        {
            Cursor.visible = true;

            var mousePos = Input.mousePosition;
            var leftHeld = Input.GetMouseButton(0);
            var inWindow = 0 <= mousePos.x && mousePos.x < Screen.width && 0 <= mousePos.y && mousePos.y < Screen.height;
            var scrollDelta = Input.mouseScrollDelta.y;

            if (leftHeld && prevMousePos != mousePos)
            {
                if (!IsFixed) IsFixed = true;
                Position -= (mousePos - prevMousePos) * HalfHeight / (Screen.height / 2f);
            }

            if (scrollDelta != 0 && (leftHeld || inWindow))
            {
                if (!IsFixed) IsFixed = true;
                HalfHeight *= Mathf.Pow(2, -scrollDelta / 2);
                FOV = 2 * Mathf.Atan(HalfHeight / -Position.z) * 180 / Mathf.PI;
            }

            if (resetCamera.Value.IsDown())
                IsFixed = false;

            prevMousePos = mousePos;
        }
    }

    [HarmonyPatch(typeof(CameraController.CameraState), "Apply")]
    internal class CameraApplyPatch
    {
        private static bool Prefix(CameraController.CameraState __instance)
        {
            if (!Camera.IsFixed)
            {
                Camera.SetParameters(__instance.Camera.transform.position, __instance.Camera.fieldOfView);
                return true;
            }

            __instance.Camera.transform.position = Camera.Position;
            __instance.Camera.fieldOfView = Camera.FOV;
            foreach (var camera in __instance.Camera.transform.GetComponentsInImmediateChildren<UnityEngine.Camera>())
                camera.orthographicSize = Camera.HalfHeight;

            return false;
        }
    }
}
