using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TasBird
{
    public class Replay : MonoBehaviour
    {
        public static event UnityAction<string, string, int> SaveReplay;

        private static ConfigEntry<KeyboardShortcut> takeOver;
        private static ConfigEntry<KeyboardShortcut> saveReplay;

        private static readonly Harmony Harmony = new Harmony("com.alexmorson.tasbird.replay");

        private void Awake()
        {
            var config = Plugin.Instance.Config;
            takeOver = config.Bind("Replay", "TakeOver", new KeyboardShortcut(KeyCode.Insert),
                "Take over a currently running replay");
            saveReplay = config.Bind("Replay", "SaveReplay", new KeyboardShortcut(KeyCode.S, KeyCode.LeftControl),
                "Save a replay formed by the inputs entered up to this point");

            Harmony.PatchAll(typeof(LoadReplayBuffersPatch));
        }

        private void OnDestroy()
        {
            Harmony.UnpatchSelf();
        }

        private void Update()
        {
            if (takeOver.Value.IsDown()) TakeOver();
            if (saveReplay.Value.IsDown()) Save();
        }

        public static void Load(string levelName, string replayBuffer, int breakpoint)
        {
            if (SceneManager.GetActiveScene() == LevelManager.ManagementScene)
                return;

            if (!LevelNames.NameExists(levelName)) return;
            var levelFile = LevelNames.NameToFile(levelName);

            LeaderBoard.Singleton.ToggleVisible(false);
            LevelInfoDisplay.uiDisplay.SetActive(false);
            if (PauseMenu.IsPaused) PauseMenu.instance.ToggleMenu();

            PlayerPip.Instance.QueueReplay(replayBuffer, Application.version);

            if (levelFile == SceneManager.GetActiveScene().name)
            {
                // Bit hacky, but avoids reloading the scene
                Checkpoint.FullResetCheckpoints(MasterController.GetPlayer());
                ++MasterController.GetPlayer().framesInLevel;
                MasterController.GetInput().OnFixedUpdate();
                Util.OnLevelReload();
                GameObject.Find("Fader").GetComponent<FadeLoader>().Fade(0);
            }
            else
            {
                SceneChanger.Instance.ChangeScene(levelFile);
            }

            Time.FastForwardUntil(breakpoint);
        }

        public static void LoadReplayBuffers(ReplayData buffers, uint breakpoint = 0)
        {
            Debug.Log("LoadReplayBuffers");

            var input = MasterController.GetInput();
            input.isReplay = true;

            foreach (var axis in buffers.axisBuffers.Keys)
            {
                var axisBuffer = new List<RootInputManager.Entry<InputManager.AxisChannel.State>>();
                foreach (var time in buffers.axisBuffers[axis].Keys)
                {
                    var state = (InputManager.AxisChannel.State)buffers.axisBuffers[axis][time];
                    axisBuffer.Add(new RootInputManager.Entry<InputManager.AxisChannel.State>(state, time));
                }

                var axisReplay = new InputManager.AxisReplay(axisBuffer);
                axisReplay.buffer.Clear(); // Remove default (0, Release) entry
                axisReplay.Update(breakpoint);
                input.axes[input.ToAxis(axis)] = axisReplay;
            }

            foreach (var key in buffers.buttonBuffers.Keys)
            {
                var buttonBuffer = new List<RootInputManager.Entry<InputManager.ButtonChannel.State>>();
                foreach (var time in buffers.buttonBuffers[key].Keys)
                {
                    var state = (InputManager.ButtonChannel.State)buffers.buttonBuffers[key][time];
                    buttonBuffer.Add(new RootInputManager.Entry<InputManager.ButtonChannel.State>(state, time));
                }

                var buttonReplay = new InputManager.ButtonReplay(buttonBuffer);
                buttonReplay.buffer.Clear(); // Remove default (0, Release) entry
                buttonReplay.Update(breakpoint);
                input.buttons[input.ToKey(key)] = buttonReplay;
            }
        }

        public static void TakeOver()
        {
            if (SceneManager.GetActiveScene() == LevelManager.ManagementScene)
                return;

            var input = MasterController.GetInput();
            if (!input.IsReplay) return;

            input.isReplay = false;

            // Convert AxisReplay into AxisChannel
            foreach (InputManager.Axis axis in Enum.GetValues(typeof(InputManager.Axis)))
                input.axes[axis] = new InputManager.AxisChannel { buffer = input.axes[axis].buffer };

            // Convert ButtonReplay into ButtonChannel
            foreach (InputManager.Key key in Enum.GetValues(typeof(InputManager.Key)))
                input.buttons[key] = new InputManager.ButtonChannel { buffer = input.buttons[key].buffer };

            // Bind the new input channels to the current control layout
            input.SetupControls(input.CurrentPeripheral, true);
        }

        public static void Save()
        {
            if (SceneManager.GetActiveScene() == LevelManager.ManagementScene)
                return;

            var input = MasterController.GetInput();
            if (input.IsReplay)
                return;

            var sceneName = SceneManager.GetActiveScene().name;
            if (!LevelNames.FileExists(sceneName))
            {
                Debug.Log($"Attempted to save replay in unknown scene: {sceneName}");
                return;
            }

            var levelName = LevelNames.FileToName(sceneName);

            var replayData = default(ReplayData);
            input.SetBuffers(ref replayData.buttonBuffers, ref replayData.axisBuffers);
            var replayBuffer = replayData.BuffersToString();

            var frame = MasterController.GetPlayer().framesInLevel;

            SaveReplay?.Invoke(levelName, replayBuffer, frame);
        }
    }

    [HarmonyPatch]
    internal static class LoadReplayBuffersPatch
    {
        private static MethodBase TargetMethod()
        {
            return typeof(BaseInputManager<InputManager.Key, InputManager.Axis, InputManager.KeyComparer,
                InputManager.AxisComparer>).GetMethod("LoadReplayBuffers");
        }

        private static bool Prefix(ReplayData rm)
        {
            Replay.LoadReplayBuffers(rm);
            return false;
        }
    }
}
