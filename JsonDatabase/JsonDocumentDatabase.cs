using System;
using System.Collections.Generic;
using TinyBlockStorage.Json;

namespace TinyBlockStorage.JsonDatabase
{
    public class JsonDocumentDatabase
    {
        private readonly Dictionary<Type, JsonDocumentCollection> _collections;

        public JsonDocumentDatabase(Dictionary<Type, JsonDocumentCollection> collections)
        {
            _collections = collections;
        }

        public IJsonDocumentCollection<T> GetCollection<T>() where T : IJsonDocument, new()
        {
            return (IJsonDocumentCollection<T>)_collections[typeof(T)];
        }
    }
}
