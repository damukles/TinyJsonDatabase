using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TinyBlockStorage.Core;
using System.Reflection;

namespace TinyBlockStorage.Json
{
    /// <summary>
    /// Then, define our database
    /// </summary>
    public class JsonDatabase<T> : IJsonDatabase<T>, IDisposable where T : JsonDocument, new()
    {
        private readonly string pathToJsonDb;
        // readonly Type jsonType;
        readonly Stream mainDatabaseFile;
        readonly Stream primaryIndexFile;
        // readonly Stream secondaryIndexFile;
        readonly Tree<Guid, uint> primaryIndex;
        // readonly Tree<string, uint> secondaryIndex;
        readonly RecordStorage jsonRecordStorage;
        readonly JsonSerializer<T> jsonSerializer = new JsonSerializer<T>();

        // private Dictionary<Tuple<string, Type>, object> dbIndices = new Dictionary<Tuple<string, Type>, object>();
        private Dictionary<string, Stream> dbIndexFiles = new Dictionary<string, Stream>();
        private Dictionary<string, IndexTree> dbIndices = new Dictionary<string, IndexTree>();

        private Comparer<byte[]> ByteArrayComparer = Comparer<byte[]>.Create((a, b) => a.CompareTo(b));

        object SyncRoot = new Object();


        /// <summary>
        /// </summary>
        /// <param name="pathToJsonDb">Path to json db.</param>
        public JsonDatabase(string pathToJsonDb)
        {
            if (pathToJsonDb == null)
                throw new ArgumentNullException("pathToJsonDb");

            this.pathToJsonDb = pathToJsonDb;

            // As soon as JsonDatabase is constructed, open the steam to talk to the underlying Jsons
            this.mainDatabaseFile = new FileStream(pathToJsonDb, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.primaryIndexFile = new FileStream(pathToJsonDb + ".pidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            // this.secondaryIndexFile = new FileStream(pathToJsonDb + ".sidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);

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

            // this.secondaryIndex = new Tree<string, uint>(
            //     new TreeDiskNodeManager<string, uint>(
            //         new StringSerializer(),
            //         new TreeUIntSerializer(),
            //         new RecordStorage(new BlockStorage(this.secondaryIndexFile, 4096))
            //     ),
            //     true
            // );
        }

        public void CreateIndexOn<I>(string propertyName, bool duplicateKeys = false)
        {
            // Create Serializer dynamically
            var serializerImpl = Assembly
                .GetAssembly(typeof(JsonDatabase<>))
                .GetTypes()
                .Where(t => typeof(ISerializer<I>).IsAssignableFrom(t) && !t.IsInterface).SingleOrDefault();

            if (serializerImpl == null)
                throw new InvalidOperationException($"There is no Serializer for type {typeof(I).Name}, cannot create an Index on field {propertyName}.");

            var serializerInstance = (ISerializer<I>)Activator.CreateInstance(serializerImpl);

            // Create Db file
            var dbFile = new FileStream(this.pathToJsonDb + "." + propertyName + ".idx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            dbIndexFiles.Add(propertyName, dbFile);

            dbIndices.Add(propertyName,
                new IndexTree(
                    new TreeDiskNodeManager<byte[], uint>(
                        new TreeByteArraySerializer(),
                        new TreeUIntSerializer(),
                        new RecordStorage(new BlockStorage(dbFile, 4096)),
                        ByteArrayComparer
                    ),
                    duplicateKeys
                )
            );
        }

        private IndexTree IndexOf(string propertyName)
        {
            IndexTree value;
            dbIndices.TryGetValue(propertyName, out value);
            return value;

            // if (dbIndices.TryGetValue(propertyName, out value))
            // {
            //     return value;
            // }
            // else
            // {
            //     return default(IndexTree);
            // };
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

                // Secondary Indices
                foreach (var idx in this.dbIndices)
                {
                    var propName = idx.Key;
                    var jsonProp = json
                        .GetType()
                        .GetProperties()
                        .Where(p => p.Name.Equals(propName))
                        .SingleOrDefault();

                    if (jsonProp != default(PropertyInfo))
                    {
                        IndexTree indexToInsertInto = idx.Value;
                        object value = jsonProp.GetValue(json);
                        byte[] byteValue = GetBytes(value, jsonProp.PropertyType);
                        indexToInsertInto.Insert(byteValue, recordId);
                    }
                }
                // Secondary index
                // this.secondaryIndex.Insert(json.Name, recordId);
                return id;
            }
        }

        private byte[] GetBytes<I>(I value)
        {
            return GetBytes((object)value, typeof(I));
        }

        private byte[] GetBytes(object value, Type valueType)
        {
            if (valueType == typeof(int)) return LittleEndianByteOrder.GetBytes((int)value);
            if (valueType == typeof(long)) return LittleEndianByteOrder.GetBytes((long)value);
            if (valueType == typeof(uint)) return LittleEndianByteOrder.GetBytes((uint)value);
            if (valueType == typeof(float)) return LittleEndianByteOrder.GetBytes((float)value);
            if (valueType == typeof(double)) return LittleEndianByteOrder.GetBytes((double)value);
            if (valueType == typeof(string)) return System.Text.Encoding.UTF8.GetBytes((string)value);

            throw new InvalidOperationException($"Unsupported Type {valueType} used as Index.");
        }

        public T First<I>(string propertyName, I value)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDatabase");
            }

