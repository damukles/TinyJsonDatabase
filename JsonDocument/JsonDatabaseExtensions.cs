using System;

namespace TinyBlockStorage.Json
{
    public static class JsonDatabaseExtensions
    {
        public static JsonDatabase<T> CreateIndexOn<T, I>(this JsonDatabase<T> db, string propertyName, bool duplicateKeys = false) where T : JsonDocument, new()
        {
            db.CreateIndexOn<I>(propertyName, duplicateKeys);
            return db;
        }
    }
}