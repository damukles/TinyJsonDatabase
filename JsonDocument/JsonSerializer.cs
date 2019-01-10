using System;
using Newtonsoft.Json;
using TinyBlockStorage.Core;

namespace TinyBlockStorage.Json
{
    /// <summary>
    /// This class serializes a JsonModel into byte[] for using with RecordStorage;
    /// It does not matter how you serialize the model, whenever it is XML, JSON, Protobuf or Binary serialization.
    /// </summary>
    public class JsonSerializer<T> where T : IJsonDocument, new()
    {
        public (byte[], Guid) Serialize(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            var jsonData = new byte[
                16 +                    // 16 bytes for Guid id
                4 +                     // 4 bytes indicate length of data
                bytes.Length            // y bytes of data
            ];

            var id = obj.Id.Equals(Guid.Empty) ? Guid.NewGuid() : obj.Id;

            // Id
            Buffer.BlockCopy(
                      src: id.ToByteArray(),
                srcOffset: 0,
                      dst: jsonData,
                dstOffset: 0,
                    count: 16
            );

            // Json length
            Buffer.BlockCopy(
                      src: LittleEndianByteOrder.GetBytes((int)bytes.Length),
                srcOffset: 0,
                      dst: jsonData,
                dstOffset: 16,
                    count: 4
            );

            // Json data
            Buffer.BlockCopy(
                      src: bytes,
                srcOffset: 0,
                      dst: jsonData,
                dstOffset: 16 + 4,
                    count: bytes.Length
            );

            return (jsonData, id);
        }

        public T Deserialize(byte[] data)
        {
            // Read id
            var id = BufferHelper.ReadBufferGuid(data, 0);

            // Read json length
            var dataLength = BufferHelper.ReadBufferInt32(data, 16);
            if (dataLength < 0 || dataLength > (16 * 1024))
            {
                throw new Exception("Invalid string length: " + dataLength);
            }

            // Read json data
            var jsonData = new byte[dataLength];
            Buffer.BlockCopy(src: data, srcOffset: 16 + 4, dst: jsonData, dstOffset: 0, count: jsonData.Length);

            var json = System.Text.Encoding.UTF8.GetString(jsonData);
            var obj = JsonConvert.DeserializeObject<T>(json);
            obj.Id = id;

            // Return constructed model
            return obj;
        }
    }
}

