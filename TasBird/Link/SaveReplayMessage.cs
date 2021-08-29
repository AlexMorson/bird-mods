using System.Net.Sockets;

namespace TasBird.Link
{
    public class SaveReplayMessage : Message
    {
        private readonly string levelName;
        private readonly string replayBuffer;
        private readonly int currentFrame;

        private SaveReplayMessage(string levelName, string replayBuffer, int currentFrame)
        {
            this.levelName = levelName;
            this.replayBuffer = replayBuffer;
            this.currentFrame = currentFrame;
        }

        public override void Write(NetworkStream stream)
        {
            Util.WriteString(stream, "SaveReplay");
            Util.WriteString(stream, levelName);
            Util.WriteString(stream, replayBuffer);
            Util.WriteInt(stream, currentFrame);
        }

        public static void Register() => Replay.SaveReplay += OnSaveReplay;
        public static void Unregister() => Replay.SaveReplay -= OnSaveReplay;

        private static void OnSaveReplay(string levelName, string replayBuffer, int currentFrame)
        {
            var message = new SaveReplayMessage(levelName, replayBuffer, currentFrame);
            Link.SendMessage(message);
        }
    }
}
