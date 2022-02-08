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
        private readonly ConfigEntry<bool> instantRestart;
        private readonly ConfigEntry<KeyboardShortcut> nextCheckpoint;
        private readonly ConfigEntry<KeyboardShortcut> prevCheckpoint;

        private List<Checkpoint> checkpoints = new List<Checkpoint>();

        private Practise()
        {
            var config = Plugin.Instance.Config;
            instantRestart = config.Bind("Practise", "InstantRestart", false, "Remove the delay when pressing restart");
            nextCheckpoint = config.Bind("Practise", "NextCheckpoint", new KeyboardShortcut(KeyCode.RightArrow, KeyCode.LeftControl), "Go to the next checkpoint");
            prevCheckpoint = config.Bind("Practise", "PrevCheckpoint", new KeyboardShortcut(KeyCode.LeftArrow, KeyCode.LeftControl), "Go to the previous checkpoint");
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene() != LevelManager.ManagementScene)
                OnNewSceneLoaded();

            Util.LevelStart += OnLevelStart;
            Util.PlayerUpdate += OnPlayerUpdate;
        }

        private void OnLevelStart(bool newScene)
        {
            if (newScene)
                OnNewSceneLoaded();
        }

        private void OnNewSceneLoaded()
        {
            checkpoints = MasterController.GetObjects().ListOutAllObjects<Checkpoint>().OrderBy(c => c.priority).ThenBy(c => c.FinalSpawnPosition.x).ToList();
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
        }
    }
}
