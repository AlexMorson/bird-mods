using BepInEx.Configuration;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TasBird
{
    public class InputDisplay : MonoBehaviour
    {
        private readonly ConfigEntry<bool> draw;
        private readonly ConfigEntry<float> scale;

        private readonly Dictionary<string, Texture2D> textures;

        private const int WIDTH = 416;
        private const int HEIGHT = 242;

        private InputDisplay()
        {
            draw = Plugin.Instance.Config.Bind("InputDisplay", "Draw", false);
            scale = Plugin.Instance.Config.Bind("InputDisplay", "Scale", 1.0f, new ConfigDescription("How large the input display should be", new AcceptableValueRange<float>(0.5f, 3.0f)));

            textures = new Dictionary<string, Texture2D>
            {
                ["background"] = LoadImage("background.png"),
                ["left"] = LoadImage("left.png"),
                ["right"] = LoadImage("right.png"),
                ["down"] = LoadImage("down.png"),
                ["up"] = LoadImage("up.png"),
                ["glide"] = LoadImage("glide.png"),
                ["dash"] = LoadImage("dash.png"),
                ["jump"] = LoadImage("jump.png"),
            };
        }

        private Texture2D LoadImage(string name)
        {
            var path = $"TasBird.Assets.{name}";

            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(path);

            if (stream == null)
            {
                Debug.LogWarning($"Resource {name} does not exist");
                return null;
            }

            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            var texture = new Texture2D(0, 0);
            if (!texture.LoadImage(data))
            {
                Debug.LogWarning($"Could not load image {name}");
                return null;
            }

            Debug.Log($"Loaded image {name}");
            return texture;
        }

        void OnGUI()
        {
            if (!draw.Value)
                return;

            if (!Event.current.type.Equals(EventType.Repaint))
                return;

            var input = MasterController.GetInput();
            if (input == null)
                return;

            var actualScale = scale.Value * Screen.height / 1080;
            var width = actualScale * WIDTH;
            var height = actualScale * HEIGHT;
            var rect = new Rect(Screen.width - width, Screen.height - height, width, height);

            GUI.DrawTexture(rect, textures["background"]);

            if (input.IsNegative(InputManager.Axis.X))
                GUI.DrawTexture(rect, textures["left"]);
            if (input.IsPositive(InputManager.Axis.X))
                GUI.DrawTexture(rect, textures["right"]);
            if (input.IsNegative(InputManager.Axis.Y))
                GUI.DrawTexture(rect, textures["down"]);
            if (input.IsPositive(InputManager.Axis.Y))
                GUI.DrawTexture(rect, textures["up"]);

            if (input.IsPressed(InputManager.Key.Cloak))
                GUI.DrawTexture(rect, textures["glide"]);
            if (input.IsPressed(InputManager.Key.Dash))
                GUI.DrawTexture(rect, textures["dash"]);
            if (input.IsPressed(InputManager.Key.Jump))
                GUI.DrawTexture(rect, textures["jump"]);
        }
    }
}
