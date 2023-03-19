using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace TasBird.Link
{
    public class Link : MonoBehaviour
    {
        private readonly ConfigEntry<bool> running;
        private readonly ConfigEntry<int> port;

        private static Server server;
        public static Queue<Command> CommandQueue { get; } = new Queue<Command>();

        private Link()
        {
            var config = Plugin.Instance.Config;
            running = config.Bind("Link", "Enabled", true, "Enable the TCP Server");
            port = config.Bind("Link", "Port", 13337, "The port that the TCP Server listens on");
        }

        private void Awake()
        {
            LogCommand.Register();
            TeleportCommand.Register();
            LoadReplayCommand.Register();
            LoadReplayFromCommand.Register();
            QueueReplayCommand.Register();
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
            LoadReplayFromCommand.Unregister();
            QueueReplayCommand.Unregister();
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
