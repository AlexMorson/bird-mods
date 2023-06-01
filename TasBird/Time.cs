using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityTime = UnityEngine.Time;

namespace TasBird
{
    public class Time : MonoBehaviour
    {
        private static ConfigEntry<KeyboardShortcut> togglePause;
        private static ConfigEntry<KeyboardShortcut> stepFrame;
        private static ConfigEntry<KeyboardShortcut> speedUp;
        private static ConfigEntry<KeyboardShortcut> slowDown;
        private static ConfigEntry<bool> shouldFastForward;

        private static readonly Harmony Harmony = new Harmony("com.alexmorson.tasbird.time");

        private static float lastTimeScale = 0.8f;
        private static int lastFrameSkip = 1;

        private static bool stepping;

        private static bool willFastForward;
        private static bool fastForwarding;
        private static int fastForwardUntil;

        public static float Multiplier
        {
            get => lastTimeScale / 0.8f;
            set
            {
                var scale = value * 0.8f;
                if (0.01f < scale && scale < 100f) lastTimeScale = scale;
                if (!Paused) UnityTime.timeScale = lastTimeScale;
            }
        }

        public static bool Paused
        {
            get => UnityTime.timeScale == 0f;
            set => UnityTime.timeScale = value ? 0f : lastTimeScale;
        }

        private Time()
        {
            var config = Plugin.Instance.Config;
            togglePause = config.Bind("Time", "TogglePause", new KeyboardShortcut(KeyCode.Keypad0),
                "Play/Pause the game");
            stepFrame = config.Bind("Time", "StepFrame", new KeyboardShortcut(KeyCode.Space),
                "Step a single frame forward");
            speedUp = config.Bind("Time", "SpeedUp", new KeyboardShortcut(KeyCode.Equals),
                "Speed the game up");
            slowDown = config.Bind("Time", "SlowDown", new KeyboardShortcut(KeyCode.Minus),
                "Slow the game down");
            shouldFastForward = config.Bind("Time", "ShouldFastForward", true,
                "Should the game be sped up when watching a replay to a breakpoint?");
        }

        private void Awake()
        {
            Util.SceneLoaded += OnSceneLoaded;
            Util.PlayerUpdate += OnPlayerUpdate;
            Harmony.PatchAll(typeof(ValidateSettingsPatch));
            Harmony.PatchAll(typeof(TogglePlayerLockPatch));
        }

        private void OnDestroy()
        {
            UnityTime.timeScale = 0.8f;
            ToggleFlowEffects(true);

            Util.SceneLoaded -= OnSceneLoaded;
            Util.PlayerUpdate -= OnPlayerUpdate;
            Harmony.UnpatchSelf();
        }

        private void Update()
        {
            if (Input.GetKeyDown(togglePause.Value.MainKey)) Paused = !Paused;
            if (Input.GetKeyDown(stepFrame.Value.MainKey)) StepFrame();
            if (Input.GetKeyDown(speedUp.Value.MainKey)) SpeedUp();
            if (Input.GetKeyDown(slowDown.Value.MainKey)) SlowDown();
        }

        private static void OnSceneLoaded()
        {
            if (willFastForward)
            {
                willFastForward = false;
                fastForwarding = true;
            }
        }

        private static void OnPlayerUpdate(int frame)
        {
            if (stepping)
            {
                stepping = false;
                UnityTime.timeScale = 0;
                EnableFrameSkip();
            }

            if (fastForwarding)
            {
                if (frame < fastForwardUntil)
                {
                    var maxTimeScale = shouldFastForward.Value ? 100f : 0.8f;
                    UnityTime.timeScale = Calc.Clamp(Mathf.Floor(fastForwardUntil - frame - lastTimeScale), 0.8f, maxTimeScale);
                }
                else
                {
                    fastForwarding = false;
                    UnityTime.timeScale = 0;
                    EnableFrameSkip();
                }
            }

            ToggleFlowEffects(UnityTime.timeScale < 2);
        }

        public static void SpeedUp() => Multiplier *= Mathf.Sqrt(2);

        public static void SlowDown() => Multiplier /= Mathf.Sqrt(2);

        public static void StepFrame()
        {
            stepping = true;
            UnityTime.timeScale = 0.8f;
            DisableFrameSkip();
        }

        public static void FastForwardUntil(int frame, bool immediate = false)
        {
            UnityTime.timeScale = 0.8f;

            if (frame < 0)
            {
                // Watch from the start
                lastTimeScale = 0.8f;
                return;
            }

            if (immediate)
                fastForwarding = true;
            else
                willFastForward = true;

            fastForwardUntil = frame;
            DisableFrameSkip();
        }

        private static void DisableFrameSkip()
        {
            lastFrameSkip = (int)Mathf.Round(UnityTime.maximumDeltaTime / UnityTime.fixedDeltaTime);
            UnityTime.maximumDeltaTime = UnityTime.fixedDeltaTime;
        }

        private static void EnableFrameSkip()
        {
            UnityTime.maximumDeltaTime = UnityTime.fixedDeltaTime * lastFrameSkip;
        }

        private static void ToggleFlowEffects(bool on)
        {
            foreach (var flow in FlowShaderProcessor.AllFlows)
            {
                flow.skipCount = on ? 1 : 1e10f;
            }
        }
    }

    [HarmonyPatch(typeof(AssistModeController), "ValidateSettings")]
    internal static class ValidateSettingsPatch
    {
        private static float timeScale;
        private static void Prefix() => timeScale = UnityTime.timeScale;
        private static void Postfix() => UnityTime.timeScale = timeScale;
    }

    [HarmonyPatch(typeof(Player), "TogglePlayerLock")]
    internal static class TogglePlayerLockPatch
    {
        private static float timeScale;
        private static void Prefix() => timeScale = UnityTime.timeScale;
        private static void Postfix() => UnityTime.timeScale = timeScale;
    }
}
