using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TasBird.Link
{
    internal class Server
    {
        public int Port { get; }

        private volatile bool running;
        private readonly TcpListener listener;
        private readonly List<ClientHandler> clientHandlers = new List<ClientHandler>();

        public Server(int port)
        {
            Port = port;
            listener = new TcpListener(IPAddress.Loopback, port);
        }

        public void SendMessage(Message message)
        {
            lock (clientHandlers)
            {
                foreach (var clientHandler in clientHandlers)
                {
                    clientHandler.SendMessage(message);
                }
            }
        }

        public void Start()
        {
            new Thread(Run).Start();
        }

        private void Run()
        {
            running = true;
            listener.Start();
            while (running)
            {
                var tcpClient = listener.AcceptTcpClient();
                lock (clientHandlers)
                {
                    if (!running) break;
                    var messageQueue = new Queue<Message>();
                    var clientHandler = new ClientHandler(tcpClient, messageQueue);
                    clientHandlers.Add(clientHandler);
                    new Thread(() =>
                    {
                        clientHandler.Run();
                        lock (clientHandlers) clientHandlers.Remove(clientHandler);
                    }).Start();
                }
            }
        }

        public void Stop()
        {
            lock (clientHandlers)
            {
                running = false;
                listener.Stop();
                foreach (var clientHandler in clientHandlers)
                {
                    clientHandler.Stop();
                }
            }
        }
    }
}
