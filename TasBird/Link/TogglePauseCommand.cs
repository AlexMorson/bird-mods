using System.Net.Sockets;

namespace TasBird.Link
{
    public class TogglePauseCommand : Command
    {
        public override void Execute()
        {
            Time.TogglePause();
        }

        public static void Register() => CommandParsers.Add("TogglePause", Parse);
        public static void Unregister() => CommandParsers.Remove("TogglePause");

        private static Command Parse(NetworkStream stream)
        {
            return new TogglePauseCommand();
        }
    }
}
