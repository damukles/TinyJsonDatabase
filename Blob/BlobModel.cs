using System;
using System.Security.Cryptography;

namespace TinyBlockStorage.Blob
{
    /// <summary>
    /// Our database stores cows, first we define our Cow model
    /// </summary>
    public class BlobModel
    {
        public BlobModel(byte[] blockData)
        {
            BlockData = blockData;

            using (var sha256 = new SHA256CryptoServiceProvider())
            {
                Id = Convert.ToBase64String(sha256.ComputeHash(blockData));
            }
        }

        public string Id
        {
            get;
        }

        public byte[] BlockData
        {
            get;
        }

        public override string ToString()
        {
            return string.Format("[BlobModel: Id={0}, BlockData={1}]", Id, BlockData.Length + " bytes");
        }
    }
}