            var byteValue = GetBytes<I>(value);

            // Look in the primary index for this json
            var entry = this.IndexOf(propertyName).Get(byteValue);
            if (entry == default(Tuple<byte[], uint>))
            {
                return null;
            }

            return this.jsonSerializer.Deserializer(this.jsonRecordStorage.Find(entry.Item2));
        }

        public IEnumerable<T> Find<I>(string propertyName, I value)
        {
            var byteValue = GetBytes<I>(value);

            // Use the secondary index to find this json
            foreach (var entry in this.IndexOf(propertyName).LargerThanOrEqualTo(byteValue))
            {
                // As soon as we reached larger key than the key given by client, stop
                if (ByteArrayComparer.Compare(entry.Item1, byteValue) > 0)
                {
                    break;
                }

                // Still in range, yield return
                yield return this.jsonSerializer.Deserializer(this.jsonRecordStorage.Find(entry.Item2));
            }
        }

        /// <summary>
        /// Find a json by its unique id
        /// </summary>
        // public T Find(Guid jsonId)
        // {
        //     if (disposed)
        //     {
        //         throw new ObjectDisposedException("JsonDatabase");
        //     }

        //     // Look in the primary index for this json
        //     var entry = this.primaryIndex.Get(jsonId);
        //     if (entry == null)
        //     {
        //         return null;
        //     }

        //     return this.jsonSerializer.Deserializer(this.jsonRecordStorage.Find(entry.Item2));
        // }

        /// <summary>
        /// Find all jsons that beints to given JsonName
        /// </summary>
        // public IEnumerable<T> FindByName(string name, bool fullMatchesOnly = true)
        // {
        //     var comparer = Comparer<string>.Default;

        //     // Use the secondary index to find this json
        //     foreach (var entry in this.secondaryIndex.LargerThanOrEqualTo(name))
        //     {
        //         // As soon as we reached larger key than the key given by client, stop
        //         if (comparer.Compare(entry.Item1, name) > 0)
        //         {
        //             if (fullMatchesOnly)
        //             {
        //                 break;
        //             }
        //             else
        //             {
        //                 if (!entry.Item1.StartsWith(name))
        //                 {
        //                     break;
        //                 }
        //             }
        //         }

        //         // Still in range, yield return
        //         yield return this.jsonSerializer.Deserializer(this.jsonRecordStorage.Find(entry.Item2));
        //     }
        // }

        // public IEnumerable<T> FindByName(Func<string, bool> func)
        // {
        //     foreach (var entry in this.secondaryIndex.All())
        //     {
        //         if (func(entry.Item1))
        //         {
        //             yield return this.jsonSerializer.Deserializer(this.jsonRecordStorage.Find(entry.Item2));
        //         }
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
                this.mainDatabaseFile.Dispose();
                foreach (var entry in this.dbIndexFiles)
                    entry.Value.Dispose();
                // this.secondaryIndexFile.Dispose();
                // this.primaryIndexFile.Dispose();
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

