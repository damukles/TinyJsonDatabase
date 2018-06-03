using System;
using System.Collections.Generic;

namespace TinyBlockStorage.Json
{
    public interface IJsonDatabase<T>
    {
        Guid Insert(T json);
        void Delete(T json);
        void Update(T json);
        T Find(Guid id);
        // IEnumerable<T> FindBy(string JsonName);
    }
}

