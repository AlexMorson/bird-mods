using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TasBird
{
    public class Practise : MonoBehaviour
    {
        private readonly ConfigEntry<bool> collectCheckpoints;
        private readonly ConfigEntry<bool> instantRestart;
        private readonly ConfigEntry<KeyboardShortcut> nextCheckpoint;
        private readonly ConfigEntry<KeyboardShortcut> prevCheckpoint;
        private readonly ConfigEntry<KeyboardShortcut> setPosition;

        private List<Checkpoint> checkpoints = new List<Checkpoint>();

        private Practise()
        {
            var config = Plugin.Instance.Config;
            collectCheckpoints = config.Bind("Practise", "CollectCheckpoints", true, "Whether touching a checkpoint should collect it");
            instantRestart = config.Bind("Practise", "InstantRestart", false, "Remove the delay when pressing restart");
            nextCheckpoint = config.Bind("Practise", "NextCheckpoint", new KeyboardShortcut(KeyCode.RightArrow, KeyCode.LeftControl), "Go to the next checkpoint");
            prevCheckpoint = config.Bind("Practise", "PrevCheckpoint", new KeyboardShortcut(KeyCode.LeftArrow, KeyCode.LeftControl), "Go to the previous checkpoint");
            setPosition = config.Bind("Practise", "SetPosition", new KeyboardShortcut(KeyCode.Mouse1), "Set Quill's position");

            collectCheckpoints.SettingChanged += (sender, e) => UpdateCheckpointState();
        }

        private void UpdateCheckpointState()
        {
            var player = MasterController.GetPlayer();
            if (player is null) return;

            var currentPriority = player.Checkpoint != null ? player.Checkpoint.priority : 0;
            foreach (var checkpoint in checkpoints)
            {
                var passed = !collectCheckpoints.Value || checkpoint.priority < currentPriority;
                var current = checkpoint == player.Checkpoint;
                checkpoint.passed = passed;
                if (checkpoint.LanternSprite != null)
                    checkpoint.LanternSprite.sprite =
                        current || !passed ? checkpoint.visualLantern : checkpoint.visualClosed;
                if (checkpoint.FlameObject != null)
                    checkpoint.FlameObject.SetActive(current);
            }
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene() != LevelManager.ManagementScene)
                OnLevelStart(true);

            Util.LevelStart += OnLevelStart;
            Util.PlayerUpdate += OnPlayerUpdate;
        }

        private void OnLevelStart(bool newScene)
        {
            checkpoints = MasterController.GetObjects().ListOutAllObjects<Checkpoint>().OrderBy(c => c.priority).ThenBy(c => c.FinalSpawnPosition.x).ToList();
            UpdateCheckpointState();
        }

        private void OnPlayerUpdate(int frame)
        {
            if (!instantRestart.Value) return;

            var player = MasterController.GetPlayer();
            player.CheckpointResetAvailable = true;
            if (player.Killed)
                GameObject.Find("Fader").GetComponent<FadeLoader>().currentFrameCount += 3;
        }

        private void Update()
        {
            if (nextCheckpoint.Value.IsDown())
                LoadCheckpoint(1);
            if (prevCheckpoint.Value.IsDown())
                LoadCheckpoint(-1);
            if (setPosition.Value.IsDown())
            {
                var player = MasterController.GetPlayer();
                player.Position = Camera.MouseWorld;
                player.Velocity = Coord.Zero;
                player.Contact = Vector.Null;
            }
        }

        private void LoadCheckpoint(int offset)
        {
            if (checkpoints.Count == 0) return;

            var player = MasterController.GetPlayer();
            if (player is null) return;

            var currentIndex = checkpoints.IndexOf(player.Checkpoint);
            var newIndex = Math.Max(0, Math.Min(checkpoints.Count - 1, currentIndex + offset));

            Checkpoint.SaveCheckpoints(checkpoints[newIndex], player);
            player.LoadCheckpoint();
            UpdateCheckpointState();
        }
    }
}
