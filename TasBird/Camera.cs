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
        public static Vector3 MouseWorld => Position - new Vector3(HalfWidth, HalfHeight) + Input.mousePosition / Zoom;

        private readonly ConfigEntry<KeyboardShortcut> resetCamera;

        private static Vector3 prevMousePos;

        private static readonly Harmony Harmony = new Harmony("com.alexmorson.tasbird.camera");

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
            if (Input.GetMouseButton(0) && prevMousePos != mousePos)
            {
                if (!IsFixed) IsFixed = true;
                Position -= (mousePos - prevMousePos) * HalfHeight / (Screen.height / 2f);
            }

            var scroll = Input.mouseScrollDelta.y;
            if (scroll != 0)
            {
                if (!IsFixed) IsFixed = true;
                HalfHeight *= Mathf.Pow(2, -scroll / 2);
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
