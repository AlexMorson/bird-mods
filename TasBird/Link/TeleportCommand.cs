using System.Net.Sockets;

namespace TasBird.Link
{
    public class TeleportCommand : Command
    {
        private readonly float dx;
        private readonly float dy;

        private TeleportCommand(float dx, float dy)
        {
            this.dx = dx;
            this.dy = dy;
        }

        public override void Execute()
        {
            MasterController.GetPlayer().Position += new Coord(dx, dy);
        }

        public static void Register() => CommandParsers.Add("Teleport", Parse);
        public static void Unregister() => CommandParsers.Remove("Teleport");

        private static Command Parse(NetworkStream stream)
        {
            var dx = Util.ReadInt(stream);
            var dy = Util.ReadInt(stream);
            return new TeleportCommand(dx, dy);
        }
    }
}
