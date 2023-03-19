using System.Net.Sockets;

namespace TasBird.Link
{
    public class QueueReplayCommand : Command
    {
        private readonly string replayBuffer;

        private QueueReplayCommand(string replayBuffer)
        {
            this.replayBuffer = replayBuffer;
        }

        public override void Execute() => Replay.Queue(replayBuffer);

        public static void Register() => CommandParsers.Add("QueueReplay", Parse);
        public static void Unregister() => CommandParsers.Remove("QueueReplay");

        private static Command Parse(NetworkStream stream)
        {
            var replayBuffer = Util.ReadString(stream);
            return new QueueReplayCommand(replayBuffer);
        }
    }
}
