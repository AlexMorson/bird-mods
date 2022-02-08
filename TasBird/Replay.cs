using System;
using BepInEx.Configuration;
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

        private void Awake()
        {
            var config = Plugin.Instance.Config;
            takeOver = config.Bind("Replay", "TakeOver", new KeyboardShortcut(KeyCode.Insert),
                "Take over a currently running replay");
            saveReplay = config.Bind("Replay", "SaveReplay", new KeyboardShortcut(KeyCode.S, KeyCode.LeftControl),
                "Save a replay formed by the inputs entered up to this point");
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
