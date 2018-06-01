using System;
using System.IO;
using TinyBlockStorage.Core;

namespace TinyBlockStorage.File
{
    /// <summary>
    /// This class serializes a FileModel into byte[] for using with RecordStorage;
    /// It does not matter how you serialize the model, whenever it is XML, JSON, Protobuf or Binary serialization.
    /// </summary>
    public class FileSerializer
    {
        public byte[] Serialize(FileModel blob)
        {
            var nameBytes = System.Text.Encoding.UTF8.GetBytes(blob.FileName);
            var blobData = new byte[
                16 +                   // 16 bytes for Guid id
                4 +                    // 4 bytes indicate the length of the `name` string
                nameBytes.Length +     // z bytes for name 
                4 +                    // 4 bytes for age
                4 +                    // 4 bytes indicate length of DNA data
                blob.BlockData.Length     // y bytes of DNA data
            ];

            // Id

            Buffer.BlockCopy(
                      src: blob.Id.ToByteArray(),
                srcOffset: 0,
                      dst: blobData,
                dstOffset: 0,
                    count: 16
            );

            // Name

            Buffer.BlockCopy(

                      src: LittleEndianByteOrder.GetBytes((int)nameBytes.Length),
                srcOffset: 0,
                      dst: blobData,
                dstOffset: 16,
                    count: 4
            );

            Buffer.BlockCopy(
                      src: nameBytes,
                srcOffset: 0,
                      dst: blobData,
                dstOffset: 16 + 4,
                    count: nameBytes.Length
            );

            // LastEdited

            Buffer.BlockCopy(
                      src: LittleEndianByteOrder.GetBytes((int)blob.LastEditedUnixTimeSeconds),
                srcOffset: 0,
                      dst: blobData,
                dstOffset: 16 + 4 + nameBytes.Length,
                    count: 4
            );

            // Data

            Buffer.BlockCopy(
                      src: LittleEndianByteOrder.GetBytes(blob.BlockData.Length),
                srcOffset: 0,
                      dst: blobData,
                dstOffset: 16 + 4 + nameBytes.Length + 4,
                    count: 4
            );

            Buffer.BlockCopy(
                      src: blob.BlockData,
                srcOffset: 0,
                      dst: blobData,
                dstOffset: 16 + 4 + nameBytes.Length + 4 + 4,
                    count: blob.BlockData.Length
            );

            return blobData;
        }

        public FileModel Deserializer(byte[] data)
        {
            var blobModel = new FileModel();

            // Read id
            blobModel.Id = BufferHelper.ReadBufferGuid(data, 0);

            // Read name
            var nameLength = BufferHelper.ReadBufferInt32(data, 16);
            if (nameLength < 0 || nameLength > (16 * 1024))
            {
                throw new Exception("Invalid string length: " + nameLength);
            }
            blobModel.FileName = System.Text.Encoding.UTF8.GetString(data, 16 + 4, nameLength);

            // Read last edited
            blobModel.LastEditedUnixTimeSeconds = BufferHelper.ReadBufferInt32(data, 16 + 4 + nameLength);

            // Read block data
            var blockDataLength = BufferHelper.ReadBufferInt32(data, 16 + 4 + nameLength + 4);
            if (blockDataLength < 0 || blockDataLength > (64 * 1024))
            {
                throw new Exception("Invalid DNA data length: " + blockDataLength);
            }
            blobModel.BlockData = new byte[blockDataLength];
            Buffer.BlockCopy(src: data, srcOffset: 16 + 4 + nameLength + 4 + 4, dst: blobModel.BlockData, dstOffset: 0, count: blobModel.BlockData.Length);

            // Return constructed model
            return blobModel;
        }
    }
}

