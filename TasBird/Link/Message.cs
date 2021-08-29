using System.Net.Sockets;

namespace TasBird.Link
{
    public abstract class Message
    {
        public abstract void Write(NetworkStream stream);
    }
}
