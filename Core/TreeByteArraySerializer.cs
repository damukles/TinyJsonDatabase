using System;

namespace TinyBlockStorage.Core
{
    public class TreeByteArraySerializer : ISerializer<byte[]>
    {
        public byte[] Serialize(byte[] value)
        {
            return value;
        }

        public byte[] Deserialize(byte[] buffer, int offset, int length)
        {
            return buffer;
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public int Length
        {
            get
            {
                throw new InvalidOperationException();
            }
        }
    }
}

