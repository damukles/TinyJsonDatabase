using System;
using System.Collections.Generic;

namespace TinyBlockStorage.Blob
{
    public interface IBlobDatabase
    {
        void Insert(BlobModel blob);
        void Delete(BlobModel blob);
        void Update(BlobModel blob);
        BlobModel Find(Guid id);
        IEnumerable<BlobModel> FindBy(string fileName);
    }
}

