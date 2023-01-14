using System.Net.Sockets;

namespace TasBird.Link
{
    public class LoadReplayFromCommand : Command
    {
        private readonly string levelName;
        private readonly string replayBuffer;
        private readonly int breakpoint;
        private readonly Coord startPosition;

        private LoadReplayFromCommand(string levelName, string replayBuffer, int breakpoint, Coord startPosition)
        {
            this.levelName = levelName;
            this.replayBuffer = replayBuffer;
            this.breakpoint = breakpoint;
            this.startPosition = startPosition;
        }

        public override void Execute() => Replay.Load(levelName, replayBuffer, breakpoint, startPosition);

        public static void Register() => CommandParsers.Add("LoadReplayFrom", Parse);
        public static void Unregister() => CommandParsers.Remove("LoadReplayFrom");

        private static Command Parse(NetworkStream stream)
        {
            var levelName = Util.ReadString(stream);
            var replayBuffer = Util.ReadString(stream);
            var breakpoint = Util.ReadInt(stream);
            var x = Util.ReadFloat(stream);
            var y = Util.ReadFloat(stream);
            return new LoadReplayFromCommand(levelName, replayBuffer, breakpoint, new Coord(x, y));
        }
    }
}
