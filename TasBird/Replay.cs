using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TasBird
{
    class QueuedReplay
    {
        public ReplayData replayData;
        public Coord? startPosition;

        public QueuedReplay(ReplayData replayData, Coord? startPosition = null)
        {
            this.replayData = replayData;
            this.startPosition = startPosition;
        }
    }

    public class Replay : MonoBehaviour
    {
        public static event UnityAction<string, string, int> SaveReplay;

        private static ConfigEntry<KeyboardShortcut> takeOver;
        private static ConfigEntry<KeyboardShortcut> saveReplay;

        private static Queue<QueuedReplay> queuedReplays = new Queue<QueuedReplay>();

        private void Awake()
        {
            var config = Plugin.Instance.Config;
            takeOver = config.Bind("Replay", "TakeOver", new KeyboardShortcut(KeyCode.Insert),
                "Take over a currently running replay");
            saveReplay = config.Bind("Replay", "SaveReplay", new KeyboardShortcut(KeyCode.S, KeyCode.LeftControl),
                "Save a replay formed by the inputs entered up to this point");

            Util.SceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            Util.SceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded()
        {
            if (queuedReplays.Count == 0)
                return;

            var replay = queuedReplays.Dequeue();

            if (replay.startPosition.HasValue)
            {
                MasterController.GetPlayer().Position = replay.startPosition.Value;
                MasterController.GetCamera().Restart();
            }

            // Ensure that the replay is played back with the current game version's physics
            PlayerPip.instance.replayVersion = Application.version;

            MasterController.GetInput().LoadReplayBuffers(replay.replayData);
        }

        private void Update()
        {
            if (takeOver.Value.IsDown()) TakeOver();
            if (saveReplay.Value.IsDown()) Save();
        }

        public static void Load(string levelName, string replayString, int breakpoint, Coord? startPosition = null)
        {
            if (SceneManager.GetActiveScene() == LevelManager.ManagementScene)
                return;

            if (!LevelNames.NameExists(levelName)) return;
            var levelFile = LevelNames.NameToFile(levelName);

            // Hide all UI elements
            LeaderBoard.Singleton.ToggleVisible(false);
            LevelInfoDisplay.uiDisplay.SetActive(false);
            if (PauseMenu.IsPaused) PauseMenu.instance.ToggleMenu();

            var replayData = default(ReplayData);
            replayData.StringToBuffer(replayString);

            // Try to save some fast-forwarding by starting from an existing state
            if (breakpoint >= 0 && levelFile == SceneManager.GetActiveScene().name && !MasterController.GetPlayer().ending && !startPosition.HasValue)
            {
                State? chosenState = null;
                foreach (var state in StateManager.States.Values)
                {
                    // If this state is no better than the current best, ignore it
                    if (chosenState.HasValue && state.Frame <= chosenState.Value.Frame)
                        continue;

                    // We cannot use a state that was saved after the breakpoint
                    if (state.Frame > breakpoint)
                        continue;

                    if (state.IsPrefixOf(replayData))
                        chosenState = state;
                }

                if (chosenState.HasValue)
                {
                    // Load the state, overwrite the position and inputs, and fast-forward
                    chosenState.Value.Load();

                    // Hack to match behaviour when loading from another scene
                    // because an extra FixedUpdate call is being made somewhere.
                    PlayerPip.Instance.countdown += 1;

                    if (startPosition.HasValue)
                    {
                        MasterController.GetPlayer().Position = startPosition.Value;
                        MasterController.GetCamera().Restart();
                    }

                    MasterController.GetInput().LoadReplayBuffers(replayData);
                    ReadInputsUntil(chosenState.Value.Frame);

                    if (chosenState.Value.Frame == breakpoint)
                        Time.Paused = true;
                    else
                        Time.FastForwardUntil(breakpoint, true);

                    return;
                }
            }

            // We do not want to load any existing queued replays
            queuedReplays.Clear();

            // No candidate state exists, so just reload the scene
            queuedReplays.Enqueue(new QueuedReplay(replayData, startPosition));
            Time.FastForwardUntil(breakpoint);
            SceneChanger.Instance.ChangeScene(levelFile);
        }

        public static void ReadInputsUntil(uint frame)
        {
            var input = MasterController.GetInput();
            foreach (var axisBuffer in input.axes.Values)
                axisBuffer.Update(frame);
            foreach (var buttonBuffer in input.buttons.Values)
                buttonBuffer.Update(frame);
        }

        public static void Queue(string replayString)
        {
            var replayData = default(ReplayData);
            replayData.StringToBuffer(replayString);
            queuedReplays.Enqueue(new QueuedReplay(replayData));
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
}
