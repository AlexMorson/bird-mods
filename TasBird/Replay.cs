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

        private static State? levelStartState;

        private void Awake()
        {
            var config = Plugin.Instance.Config;
            takeOver = config.Bind("Replay", "TakeOver", new KeyboardShortcut(KeyCode.Insert),
                "Take over a currently running replay");
            saveReplay = config.Bind("Replay", "SaveReplay", new KeyboardShortcut(KeyCode.S, KeyCode.LeftControl),
                "Save a replay formed by the inputs entered up to this point");

            Util.SceneLoaded += OnSceneLoaded;

            Harmony.PatchAll(typeof(LoadReplayBuffersPatch));
        }

        private void OnDestroy()
        {
            Util.SceneLoaded -= OnSceneLoaded;

            Harmony.UnpatchSelf();
        }

        private static void OnSceneLoaded()
        {
            levelStartState = State.Save();
        }

        private void Update()
        {
            if (takeOver.Value.IsDown()) TakeOver();
            if (saveReplay.Value.IsDown()) Save();
        }

        public static void Load(string levelName, string replayString, int breakpoint)
        {
            if (SceneManager.GetActiveScene() == LevelManager.ManagementScene)
                return;

            if (!LevelNames.NameExists(levelName)) return;
            var levelFile = LevelNames.NameToFile(levelName);

            // Hide all UI elements
            LeaderBoard.Singleton.ToggleVisible(false);
            LevelInfoDisplay.uiDisplay.SetActive(false);
            if (PauseMenu.IsPaused) PauseMenu.instance.ToggleMenu();

            if (levelFile == SceneManager.GetActiveScene().name && !MasterController.GetPlayer().ending)
            {
                var replayBuffers = default(ReplayData);
                replayBuffers.StringToBuffer(replayString);

                // Try to save some fast-forwarding by starting from an existing state
                var chosenState = levelStartState;
                foreach (var state in StateManager.States.Values)
                {
                    if (state.Frame <= breakpoint && (!chosenState.HasValue || state.Frame > chosenState.Value.Frame) &&
                        state.IsPrefixOf(replayBuffers))
                    {
                        chosenState = state;
                    }
                }

                if (chosenState.HasValue)
                {
                    // Load the state, overwrite the inputs and fast-forward
                    chosenState.Value.Load();
                    LoadReplayBuffers(replayBuffers, chosenState.Value.Frame);
                    if (chosenState.Value.Frame == breakpoint)
                        Time.Paused = true;
                    else
                        Time.FastForwardUntil(breakpoint, true);

                    return;
                }
            }

            // No candidate state exists, so just reload the scene
            PlayerPip.Instance.QueueReplay(replayString, Application.version);
            Time.FastForwardUntil(breakpoint);
            SceneChanger.Instance.ChangeScene(levelFile);
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
