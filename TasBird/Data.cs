using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TasBird
{
    [BepInPlugin("com.alexmorson.tasbird.data", "TasBird.Data", "1.0")]
    [BepInDependency("com.alexmorson.tasbird.invalidate", "1.0")]
    [BepInDependency("com.alexmorson.tasbird.util", "1.0")]
    public class Data : BaseUnityPlugin
    {
        private static bool minimalModeEnabled;
        private static ConfigEntry<bool> minimalMode;

        private static readonly GUIStyle BgStyle = new GUIStyle { fontSize = 20, normal = { textColor = Color.black } };
        private static readonly GUIStyle FgStyle = new GUIStyle { fontSize = 20, normal = { textColor = Color.white } };

        private static readonly Material Material = new Material(Shader.Find("Sprites/Default"));
        private static readonly Texture2D Texture = new Texture2D(1, 1);

        private LineRenderer noDash;
        private readonly List<LineRenderer> surfaces = new List<LineRenderer>();
        private readonly List<LineRenderer> circles = new List<LineRenderer>();
        private readonly List<Rectangle> deathZones = new List<Rectangle>();
        private readonly List<Rectangle> checkpoints = new List<Rectangle>();
        private readonly List<Rectangle> endPoints = new List<Rectangle>();

        private Data()
        {
            minimalMode = Config.Bind("General", "MinimalMode", false, "Turn off all background art and details");
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene() != LevelManager.ManagementScene)
                OnNewSceneLoaded();

            Util.LevelStart += OnLevelStart;
            Util.PlayerUpdate += OnPlayerUpdate;

            if (noDash is null)
                noDash = CreateLineRenderer(Color.red, 4, true, 1);
        }

        private void OnDestroy()
        {
            Util.LevelStart -= OnLevelStart;
            Util.PlayerUpdate -= OnPlayerUpdate;

            if (minimalModeEnabled)
                ToggleMinimalMode(false);

            foreach (var renderer in surfaces.Union(circles))
            {
                if (renderer is null) continue;
                Destroy(renderer.gameObject);
            }

            surfaces.Clear();
            circles.Clear();
        }

        private void Update()
        {
            if (minimalMode.Value != minimalModeEnabled)
                ToggleMinimalMode(minimalMode.Value);
        }

        private void OnPlayerUpdate(int frame)
        {
            var player = MasterController.GetPlayer();
            var lastDash = AccessTools.FieldRefAccess<Player, Vector>(player, "lastDash");
            if (lastDash.Exists)
            {
                var points = ConnectedVectorsOfSameKind(lastDash).ToArray();
                noDash.positionCount = points.Length;
                noDash.SetPositions(points);
            }
            else
            {
                noDash.positionCount = 0;
            }
        }

        private void OnLevelStart(bool newScene)
        {
            if (newScene)
                OnNewSceneLoaded();
        }

        private void OnNewSceneLoaded()
        {
            minimalModeEnabled = false;

            deathZones.Clear();
            foreach (var deathZone in MasterController.GetObjects().GetObjects<DeathZone>())
                if (deathZone.kind == 0)
                    deathZones.Add(deathZone.GridHitbox);

            checkpoints.Clear();
            foreach (var checkpoint in MasterController.GetObjects().GetObjects<Checkpoint>())
                checkpoints.Add(checkpoint.GridHitbox);

            endPoints.Clear();
            foreach (var endPoint in MasterController.GetObjects().GetObjects<EndPoint>())
                endPoints.Add(endPoint.GridHitbox);

            surfaces.Clear();
            CreateSurfaceRenderers();

            circles.Clear();
            CreateCircleRenderers();
        }

        private void OnGUI()
        {
            var player = MasterController.GetPlayer();
            if (player is null) return;

            var timers = AccessTools.FieldRefAccess<Player, Player.Timer>(player, "timers");

            var cloakField = AccessTools.Field(typeof(Player), "cloak");
            var powerField = AccessTools.Field(cloakField.FieldType, "power");
            var power = (double)powerField.GetValue(cloakField.GetValue(player));

            var text = $@"Frame: {player.framesInLevel}
Time Scale: {Time.Multiplier:0.00}
Pos: {player.Position.x:0.00}, {player.Position.y:0.00}
Vel: {player.Velocity.x:0.00}, {player.Velocity.y:0.00}
Speed: {player.Velocity.Length:0.00} at {Math.Atan2(player.Velocity.y, player.Velocity.x) * 180 / Math.PI:0.0}°
Cloak: {timers.Get(Player.Timers.Cloak):0}, {Math.Round(power * 45.0):0}
Contact Angle: {(player.Contact.Exists ? $"{(float)player.Contact.Angle:0.0}°" : "None")}";

            var timersText = "Timers:\n";
            foreach (Player.Timers timer in Enum.GetValues(typeof(Player.Timers)))
                if (timer != Player.Timers.CanWind)
                    timersText += $"{timer}: {timers.Get(timer)}\n";

            DrawBounds(player.Hitbox.Bounds, new Color(0, 1, 0, 0.5f));
            DrawBounds(new Rectangle(player.Hitbox.Center, 4, 4), new Color(1, 1, 1, 1));
            foreach (var deathZone in deathZones)
                DrawBounds(deathZone, new Color(1, 0, 0, 0.5f));
            foreach (var checkpoint in checkpoints)
                DrawBounds(checkpoint, new Color(1, 1, 0, 0.3f));
            foreach (var endPoint in endPoints)
                DrawBounds(endPoint, new Color(0, 1, 0, 0.3f));

            DrawText(text, 10, 10);
            DrawText(timersText, 10, 200);
        }

        private static void ToggleMinimalMode(bool on)
        {
            if (minimalModeEnabled == on) return;

            minimalModeEnabled = on;
            foreach (var layer in LayerManager.Manager.Layers)
            {
                if (layer.name == "PlayerLayer")
                    layer.ToggleArtVisibility(!on);
                else
                    layer.ToggleLayerVisibility(!on);
            }
        }

        private void CreateSurfaceRenderers()
        {
            var seen = new HashSet<Vector>();
            foreach (var vector in MasterController.GetCollisionVectors())
            {
                if (seen.Contains(vector)) continue;
                seen.Add(vector);

                var points = new List<Vector3> { vector.End.V3 };

                var current = vector.Tail;
                while (current != vector && current.Exists)
                {
                    seen.Add(current);
                    points.Add(current.End.V3);
                    current = current.Tail;
                }

                var lineRenderer = CreateLineRenderer(Color.white, 4, false, 0);

                if (current.Exists)
                    lineRenderer.loop = true;
                else
                    points.Insert(0, vector.Start.V3);

                lineRenderer.positionCount = points.Count;
                lineRenderer.SetPositions(points.ToArray());
                surfaces.Add(lineRenderer);
            }
        }

        private void CreateCircleRenderers()
        {
            foreach (var bird in MasterController.GetObjects().GetObjects<Collectibird>())
            {
                var lineRenderer = CreateLineRenderer(Color.white, 2, false, 0);
                var spawn = AccessTools.FieldRefAccess<Collectibird, Coord>(bird, "spawn");
                CreateCircleRenderer(lineRenderer, (float)spawn.x, (float)spawn.y, 64, 30);
                circles.Add(lineRenderer);
            }

            foreach (var totem in MasterController.GetObjects().GetObjects<Totem>())
            {
                var lineRenderer = CreateLineRenderer(Color.white, 2, false, 0);
                var position = totem.transform.position;
                CreateCircleRenderer(lineRenderer, position.x, position.y, 96, 30);
                circles.Add(lineRenderer);
            }

            foreach (var cage in MasterController.GetObjects().GetObjects<CageZone>())
            {
                var start = cage.Beginning;
                var end = cage.End;
                var lineRenderer = CreateLineRenderer(Color.white, 2, false, 0);
                var position = start.transform.position;
                CreateCircleRenderer(lineRenderer, position.x, position.y, start.radius, 30);
                circles.Add(lineRenderer);
                lineRenderer = CreateLineRenderer(Color.white, 2, false, 0);
                position = end.transform.position;
                CreateCircleRenderer(lineRenderer, position.x, position.y, end.radius, 30);
                circles.Add(lineRenderer);
            }
        }

        private static void DrawBounds(Rectangle bounds, Color color)
        {
            Texture.SetPixel(0, 0, color);
            Texture.Apply();

            var camera = MasterController.GetCamera().state.Camera;
            var ul = camera.WorldToScreenPoint(bounds.UL.V3);
            var lr = camera.WorldToScreenPoint(bounds.LR.V3);
            var position = new Vector2(ul.x, Screen.height - ul.y);
            var size = new Vector2(lr.x - ul.x, ul.y - lr.y);
            GUI.DrawTexture(new Rect(position, size), Texture);
        }

        private static void DrawText(string text, float x, float y)
        {
            float width = Screen.width;
            float height = Screen.height;
            GUI.Label(new Rect(x - 1, y - 1, width, height), text, BgStyle);
            GUI.Label(new Rect(x + 1, y - 1, width, height), text, BgStyle);
            GUI.Label(new Rect(x - 1, y + 1, width, height), text, BgStyle);
            GUI.Label(new Rect(x + 1, y + 1, width, height), text, BgStyle);
            GUI.Label(new Rect(x, y, width, height), text, FgStyle);
        }

        private static List<Vector3> ConnectedVectorsOfSameKind(Vector vector)
        {
            var kind = vector.GetKind;
            var points = new List<Vector3> { vector.Start.V3, vector.End.V3 };

            var current = vector.Tail;
            while (current != vector && current.Exists && current.GetKind == kind)
            {
                points.Add(current.End.V3);
                current = current.Tail;
            }

            points.Reverse();

            current = vector.Head;
            while (current != vector && current.Exists && current.GetKind == kind)
            {
                points.Add(current.Start.V3);
                current = current.Head;
            }

            return points;
        }

        private LineRenderer CreateLineRenderer(Color color, float width, bool persist, int order)
        {
            var child = new GameObject();
            if (persist) child.transform.parent = gameObject.transform;
            var lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.sortingLayerName = "Foreground";
            lineRenderer.sortingOrder = 10000 + order;
            lineRenderer.material = Material;
            lineRenderer.startColor = lineRenderer.endColor = color;
            lineRenderer.startWidth = lineRenderer.endWidth = width;
            return lineRenderer;
        }

        private static void CreateCircleRenderer(LineRenderer lineRenderer, float x, float y, float r, int segments)
        {
            lineRenderer.loop = true;
            lineRenderer.positionCount = segments;
            var delta = 2 * Mathf.PI / segments;
            for (var i = 0; i < segments; ++i)
                lineRenderer.SetPosition(i, new Vector3(x + r * Mathf.Sin(i * delta), y + r * Mathf.Cos(i * delta), 0));
        }
    }
}
