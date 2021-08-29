﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityTime = UnityEngine.Time;

namespace TasBird
{
    [BepInPlugin("com.alexmorson.tasbird.time", "TasBird.Time", "1.0")]
    [BepInDependency("com.alexmorson.tasbird.invalidate", "1.0")]
    [BepInDependency("com.alexmorson.tasbird.util", "1.0")]
    public class Time : BaseUnityPlugin
    {
        private static ConfigEntry<KeyboardShortcut> togglePause;
        private static ConfigEntry<KeyboardShortcut> stepFrame;
        private static ConfigEntry<KeyboardShortcut> speedUp;
        private static ConfigEntry<KeyboardShortcut> slowDown;

        private static readonly Harmony Harmony = new Harmony("com.alexmorson.tasbird.time");

        private static float lastTimeScale = 0.8f;
        private static int lastFrameSkip = 1;

        private static bool stepping;

        private static bool willFastForward;
        private static bool fastForwarding;
        private static int fastForwardUntil;

        public static float Multiplier => lastTimeScale / 0.8f;

        private Time()
        {
            togglePause = Config.Bind("Hotkeys", "TogglePause", new KeyboardShortcut(KeyCode.Keypad0),
                "Play/Pause the game");
            stepFrame = Config.Bind("Hotkeys", "StepFrame", new KeyboardShortcut(KeyCode.Space),
                "Step a single frame forward");
            speedUp = Config.Bind("Hotkeys", "SpeedUp", new KeyboardShortcut(KeyCode.Equals),
                "Speed the game up");
            slowDown = Config.Bind("Hotkeys", "SlowDown", new KeyboardShortcut(KeyCode.Minus),
                "Slow the game down");
        }

        private void Awake()
        {
            Util.LevelStart += OnLevelStart;
            Util.PlayerUpdate += OnPlayerUpdate;
            Harmony.PatchAll(typeof(ValidateSettingsPatch));
            Harmony.PatchAll(typeof(TogglePlayerLockPatch));
        }

        private void OnDestroy()
        {
            UnityTime.timeScale = 0.8f;
            ToggleFlowEffects(true);

            Util.LevelStart -= OnLevelStart;
            Util.PlayerUpdate -= OnPlayerUpdate;
            Harmony.UnpatchSelf();
        }

        private void Update()
        {
            if (Input.GetKeyDown(togglePause.Value.MainKey)) TogglePause();
            if (Input.GetKeyDown(stepFrame.Value.MainKey)) StepFrame();
            if (Input.GetKeyDown(speedUp.Value.MainKey)) SpeedUp();
            if (Input.GetKeyDown(slowDown.Value.MainKey)) SlowDown();
        }

        private static void OnLevelStart(bool newScene)
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
                    UnityTime.timeScale = Calc.Clamp(Mathf.Floor(fastForwardUntil - frame - lastTimeScale), 0.8f, 100f);
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

        public static void TogglePause()
        {
            UnityTime.timeScale = UnityTime.timeScale > 0 ? 0f : lastTimeScale;
        }

        public static void SpeedUp()
        {
            if (lastTimeScale > 100 / Mathf.Sqrt(2)) return;
            UnityTime.timeScale *= Mathf.Sqrt(2);
            lastTimeScale *= Mathf.Sqrt(2);
        }

        public static void SlowDown()
        {
            if (lastTimeScale < 0.01) return;
            UnityTime.timeScale /= Mathf.Sqrt(2);
            lastTimeScale /= Mathf.Sqrt(2);
        }

        public static void StepFrame()
        {
            stepping = true;
            UnityTime.timeScale = 0.8f;
            DisableFrameSkip();
        }

        public static void FastForwardUntil(int frame)
        {
            willFastForward = true;
            fastForwardUntil = frame;
            UnityTime.timeScale = 0.8f;
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
        private static void Prefix() => timeScale = UnityEngine.Time.timeScale;
        private static void Postfix() => UnityEngine.Time.timeScale = timeScale;
    }

    [HarmonyPatch(typeof(Player), "TogglePlayerLock")]
    internal static class TogglePlayerLockPatch
    {
        private static float timeScale;
        private static void Prefix() => timeScale = UnityEngine.Time.timeScale;
        private static void Postfix() => UnityEngine.Time.timeScale = timeScale;
    }
}