using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TinyBlockStorage.Core;
using System.Reflection;
using System.Threading.Tasks;

namespace TinyBlockStorage.Json
{
    /// <summary>
    /// Then, define our database
    /// </summary>
    public class JsonDatabase<T> : IJsonDatabase<T>, IDisposable where T : JsonDocument, new()
    {
        // readonly Type jsonType;
        readonly Stream mainDatabaseFile;
        readonly Stream primaryIndexFile;
        readonly Stream secondaryIndexFile;
        readonly Tree<Guid, uint> primaryIndex;
        readonly Tree<string, uint> secondaryIndex;
        readonly RecordStorage jsonRecordStorage;
        readonly JsonSerializer<T> jsonSerializer = new JsonSerializer<T>();

        object SyncRoot = new Object();


        /// <summary>
        /// </summary>
        /// <param name="pathToJsonDb">Path to json db.</param>
        public JsonDatabase(string pathToJsonDb)
        {
            if (pathToJsonDb == null)
                throw new ArgumentNullException("pathToJsonDb");

            // jsonType = typeof(T);

            // var pk = jsonType.GetProperties().Where(p => p.IsDefined(typeof(PrimaryKeyAttribute), false)).SingleOrDefault();
            // if (pk == default(PropertyInfo))
            //     throw new InvalidOperationException("No primary key found, apply the PrimaryKeyAttribute.");

            // var treeType = typeof(Tree<,>);
            // var typeArgs = new Type[] { pk.PropertyType, typeof(uint) };
            // var indexType = treeType.MakeGenericType(typeArgs);

            // var nodeMgrType = typeof(TreeDiskNodeManager<,>);
            // // nicht sehr generisch, hä
            // var nodeMgrInstArgs = new object[] {
            //     new GuidSerializer(),
            //     new TreeUIntSerializer(),
            //     new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))
            // };
            // var nodeMgrInst = Activator.CreateInstance(nodeMgrType, nodeMgrInstArgs);

            // var indexArgs = new object[] { nodeMgrInst, false };
            // var indexInst = Activator.CreateInstance(indexType, indexArgs);

            // As soon as JsonDatabase is constructed, open the steam to talk to the underlying Jsons
            this.mainDatabaseFile = new FileStream(pathToJsonDb, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.primaryIndexFile = new FileStream(pathToJsonDb + ".pidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.secondaryIndexFile = new FileStream(pathToJsonDb + ".sidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);

            // Construct the RecordStorage that use to store main json data
            this.jsonRecordStorage = new RecordStorage(new BlockStorage(this.mainDatabaseFile, 4096, 48));

            // Construct the primary and secondary indexes 
            this.primaryIndex = new Tree<Guid, uint>(
                new TreeDiskNodeManager<Guid, uint>(
                    new GuidSerializer(),
                    new TreeUIntSerializer(),
                    new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))
                ),
                false
            );

            this.secondaryIndex = new Tree<string, uint>(
                new TreeDiskNodeManager<string, uint>(
                    new StringSerializer(),
                    new TreeUIntSerializer(),
                    new RecordStorage(new BlockStorage(this.secondaryIndexFile, 4096))
                ),
                true
            );
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

            lock (SyncRoot)
            {
                // Serialize the json and insert it
                var (bytes, id) = this.jsonSerializer.Serialize(json);
                var recordId = this.jsonRecordStorage.Create(bytes);

                // Primary index
                this.primaryIndex.Insert(id, recordId);

                // Secondary index
                this.secondaryIndex.Insert(json.Name, recordId);
                return id;
            }
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

            return this.jsonSerializer.Deserializer(this.jsonRecordStorage.Find(entry.Item2));
        }

        /// <summary>
        /// Find all jsons that beints to given JsonName
        /// </summary>
        public IEnumerable<T> FindByName(string name, bool fullMatchesOnly = true)
        {
            var comparer = Comparer<string>.Default;

            // Use the secondary index to find this json
            foreach (var entry in this.secondaryIndex.LargerThanOrEqualTo(name))
            {
                // As soon as we reached larger key than the key given by client, stop
                if (comparer.Compare(entry.Item1, name) > 0)
                {
                    if (fullMatchesOnly)
                    {
                        break;
                    }
                    else
                    {
                        if (!entry.Item1.StartsWith(name))
                        {
                            break;
                        }
                    }
                }

                // Still in range, yield return
                yield return this.jsonSerializer.Deserializer(this.jsonRecordStorage.Find(entry.Item2));
            }
        }

        public IEnumerable<T> FindByName(Func<string, bool> func)
        {
            foreach (var entry in this.secondaryIndex.All())
            {
                if (func(entry.Item1))
                {
                    yield return this.jsonSerializer.Deserializer(this.jsonRecordStorage.Find(entry.Item2));
                }
            }
        }

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
                this.mainDatabaseFile.Dispose();
                this.secondaryIndexFile.Dispose();
                this.primaryIndexFile.Dispose();
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

