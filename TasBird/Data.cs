using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TasBird
{
    public class Data : MonoBehaviour
    {
        private readonly ConfigEntry<bool> minimalMode;
        private readonly ConfigEntry<bool> drawCageZones;
        private readonly ConfigEntry<bool> drawCamera;
        private readonly ConfigEntry<bool> drawCheckpoints;
        private readonly ConfigEntry<bool> drawCloakData;
        private readonly ConfigEntry<bool> drawCollectables;
        private readonly ConfigEntry<bool> drawDeathzones;
        private readonly ConfigEntry<bool> drawDebugData;
        private readonly ConfigEntry<bool> drawEndPoints;
        private readonly ConfigEntry<bool> drawHitbox;
        private readonly ConfigEntry<bool> drawLastDash;
        private readonly ConfigEntry<bool> drawOptimalAngles;
        private readonly ConfigEntry<bool> drawSurfaces;

        private static readonly GUIStyle BgStyle = new GUIStyle { fontSize = 20, normal = { textColor = Color.black } };
        private static readonly GUIStyle FgStyle = new GUIStyle { fontSize = 20, normal = { textColor = Color.white } };

        private static readonly Material Material = new Material(Shader.Find("Sprites/Default"));
        private static readonly Texture2D Texture = new Texture2D(1, 1);

        private LineRenderer lastDash;
        private LineRenderer optimalLeft, optimalRight;
        private LineRenderer cloakPower, cloakTimer;
        private LineRenderer cloakPowerBack, cloakTimerBack;
        private LineRenderer leftZone, rightZone;
        private readonly List<CameraZone> cameraZones = new List<CameraZone>();
        private readonly List<LineRenderer> cageZones = new List<LineRenderer>();
        private readonly List<LineRenderer> cameraLinks = new List<LineRenderer>();
        private readonly List<LineRenderer> collectables = new List<LineRenderer>();
        private readonly List<LineRenderer> surfaces = new List<LineRenderer>();
        private readonly List<Rectangle> deathZones = new List<Rectangle>();
        private readonly List<Rectangle> checkpoints = new List<Rectangle>();
        private readonly List<Rectangle> endPoints = new List<Rectangle>();

        private Data()
        {
            drawCageZones = Plugin.Instance.Config.Bind("Data", "DrawCageZones", true, "Draw the activation circles at the start and end of caged levels");
            drawCamera = Plugin.Instance.Config.Bind("Data", "DrawCamera", false, "Draw the camera zones with offsets, and links between them");
            drawCheckpoints = Plugin.Instance.Config.Bind("Data", "DrawCheckpoints", true, "Draw the checkpoint activation zones");
            drawCloakData = Plugin.Instance.Config.Bind("Data", "DrawCloakData", true, "Draw bars representing the cloak power and timer, and the left and right cloak zones");
            drawCollectables = Plugin.Instance.Config.Bind("Data", "DrawCollectables", true, "Draw the collection zone for birds and totems");
            drawDeathzones = Plugin.Instance.Config.Bind("Data", "DrawDeathzones", true, "Draw the deathzones");
            drawDebugData = Plugin.Instance.Config.Bind("Data", "DrawDebugData", true, "Draw debug data");
            drawEndPoints = Plugin.Instance.Config.Bind("Data", "DrawEndPoints", true, "Draw the activation zones for doors");
            drawHitbox = Plugin.Instance.Config.Bind("Data", "DrawHitbox", true, "Draw Quill's hitbox");
            drawLastDash = Plugin.Instance.Config.Bind("Data", "DrawLastDash", true, "Draw the wall or ceiling that was last dashed on");
            drawOptimalAngles = Plugin.Instance.Config.Bind("Data", "DrawOptimalAngles", true, "Draw the directions to press for optimal turning speed");
            drawSurfaces = Plugin.Instance.Config.Bind("Data", "DrawSurfaces", true, "Draw the exact boundary of walls, floors and ceilings");
            minimalMode = Plugin.Instance.Config.Bind("Data", "MinimalMode", false, "Turn off all background art and details");

            drawCageZones.SettingChanged += (sender, e) => ToggleCageZones(drawCageZones.Value);
            drawCamera.SettingChanged += (sender, e) => ToggleCamera(drawCamera.Value);
            drawCloakData.SettingChanged += (sender, e) => DrawCloakData(drawCloakData.Value);
            drawCollectables.SettingChanged += (sender, e) => ToggleCollectables(drawCollectables.Value);
            drawLastDash.SettingChanged += (sender, e) => DrawLastDash(drawLastDash.Value);
            drawOptimalAngles.SettingChanged += (sender, e) => DrawOptimalAngles(drawOptimalAngles.Value);
            drawSurfaces.SettingChanged += (sender, e) => ToggleSurfaces(drawSurfaces.Value);
            minimalMode.SettingChanged += (sender, e) => ToggleMinimalMode(minimalMode.Value);
        }

        private void Awake()
        {
            if (SceneManager.GetActiveScene() != LevelManager.ManagementScene)
                OnNewSceneLoaded();

            Util.LevelStart += OnLevelStart;
            Util.PlayerUpdate += OnPlayerUpdate;

            if (lastDash is null)
                lastDash = CreateLineRenderer(Color.red, 4, 1);

            if (optimalLeft is null)
                optimalLeft = CreateLineRenderer(Color.cyan, 4, 4);

            if (optimalRight is null)
                optimalRight = CreateLineRenderer(Color.cyan, 4, 4);

            if (cloakPower is null)
                cloakPower = CreateLineRenderer(Color.cyan, 4, 4);

            if (cloakTimer is null)
                cloakTimer = CreateLineRenderer(Color.magenta, 4, 4);

            if (cloakPowerBack is null)
                cloakPowerBack = CreateLineRenderer(Color.black, 4, 3);

            if (cloakTimerBack is null)
                cloakTimerBack = CreateLineRenderer(Color.black, 4, 3);

            if (leftZone is null)
                leftZone = CreateLineRenderer(Color.blue, 2, 2);

            if (rightZone is null)
                rightZone = CreateLineRenderer(Color.red, 2, 2);
        }

        private void OnDestroy()
        {
            Util.LevelStart -= OnLevelStart;
            Util.PlayerUpdate -= OnPlayerUpdate;

            if (minimalMode.Value)
                ToggleMinimalMode(false);
            ToggleSurfaces(false);
            ToggleCageZones(false);
            ToggleCamera(false);
            ToggleCollectables(false);
        }

        private void OnPlayerUpdate(int frame)
        {
            DrawCloakData(drawCloakData.Value);
            DrawLastDash(drawLastDash.Value);
            DrawOptimalAngles(drawOptimalAngles.Value);
        }

        private void OnLevelStart(bool newScene)
        {
            if (newScene)
                OnNewSceneLoaded();
        }

        private void OnNewSceneLoaded()
        {
            if (minimalMode.Value)
                ToggleMinimalMode(true);

            deathZones.Clear();
            foreach (var deathZone in MasterController.GetObjects().GetObjects<DeathZone>())
            {
                if (deathZone is InvertedDeathZone)
                {
                    var x1 = deathZone.GridHitbox.X1 - 16;
                    var x2 = deathZone.GridHitbox.X2 + 16;
                    var y1 = deathZone.GridHitbox.Y1 - 64;
                    var y2 = deathZone.GridHitbox.Y2 + 64;
                    deathZones.Add(new Rectangle(new Coord(-1e6, -1e6), new Coord(1e6, y1)));
                    deathZones.Add(new Rectangle(new Coord(-1e6, y2), new Coord(1e6, 1e6)));
                    deathZones.Add(new Rectangle(new Coord(-1e6, y1), new Coord(x1, y2)));
                    deathZones.Add(new Rectangle(new Coord(x2, y1), new Coord(1e6, y2)));
                }
                else if (deathZone.kind == 0)
                {
                    deathZones.Add(deathZone.GridHitbox);
                }
            }

            checkpoints.Clear();
            foreach (var checkpoint in MasterController.GetObjects().GetObjects<Checkpoint>())
                checkpoints.Add(checkpoint.GridHitbox);

            endPoints.Clear();
            foreach (var endPoint in MasterController.GetObjects().GetObjects<EndPoint>())
                endPoints.Add(endPoint.GridHitbox);

            ToggleSurfaces(drawSurfaces.Value);
            ToggleCageZones(drawCageZones.Value);
            ToggleCamera(drawCamera.Value);
            ToggleCollectables(drawCollectables.Value);
        }

        private void OnGUI()
        {
            var player = MasterController.GetPlayer();
            if (player is null) return;

            if (drawHitbox.Value)
            {
                DrawBounds(player.Hitbox.Bounds, new Color(0, 1, 0, 0.5f));
                DrawBounds(new Rectangle(player.Hitbox.Center, 4, 4), new Color(1, 1, 1, 1));
            }

            if (drawDeathzones.Value)
                foreach (var deathZone in deathZones)
                    DrawBounds(deathZone, new Color(1, 0, 0, 0.5f));

            if (drawCamera.Value)
            {
                foreach (var zone in cameraZones)
                {
                    DrawBounds(zone.GridHitbox, new Color(0, 0, 1, 0.5f));
                    DrawBounds(zone.Offset, new Color(0, 1, 1, 0.2f));
                }
            }

            if (drawCheckpoints.Value)
                foreach (var checkpoint in checkpoints)
                    DrawBounds(checkpoint, new Color(1, 1, 0, 0.3f));

            if (drawEndPoints.Value)
                foreach (var endPoint in endPoints)
                    DrawBounds(endPoint, new Color(0, 1, 0, 0.3f));

            if (drawDebugData.Value)
            {
                var text = $@"Frame: {player.framesInLevel}
Time Scale: {Time.Multiplier:0.00}
Pos: {player.Position.x:0.00}, {player.Position.y:0.00}
Vel: {player.Velocity.x:0.00}, {player.Velocity.y:0.00}
Speed: {player.Velocity.Length:0.00} at {Math.Atan2(player.Velocity.y, player.Velocity.x) * 180 / Math.PI:0.0}°
Cloak: {player.timers.Get(Player.Timers.Cloak):0}, {Math.Round(player.cloak.power * 45.0):0}
Contact Angle: {(player.Contact.Exists ? $"{(float)player.Contact.Angle:0.0}°" : "None")}";

                var timersText = "Timers:\n";
                foreach (Player.Timers timer in Enum.GetValues(typeof(Player.Timers)))
                    if (timer != Player.Timers.CanWind)
                        timersText += $"{timer}: {player.timers.Get(timer)}\n";

                DrawText(text, 10, 10);
                DrawText(timersText, 10, 200);
            }
        }

        private void DrawLastDash(bool on)
        {
            lastDash.positionCount = 0;
            if (!on) return;

            var player = MasterController.GetPlayer();
            if (player is null) return;
            if (!player.lastDash.Exists) return;

            var points = ConnectedVectorsOfSameKind(player.lastDash).ToArray();
            lastDash.positionCount = points.Length;
            lastDash.SetPositions(points);
        }

        private void DrawCloakData(bool on)
        {
            cloakPower.positionCount = 0;
            cloakTimer.positionCount = 0;
            cloakPowerBack.positionCount = 0;
            cloakTimerBack.positionCount = 0;
            leftZone.positionCount = 0;
            rightZone.positionCount = 0;
            if (!on) return;

            var player = MasterController.GetPlayer();
            if (player is null) return;

            var power = (float)player.cloak.power;
            var timer = Mathf.Max(0f, player.timers.Get(Player.Timers.Cloak)) / 45;

            const float width = 80;
            var start = player.Position.V3 + new Vector3(-width / 2, 50);

            cloakPower.positionCount = 2;
            cloakPower.SetPosition(0, start);
            cloakPower.SetPosition(1, start + width * power * Vector3.right);
            cloakPowerBack.positionCount = 2;
            cloakPowerBack.SetPosition(0, start);
            cloakPowerBack.SetPosition(1, start + width * Vector3.right);

            start += new Vector3(0, 8);

            cloakTimer.positionCount = 2;
            cloakTimer.SetPosition(0, start);
            cloakTimer.SetPosition(1, start + width * timer * Vector3.right);
            cloakTimerBack.positionCount = 2;
            cloakTimerBack.SetPosition(0, start);
            cloakTimerBack.SetPosition(1, start + width * Vector3.right);

            var left = (float)player.cloak.leftZone;
            var right = (float)player.cloak.rightZone;
            var top = Camera.Position.y + 1e6f;
            var bottom = Camera.Position.y - 1e6f;

            leftZone.positionCount = 2;
            leftZone.SetPosition(0, new Vector3(left, bottom));
            leftZone.SetPosition(1, new Vector3(left, top));

            rightZone.positionCount = 2;
            rightZone.SetPosition(0, new Vector3(right, bottom));
            rightZone.SetPosition(1, new Vector3(right, top));

        }

        private static void ToggleMinimalMode(bool on)
        {
            foreach (var layer in LayerManager.Manager.Layers)
            {
                if (layer.name == "PlayerLayer")
                    layer.ToggleArtVisibility(!on);
                else
                    layer.ToggleLayerVisibility(!on);
            }
        }

        private void ToggleSurfaces(bool on)
        {
            // Destroy old renderers
            foreach (var renderer in surfaces)
            {
                if (renderer is null) continue;
                Destroy(renderer.gameObject);
            }
            surfaces.Clear();

            // Create new renderers
            if (!on) return;
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

                var lineRenderer = CreateLineRenderer(Color.white, 4, 0);

                if (current.Exists)
                    lineRenderer.loop = true;
                else
                    points.Insert(0, vector.Start.V3);

                lineRenderer.positionCount = points.Count;
                lineRenderer.SetPositions(points.ToArray());
                surfaces.Add(lineRenderer);
            }
        }

        private void ToggleCamera(bool on)
        {
            // Destroy old renderers
            cameraZones.Clear();
            foreach (var renderer in cameraLinks)
            {
                if (renderer is null) continue;
                Destroy(renderer.gameObject);
            }
            cameraLinks.Clear();

            // Create new renderers
            if (!on) return;

            foreach (var zone in MasterController.GetObjects().GetObjects<CameraZone>())
                cameraZones.Add(zone);

            var linkColour = new Color(0, 0, 1, 0.5f);
            var offsetColour = new Color(0, 1, 1, 0.5f);
            foreach (var zone in cameraZones)
            {
                var hb = zone.GridHitbox;
                var right = zone.Get(StitchableBox.Side.Right);
                if (right != null)
                {
                    cameraLinks.Add(CreateLineRenderer(linkColour, 6, 0, hb.B, right.GridHitbox.UL));
                    cameraLinks.Add(CreateLineRenderer(linkColour, 6, 0, hb.LR, right.GridHitbox.A));
                }
                var down = zone.Get(StitchableBox.Side.Down);
                if (down != null)
                {
                    cameraLinks.Add(CreateLineRenderer(linkColour, 6, 0, hb.A, down.GridHitbox.UL));
                    cameraLinks.Add(CreateLineRenderer(linkColour, 6, 0, hb.LR, down.GridHitbox.B));
                }

                cameraLinks.Add(CreateLineRenderer(offsetColour, 6, 0, hb.A, hb.Center + hb.Dimensions * zone.multiplier * new Coord(-0.5, -0.5) + zone.offset));
                cameraLinks.Add(CreateLineRenderer(offsetColour, 6, 0, hb.B, hb.Center + hb.Dimensions * zone.multiplier * new Coord(0.5, 0.5) + zone.offset));
                cameraLinks.Add(CreateLineRenderer(offsetColour, 6, 0, hb.LR, hb.Center + hb.Dimensions * zone.multiplier * new Coord(0.5, -0.5) + zone.offset));
                cameraLinks.Add(CreateLineRenderer(offsetColour, 6, 0, hb.UL, hb.Center + hb.Dimensions * zone.multiplier * new Coord(-0.5, 0.5) + zone.offset));
            }
        }

        private void ToggleCollectables(bool on)
        {
            // Destroy old renderers
            foreach (var renderer in collectables)
            {
                if (renderer is null) continue;
                Destroy(renderer.gameObject);
            }

            collectables.Clear();

            // Create new renderers
            if (!on) return;

            foreach (var bird in MasterController.GetObjects().GetObjects<Collectibird>())
            {
                var lineRenderer = CreateLineRenderer(Color.white, 2, 0);
                CreateCircleRenderer(lineRenderer, (float)bird.spawn.x, (float)bird.spawn.y, 64, 30);
                collectables.Add(lineRenderer);
            }

            foreach (var totem in MasterController.GetObjects().GetObjects<Totem>())
            {
                var lineRenderer = CreateLineRenderer(Color.white, 2, 0);
                var position = totem.transform.position;
                CreateCircleRenderer(lineRenderer, position.x, position.y, 96, 30);
                collectables.Add(lineRenderer);
            }
        }

        private void ToggleCageZones(bool on)
        {
            // Destroy old renderers
            foreach (var renderer in cageZones)
            {
                if (renderer is null) continue;
                Destroy(renderer.gameObject);
            }

            cageZones.Clear();

            // Create new renderers
            if (!on) return;

            foreach (var cage in MasterController.GetObjects().GetObjects<CageZone>())
            {
                var start = cage.Beginning;
                var end = cage.End;
                var lineRenderer = CreateLineRenderer(Color.white, 2, 0);
                var position = start.transform.position;
                CreateCircleRenderer(lineRenderer, position.x, position.y, start.radius, 30);
                cageZones.Add(lineRenderer);
                lineRenderer = CreateLineRenderer(Color.white, 2, 0);
                position = end.transform.position;
                CreateCircleRenderer(lineRenderer, position.x, position.y, end.radius, 30);
                cageZones.Add(lineRenderer);
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

        private LineRenderer CreateLineRenderer(Color color, float width, int order)
        {
            var child = new GameObject();
            child.transform.parent = gameObject.transform;
            var lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.sortingLayerName = "Foreground";
            lineRenderer.sortingOrder = 10000 + order;
            lineRenderer.material = Material;
            lineRenderer.startColor = lineRenderer.endColor = color;
            lineRenderer.startWidth = lineRenderer.endWidth = width;
            return lineRenderer;
        }

        private LineRenderer CreateLineRenderer(Color color, float width, int order, Coord from, Coord to)
        {
            var lineRenderer = CreateLineRenderer(color, width, order);
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, from.V3);
            lineRenderer.SetPosition(1, to.V3);
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

        private void DrawOptimalAngles(bool on)
        {
            optimalLeft.positionCount = 0;
            optimalRight.positionCount = 0;
            if (!on) return;

            var player = MasterController.GetPlayer();
            if (player.Contact.Exists || player.Velocity.IsZero)
                return;

            var velocity = player.Velocity;
            var originalVelocityAngle = new Angle(velocity);

            // Gravity
            velocity += player.Gravity;

            // Drag
            velocity.x = Math.Sign(velocity.x) *
                         Math.Sqrt(Math.Max(
                             velocity.x * velocity.x - Calc.Clamp((Math.Abs(velocity.x) - 6.0) * 0.5, 0.0, 1.0), 0.0));

            // Test the 8 input angles to see which deflect the velocity most to the left/right
            // If there are ties, choose the angles closest to the current velocity angle

            var velocityAngle = new Angle(velocity);

            var leftAngle = new Angle();
            var leftDeflection = -1e10;
            var rightAngle = new Angle();
            var rightDeflection = 1e10;

            for (double input = -180; input < 180; input += 45)
            {
                // In the game there is an extra (linear) dependency on cloak power that is used in the calculation
                // for deflection but we only care which input gives the greatest deflection so this can be ignored.

                var inputAngle = new Angle(input);
                var clampedAngle = inputAngle;
                if (clampedAngle != velocityAngle.Flip)
                {
                    clampedAngle = Calc.Clamp(clampedAngle - velocityAngle, -90.0, 90.0) + velocityAngle;
                }

                double correlation = clampedAngle.Dot(velocityAngle) * 0.5 + 0.5;
                var deflection = velocityAngle.Lerp(velocityAngle.Lerp(clampedAngle, correlation),
                    Calc.Clamp(velocity.Length / 200, 0.0, 0.3 * correlation)) - velocityAngle;

                if (deflection > leftDeflection ||
                    deflection == leftDeflection &&
                    Math.Abs(originalVelocityAngle - leftAngle) >
                    Math.Abs(originalVelocityAngle - inputAngle))
                {
                    leftAngle = inputAngle;
                    leftDeflection = deflection;
                }

                if (deflection < rightDeflection ||
                    deflection == rightDeflection &&
                    Math.Abs(originalVelocityAngle - rightAngle) >
                    Math.Abs(originalVelocityAngle - inputAngle))
                {
                    rightAngle = inputAngle;
                    rightDeflection = deflection;
                }
            }

            // Update the LineRenderers

            optimalLeft.positionCount = 2;
            optimalLeft.SetPosition(0, player.Position.V3);
            optimalLeft.SetPosition(1, (player.Position + new Coord(leftAngle, 40)).V3);

            optimalRight.positionCount = 2;
            optimalRight.SetPosition(0, player.Position.V3);
            optimalRight.SetPosition(1, (player.Position + new Coord(rightAngle, 40)).V3);
        }
    }
}
