using System;

namespace TinyBlockStorage.File
{
    /// <summary>
    /// Our database stores cows, first we define our Cow model
    /// </summary>
    public class FileModel
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
            return string.Format("[FileModel: Id={0}, FileName={1}, LastEdited={2}, BlockData={3}]", Id, FileName, LastEditedUnixTimeSeconds, BlockData.Length + " bytes");
        }
    }
}

