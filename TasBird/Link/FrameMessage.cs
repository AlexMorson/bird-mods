using System.Net.Sockets;

namespace TasBird.Link
{
    public class FrameMessage : Message
    {
        private readonly int frame;
        private readonly Coord position, velocity;

        private FrameMessage(int frame, Coord position, Coord velocity)
        {
            this.frame = frame;
            this.position = position;
            this.velocity = velocity;
        }

        public override void Write(NetworkStream stream)
        {
            Util.WriteString(stream, "Frame");
            Util.WriteInt(stream, frame);
            Util.WriteFloat(stream, (float)position.x);
            Util.WriteFloat(stream, (float)position.y);
            Util.WriteFloat(stream, (float)velocity.x);
            Util.WriteFloat(stream, (float)velocity.y);
        }

        public static void Register() => TasBird.Util.FrameEnd += OnFrameEnd;
        public static void Unregister() => TasBird.Util.FrameEnd -= OnFrameEnd;

        private static void OnFrameEnd()
        {
            var player = MasterController.GetPlayer();
            var message = new FrameMessage((int)Time.Frame, player.Position, player.Velocity);
            Link.SendMessage(message);
        }
    }
}
