using System;
using System.Collections.Generic;
using TinyJsonDatabase.Json;

namespace TinyJsonDatabase
{
    public class JsonDatabase : IDisposable
    {
        private readonly Dictionary<Type, JsonDocumentCollection> _collections;

        public JsonDatabase(Dictionary<Type, JsonDocumentCollection> collections)
        {
            _collections = collections;
        }

        public IJsonDocumentCollection<T> GetCollection<T>() where T : new()
        {
            if (_collections.TryGetValue(typeof(T), out var collection))
            {
                return (IJsonDocumentCollection<T>)collection;
            }
            throw new ArgumentOutOfRangeException("Cannot find a collection for " + typeof(T).Name);
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                foreach (var coll in _collections)
                {
                    coll.Value.Dispose();
                }
                this.disposed = true;
            }
        }

        ~JsonDatabase()
        {
            Dispose(false);
        }
        #endregion
    }
}
