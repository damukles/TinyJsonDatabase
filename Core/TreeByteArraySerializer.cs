using System;

namespace TinyBlockStorage.Core
{
    public class TreeByteArraySerializer : ISerializer<byte[]>
    {
        public byte[] Serialize(byte[] value)
        {
            var data = new byte[
                4 +
                value.Length
            ];

            Buffer.BlockCopy(
                      src: LittleEndianByteOrder.GetBytes(value.Length),
                srcOffset: 0,
                      dst: data,
                dstOffset: 0,
                    count: 4
            );

            Buffer.BlockCopy(
                      src: value,
                srcOffset: 0,
                      dst: data,
                dstOffset: 4,
                    count: value.Length
            );

            return value;
        }

        public byte[] Deserialize(byte[] buffer, int offset, int length)
        {
            var data = new byte[length];

            Buffer.BlockCopy(
                      src: buffer,
                srcOffset: offset,
                      dst: data,
                dstOffset: 0,
                    count: length
            );

            return data;
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

