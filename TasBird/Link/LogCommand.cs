using System.Net.Sockets;
using UnityEngine;

namespace TasBird.Link
{
    public class LogCommand : Command
    {
        private readonly string message;

        public LogCommand(string message)
        {
            this.message = message;
        }

        public override void Execute()
        {
            Debug.Log(message);
        }

        public static void Register() => CommandParsers.Add("Log", Parse);
        public static void Unregister() => CommandParsers.Remove("Log");

        private static Command Parse(NetworkStream stream)
        {
            var message = Util.ReadString(stream);
            return new LogCommand(message);
        }
    }
}
