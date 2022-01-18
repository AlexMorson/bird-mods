using System.Net.Sockets;

namespace TasBird.Link
{
    public class StepFrameCommand : Command
    {
        public override void Execute()
        {
            Time.StepFrame();
        }

        public static void Register() => CommandParsers.Add("StepFrame", Parse);
        public static void Unregister() => CommandParsers.Remove("StepFrame");

        private static Command Parse(NetworkStream stream)
        {
            return new StepFrameCommand();
        }
    }
}
