using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using TinyJsonDatabase.Core;

namespace TinyJsonDatabase.Json
{
    public abstract class JsonDocumentCollection : IDisposable
    {
        public abstract void Dispose();
    }

    public class JsonDocumentCollection<T> : JsonDocumentCollection, IJsonDocumentCollection<T>, IDisposable where T : IJsonDocument, new()
    {
        private readonly string pathToJsonDb;
        private readonly Stream databaseFile;
        private readonly int databaseFileBlockSize;
        private readonly RecordStorage jsonDocumentStorage;
        private readonly JsonSerializer<T> jsonSerializer = new JsonSerializer<T>();
        private readonly IndexManager<T> indexManager;

        object SyncRoot = new Object();


        /// <summary>
        /// </summary>
        /// <param name="pathToJsonDb">Path to json db.</param>
        /// <param name="indexDefinitons">PropertyName and AllowDuplicateKeys</param>
        public JsonDocumentCollection(string pathToJsonDb, IEnumerable<IndexDefinition> indexDefinitons = null)
        {
            if (pathToJsonDb == null)
                throw new ArgumentNullException("pathToJsonDb");

            this.pathToJsonDb = pathToJsonDb;
            this.databaseFileBlockSize = 4096;
            this.databaseFile = new FileStream(pathToJsonDb, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.jsonDocumentStorage = new RecordStorage(new BlockStorage(this.databaseFile, databaseFileBlockSize, 48));
            this.indexManager = new IndexManager<T>(pathToJsonDb, indexDefinitons);
            RebuildIndices(this.indexManager.IndicesToRebuild);
        }

        /// <summary>
        /// Update given json
        /// </summary>
        public void Update(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (disposed)
            {
                throw new ObjectDisposedException("JsonDocumentCollection");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert a new json entry into json document collection
        /// </summary>
        public Guid Insert(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (disposed)
            {
                throw new ObjectDisposedException("JsonDocumentCollection");
            }

            lock (SyncRoot)
            {
                // Serialize the json and insert it
                var (bytes, id) = this.jsonSerializer.Serialize(obj);
                var recordId = this.jsonDocumentStorage.Create(bytes);

                this.indexManager.Insert(obj, recordId);

                return id;
            }
        }

        /// <summary>
        /// Find the first matching json entry in the json document collection
        /// </summary>
        public T First(Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDocumentCollection");
            }

            var recordId = this.indexManager.First(propertySelector, propertyValue);
            if (!recordId.HasValue)
            {
                return default(T);
            }

            var document = this.jsonDocumentStorage.Find(recordId.Value);
            return this.jsonSerializer.Deserialize(document);
        }

        /// <summary>
        /// Find all matching json entries in the json document collection
        /// </summary>
        public IEnumerable<T> Find(Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDocumentCollection");
            }

            Func<uint, T> deserializer = (recordId) => this.jsonSerializer.Deserialize(this.jsonDocumentStorage.Find(recordId));
            return this.indexManager.Find(propertySelector, propertyValue, deserializer);
        }

        /// <summary>
        /// Delete first matching json entry in the json document collection
        /// </summary>
        public void DeleteFirst(Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDocumentCollection");
            }

            var recordId = this.indexManager.First(propertySelector, propertyValue);
            if (!recordId.HasValue)
            {
                return;
            }

            var obj = this.First(propertySelector, propertyValue);
            this.indexManager.Delete(obj);

            this.jsonDocumentStorage.Delete(recordId.Value);

        }

        /// <summary>
        /// Find all matching json entries in the json document collection
        /// </summary>
        public void Delete(Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDocumentCollection");
            }

            Func<uint, T> delete = (recordId) =>
             {
                 var jsonDocument = this.jsonSerializer.Deserialize(this.jsonDocumentStorage.Find(recordId));
                 this.jsonDocumentStorage.Delete(recordId);
                 this.indexManager.Delete(jsonDocument);
                 return default(T);
             };

            var _ = this.indexManager.Find(propertySelector, propertyValue, delete);
        }

        private void RebuildIndices(IEnumerable<string> propertyNames)
        {
            if (propertyNames == null || !propertyNames.Any())
                return;

            for (uint curRecStart = 1;
                 curRecStart < this.databaseFile.Length / this.databaseFileBlockSize;
                 curRecStart++)
            {
                var currentRecord = this.jsonDocumentStorage.Find(curRecStart);
                if (currentRecord != null)
                {
                    T obj = this.jsonSerializer.Deserialize(currentRecord);
                    this.indexManager.Insert(obj, curRecStart, propertyNames);
                }
            }
        }

        #region Dispose
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                this.databaseFile.Dispose();
                this.indexManager.Dispose();
                this.disposed = true;
            }
        }

        ~JsonDocumentCollection()
        {
            Dispose(false);
        }
        #endregion
    }
}

