using System;
using System.Net.Sockets;
using System.Text;

namespace TasBird.Link
{
    public static class Util
    {
        public static byte[] ReadBytes(NetworkStream stream, int count)
        {
            var read = 0;
            var bytes = new byte[count];

            while (read < count)
            {
                var n = stream.Read(bytes, read, count - read);
                if (n == 0) throw new SocketException();
                read += n;
            }

            return bytes;
        }

        public static int ReadInt(NetworkStream stream)
        {
            var bytes = ReadBytes(stream, 4);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static float ReadFloat(NetworkStream stream)
        {
            var bytes = ReadBytes(stream, 4);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static string ReadString(NetworkStream stream)
        {
            var size = ReadInt(stream);
            var data = ReadBytes(stream, size);
            return Encoding.UTF8.GetString(data);
        }

        public static void WriteBytes(NetworkStream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteFloat(NetworkStream stream, float f)
        {
            var bytes = BitConverter.GetBytes(f);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            WriteBytes(stream, bytes);
        }

        public static void WriteInt(NetworkStream stream, int n)
        {
            var bytes = BitConverter.GetBytes(n);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            WriteBytes(stream, bytes);
        }

        public static void WriteString(NetworkStream stream, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            WriteInt(stream, bytes.Length);
            WriteBytes(stream, bytes);
        }
    }
}
