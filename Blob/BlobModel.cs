using System;

namespace TinyBlockStorage.Blob
{
    /// <summary>
    /// Our database stores cows, first we define our Cow model
    /// </summary>
    public class BlobModel
    {
        public Guid Id
        {
            get;
            set;
        }

        public string FileName
        {
            get;
            set;
        }

        public int LastEditedUnixTimeSeconds
        {
            get;
            set;
        }

        public byte[] BlockData
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Format("[BlobModel: Id={0}, FileName={1}, LastEdited={2}, BlockData={3}]", Id, FileName, LastEditedUnixTimeSeconds, BlockData.Length + " bytes");
        }
    }
}

