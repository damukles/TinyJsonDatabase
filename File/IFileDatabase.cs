using System;
using System.Collections.Generic;

namespace TinyBlockStorage.File
{
    public interface IFileDatabase
    {
        void Insert(FileModel blob);
        void Delete(FileModel blob);
        void Update(FileModel blob);
        FileModel Find(Guid id);
        IEnumerable<FileModel> FindBy(string fileName);
    }
}

