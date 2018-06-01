using System;
using System.IO;
using System.Text;
using TinyBlockStorage.Core;

namespace TinyBlockStorage.Blob
{
    /// <summary>
    /// This class serializes a BlobModel into byte[] for using with RecordStorage;
    /// It does not matter how you serialize the model, whenever it is XML, JSON, Protobuf or Binary serialization.
    /// </summary>
    public class BlobSerializer
    {
        public byte[] Serialize(BlobModel blob)
        {
            var blobData = new byte[
                4 +                    // 4 bytes indicate length of DNA data
                blob.BlockData.Length  // y bytes of DNA data
            ];

            // Data

            Buffer.BlockCopy(
                      src: LittleEndianByteOrder.GetBytes(blob.BlockData.Length),
                srcOffset: 0,
                      dst: blobData,
                dstOffset: 0,
                    count: 4
            );

            Buffer.BlockCopy(
                      src: blob.BlockData,
                srcOffset: 0,
                      dst: blobData,
                dstOffset: 4,
                    count: blob.BlockData.Length
            );

            return blobData;
        }

        public BlobModel Deserializer(byte[] data)
        {
            // Read block data
            var blockDataLength = BufferHelper.ReadBufferInt32(data, 0);
            if (blockDataLength < 0 || blockDataLength > (64 * 1024))
            {
                throw new Exception("Invalid DNA data length: " + blockDataLength);
            }
            var blockData = new byte[blockDataLength];
            Buffer.BlockCopy(src: data, srcOffset: 4, dst: blockData, dstOffset: 0, count: blockData.Length);

            // Return constructed model
            return new BlobModel(blockData);
        }
    }
}

