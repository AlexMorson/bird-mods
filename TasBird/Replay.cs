using System;
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

            var axesField = AccessTools.Field(typeof(InputManager), "axes");
            var axesItemProperty = AccessTools.Property(axesField.FieldType, "Item");

            var axisChannelGenericType = AccessTools.Inner(typeof(InputManager), "AxisChannel");
            var axisChannelType = axisChannelGenericType.MakeGenericType(typeof(InputManager.Key),
                typeof(InputManager.Axis), typeof(InputManager.KeyComparer), typeof(InputManager.AxisComparer));
            var axisChannelConstructor = AccessTools.Constructor(axisChannelType);
            var axisBufferField = AccessTools.Field(axisChannelType, "buffer");

            var axes = axesField.GetValue(input);
            foreach (InputManager.Axis axis in Enum.GetValues(typeof(InputManager.Axis)))
            {
                // Get AxisReplay from axes[axis]
                var axisReplay = axesItemProperty.GetGetMethod().Invoke(axes, new object[] { axis });

                // Create new InputManager.AxesChannel
                var axisChannel = axisChannelConstructor.Invoke(null, new object[] { });

                // axisChannel.buffer = axisReplay.buffer
                axisBufferField.SetValue(axisChannel, axisBufferField.GetValue(axisReplay));

                // Set axes[axis] to the AxesChannel
                axesItemProperty.GetSetMethod().Invoke(axes, new[] { axis, axisChannel });
            }

            var buttonsField = AccessTools.Field(typeof(InputManager), "buttons");
            var buttonsItemProperty = AccessTools.Property(buttonsField.FieldType, "Item");

            var buttonChannelGenericType = AccessTools.Inner(typeof(InputManager), "ButtonChannel");
            var buttonChannelType = buttonChannelGenericType.MakeGenericType(typeof(InputManager.Key),
                typeof(InputManager.Axis), typeof(InputManager.KeyComparer), typeof(InputManager.AxisComparer));
            var buttonChannelConstructor = AccessTools.Constructor(buttonChannelType);
            var buttonBufferField = AccessTools.Field(buttonChannelType, "buffer");

            var buttons = buttonsField.GetValue(input);
            foreach (InputManager.Key key in Enum.GetValues(typeof(InputManager.Key)))
            {
                // Get ButtonReplay from buttons[key]
                var buttonReplay = buttonsItemProperty.GetGetMethod().Invoke(buttons, new object[] { key });

                // Create new InputManager.ButtonChannel
                var buttonChannel = buttonChannelConstructor.Invoke(null, new object[] { });

                // buttonChannel.buffer = buttonReplay.buffer
                buttonBufferField.SetValue(buttonChannel, buttonBufferField.GetValue(buttonReplay));

                // Set buttons[key] to the ButtonChannel
                buttonsItemProperty.GetSetMethod().Invoke(buttons, new[] { key, buttonChannel });
            }

            // Bind the input channels to the current control layout
            var setupControls = AccessTools.Method(typeof(InputManager), "SetupControls");
            setupControls.Invoke(input, new object[] { input.CurrentPeripheral, true });

            AccessTools.FieldRefAccess<InputManager, bool>(input, "isReplay") = false;
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
