// using System;

// namespace TinyBlockStorage.Json
// {
//     public static class JsonDatabaseExtensions
//     {
//         public static JsonDatabase<T> CreateIndexOn<T>(this JsonDatabase<T> db, string propertyName, bool duplicateKeys = false) where T : JsonDocument, new()
//         {
//             db.CreateIndexOn(propertyName, duplicateKeys);
//             return db;
//         }
//     }
// }