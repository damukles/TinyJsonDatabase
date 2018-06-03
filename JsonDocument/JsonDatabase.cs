using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TinyBlockStorage.Core;

namespace TinyBlockStorage.Json
{
    /// <summary>
    /// Then, define our database
    /// </summary>
    public class JsonDatabase<T> : IJsonDatabase<T>, IDisposable where T : IJsonDocument, new()
    {
        readonly Type jsonType;
        readonly Stream mainDatabaseJson;
        readonly Stream primaryIndexJson;
        // readonly Stream secondaryIndexJson;
        readonly Tree<Guid, uint> primaryIndex;
        // readonly Tree<string, uint> secondaryIndex;
        readonly RecordStorage jsonRecords;
        readonly JsonSerializer<T> jsonSerializer = new JsonSerializer<T>();

        /// <summary>
        /// </summary>
        /// <param name="pathToJsonDb">Path to json db.</param>
        public JsonDatabase(string pathToJsonDb)
        {
            if (pathToJsonDb == null)
                throw new ArgumentNullException("pathToJsonDb");

            jsonType = typeof(T);

            // As soon as JsonDatabase is constructed, open the steam to talk to the underlying Jsons
            this.mainDatabaseJson = new FileStream(pathToJsonDb, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.primaryIndexJson = new FileStream(pathToJsonDb + ".pidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            // this.secondaryIndexJson = new FileStream(pathToJsonDb + ".sidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);

            // Construct the RecordStorage that use to store main json data
            this.jsonRecords = new RecordStorage(new BlockStorage(this.mainDatabaseJson, 4096, 48));

            // Construct the primary and secondary indexes 
            this.primaryIndex = new Tree<Guid, uint>(
                new TreeDiskNodeManager<Guid, uint>(
                    new GuidSerializer(),
                    new TreeUIntSerializer(),
                    new RecordStorage(new BlockStorage(this.primaryIndexJson, 4096))
                ),
                false
            );

            // this.secondaryIndex = new Tree<string, uint>(
            //     new TreeDiskNodeManager<string, uint>(
            //         new StringSerializer(),
            //         new TreeUIntSerializer(),
            //         new RecordStorage(new BlockStorage(this.secondaryIndexJson, 4096))
            //     ),
            //     true
            // );
        }

        /// <summary>
        /// Update given json
        /// </summary>
        public void Update(T json)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDatabase");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert a new json entry into our json database
        /// </summary>
        public Guid Insert(T json)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDatabase");
            }

            // Serialize the json and insert it
            var (bytes, id) = this.jsonSerializer.Serialize(json);
            var recordId = this.jsonRecords.Create(bytes);

            // Primary index
            this.primaryIndex.Insert(id, recordId);

            // Secondary index
            // this.secondaryIndex.Insert(json.JsonName, recordId);

            return id;
        }

        /// <summary>
        /// Find a json by its unique id
        /// </summary>
        public T Find(Guid jsonId)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDatabase");
            }

            // Look in the primary index for this json
            var entry = this.primaryIndex.Get(jsonId);
            if (entry == null)
            {
                return null;
            }

            return this.jsonSerializer.Deserializer(this.jsonRecords.Find(entry.Item2));
        }

        /// <summary>
        /// Find all jsons that beints to given JsonName
        /// </summary>
        // public IEnumerable<T> FindBy(string JsonName)
        // {
        //     var comparer = Comparer<string>.Default;

        //     // Use the secondary index to find this json
        //     foreach (var entry in this.secondaryIndex.LargerThanOrEqualTo(JsonName))
        //     {
        //         // As soon as we reached larger key than the key given by client, stop
        //         if (comparer.Compare(entry.Item1, JsonName) > 0)
        //         {
        //             break;
        //         }

        //         // Still in range, yield return
        //         yield return this.jsonSerializer.Deserializer(this.jsonRecords.Find(entry.Item2));
        //     }
        // }

        /// <summary>
        /// Delete specified json from our database
        /// </summary>
        public void Delete(T json)
        {
            throw new NotImplementedException();
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
                this.mainDatabaseJson.Dispose();
                // this.secondaryIndexJson.Dispose();
                this.primaryIndexJson.Dispose();
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

