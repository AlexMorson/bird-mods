using System.Net.Sockets;

namespace TasBird.Link
{
    public class LoadReplayCommand : Command
    {
        private readonly string levelName;
        private readonly string replayBuffer;
        private readonly int breakpoint;

        private LoadReplayCommand(string levelName, string replayBuffer, int breakpoint)
        {
            this.levelName = levelName;
            this.replayBuffer = replayBuffer;
            this.breakpoint = breakpoint;
        }

        public override void Execute() => Replay.Load(levelName, replayBuffer, breakpoint);

        public static void Register() => CommandParsers.Add("LoadReplay", Parse);
        public static void Unregister() => CommandParsers.Remove("LoadReplay");

        private static Command Parse(NetworkStream stream)
        {
            var levelName = Util.ReadString(stream);
            var replayBuffer = Util.ReadString(stream);
            var breakpoint = Util.ReadInt(stream);
            return new LoadReplayCommand(levelName, replayBuffer, breakpoint);
        }
    }
}
