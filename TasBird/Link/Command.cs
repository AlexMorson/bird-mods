using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace TasBird.Link
{
    public abstract class Command
    {
        public static Dictionary<string, Func<NetworkStream, Command>> CommandParsers { get; } =
            new Dictionary<string, Func<NetworkStream, Command>>();

        public abstract void Execute();
    }
}
