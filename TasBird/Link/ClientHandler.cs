using System.Collections.Generic;
using System.Net.Sockets;

namespace TasBird.Link
{
    internal class ClientHandler
    {
        private volatile bool running;
        private readonly TcpClient client;
        private readonly NetworkStream stream;
        private readonly Queue<Message> messageQueue;

        public ClientHandler(TcpClient client, Queue<Message> messageQueue)
        {
            this.client = client;
            stream = client.GetStream();
            this.messageQueue = messageQueue;
        }

        public void SendMessage(Message message)
        {
            lock (messageQueue)
            {
                messageQueue.Enqueue(message);
            }
        }

        public void Run()
        {
            try
            {
                running = true;
                while (running)
                {
                    HandleCommands();

                    lock (messageQueue)
                    {
                        while (messageQueue.Count > 0)
                        {
                            messageQueue.Dequeue().Write(stream);
                        }
                    }
                }
            }
            catch (SocketException)
            {
                Log("Caught SocketException");
                Stop();
            }
        }

        private void HandleCommands()
        {
            if (!stream.DataAvailable) return;

            var command = ReadCommand();

            lock (Link.CommandQueue)
            {
                Link.CommandQueue.Enqueue(command);
            }
        }

        private static void Log(string message)
        {
            lock (Link.CommandQueue)
            {
                Link.CommandQueue.Enqueue(new LogCommand(message));
            }
        }

        private Command ReadCommand()
        {
            var type = Util.ReadString(stream);
            if (Command.CommandParsers.ContainsKey(type))
                return Command.CommandParsers[type](stream);

            // Could not parse command, drop client
            Log($"Command '{type} does not exist");
            throw new SocketException();
        }

        public void Stop()
        {
            Log("Closed connection");
            running = false;
            client.Close();
        }
    }
}
