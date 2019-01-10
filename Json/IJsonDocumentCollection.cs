using System;
using System.Collections.Generic;

namespace TinyBlockStorage.Json
{
    public interface IJsonDocumentCollection<T>
    {
        // void CreateIndexOn<I>(string propertyName, bool duplicatekeys);
        // Index<I> IndexOf<I>(string propertyName);
        Guid Insert(T json);
        void Update(T json);
        void Delete(Guid jsonId);
        T First<I>(string propertyName, I value);
        IEnumerable<T> Find<I>(string propertyName, I value);
        // IEnumerable<T> FindBy(string JsonName);
    }
}

