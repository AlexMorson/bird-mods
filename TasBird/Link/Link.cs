using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;

namespace TasBird.Link
{
    [BepInPlugin("com.alexmorson.tasbird.link", "TasBird.Link", "1.0")]
    [BepInDependency("com.alexmorson.tasbird.invalidate", "1.0")]
    [BepInDependency("com.alexmorson.tasbird.replay", "1.0")]
    [BepInDependency("com.alexmorson.tasbird.util", "1.0")]
    public class Link : BaseUnityPlugin
    {
        private readonly ConfigEntry<bool> running;
        private readonly ConfigEntry<int> port;

        private static Server server;
        public static Queue<Command> CommandQueue { get; } = new Queue<Command>();

        private Link()
        {
            running = Config.Bind("General", "Enabled", true, "Enable the TCP Server");
            port = Config.Bind("General", "Port", 13337, "The port that the TCP Server listens on");
        }

        private void Awake()
        {
            LogCommand.Register();
            TeleportCommand.Register();
            LoadReplayCommand.Register();
            TogglePauseCommand.Register();
            StepFrameCommand.Register();

            FrameMessage.Register();
            SaveReplayMessage.Register();
        }

        private void OnDestroy()
        {
            LogCommand.Unregister();
            TeleportCommand.Unregister();
            LoadReplayCommand.Unregister();
            TogglePauseCommand.Unregister();
            StepFrameCommand.Unregister();

            FrameMessage.Unregister();
            SaveReplayMessage.Unregister();

            server?.Stop();
            server = null;
        }

        public static void SendMessage(Message message) => server?.SendMessage(message);

        private void Update()
        {
            lock (CommandQueue)
            {
                while (CommandQueue.Count > 0)
                {
                    CommandQueue.Dequeue().Execute();
                }
            }

            if (running.Value)
            {
                if (server != null && port.Value != server.Port)
                {
                    server.Stop();
                    server = null;
                }

                if (server is null)
                {
                    server = new Server(port.Value);
                    server.Start();
                }
            }
            else if (server != null)
            {
                server.Stop();
                server = null;
            }
        }
    }
}
