using System;
using System.Collections.Generic;

namespace TinyBlockStorage.Json
{
    public interface IJsonDatabase<T>
    {
        void CreateIndexOn<I>(string propertyName, bool duplicatekeys);
        // Index<I> IndexOf<I>(string propertyName);
        Guid Insert(T json);
        void Delete(T json);
        void Update(T json);
        T First<I>(string propertyName, I value);
        IEnumerable<T> Find<I>(string propertyName, I value);
        // IEnumerable<T> FindBy(string JsonName);
    }
}

