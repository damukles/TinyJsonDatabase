using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using TinyBlockStorage.Core;

namespace TinyBlockStorage.Json
{
    public abstract class JsonDocumentCollection : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public class JsonDocumentCollection<T> : JsonDocumentCollection, IJsonDocumentCollection<T>, IDisposable where T : IJsonDocument, new()
    {
        private readonly string pathToJsonDb;
        readonly Stream mainDatabaseFile;
        readonly int mainDatabaseFileBlockSize;
        readonly Stream primaryIndexFile;
        readonly Tree<Guid, uint> primaryIndex;
        readonly RecordStorage jsonRecordStorage;
        readonly JsonSerializer<T> jsonSerializer = new JsonSerializer<T>();
        private Dictionary<string, Stream> secondaryIndexFiles = new Dictionary<string, Stream>();
        private Dictionary<string, IndexTree> secondaryIndices = new Dictionary<string, IndexTree>();
        private Comparer<byte[]> ByteArrayComparer => Comparer<byte[]>.Create((a, b) => a.CompareTo(b));

        object SyncRoot = new Object();


        /// <summary>
        /// </summary>
        /// <param name="pathToJsonDb">Path to json db.</param>
        /// <param name="secondaryIndices">Tuple of propertyName and duplicateKeys</param>
        public JsonDocumentCollection(string pathToJsonDb, IEnumerable<Tuple<string, bool>> secondaryIndices = null)
        {
            if (pathToJsonDb == null)
                throw new ArgumentNullException("pathToJsonDb");

            this.pathToJsonDb = pathToJsonDb;
            this.mainDatabaseFileBlockSize = 4096;

            this.mainDatabaseFile = new FileStream(pathToJsonDb, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);

            bool primaryIndexExists = File.Exists(pathToJsonDb + ".pidx");
            this.primaryIndexFile = new FileStream(pathToJsonDb + ".pidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            // this.secondaryIndexFile = new FileStream(pathToJsonDb + ".sidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);

            this.jsonRecordStorage = new RecordStorage(new BlockStorage(this.mainDatabaseFile, mainDatabaseFileBlockSize, 48));

            // Construct the primary index
            this.primaryIndex = new Tree<Guid, uint>(
                new TreeDiskNodeManager<Guid, uint>(
                    new GuidSerializer(),
                    new TreeUIntSerializer(),
                    new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))
                ),
                false
            );

            var secondaryIndicesToRebuild = secondaryIndices?
                .Select(x => CreateIndexOn(x.Item1, x.Item2)).ToList()
                .Where(x => x.Item2 == false)
                .Select(x => x.Item1);

            // Rebuild every index where there was no corresponding file
            RebuildIndices(!primaryIndexExists, secondaryIndicesToRebuild);
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
                throw new ObjectDisposedException("JsonDatabase");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert a new json entry into our json database
        /// </summary>
        public Guid Insert(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (disposed)
            {
                throw new ObjectDisposedException("JsonDatabase");
            }

            lock (SyncRoot)
            {
                // Serialize the json and insert it
                var (bytes, id) = this.jsonSerializer.Serialize(obj);
                var recordId = this.jsonRecordStorage.Create(bytes);

                // Primary index
                this.primaryIndex.Insert(id, recordId);

                // Secondary Indices
                InsertIntoSecondaryIndeces(obj, recordId);
                // Secondary index
                // this.secondaryIndex.Insert(json.Name, recordId);
                return id;
            }
        }

        public T First(Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDatabase");
            }

            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            var property = ReflectionHelper.PropertyFromLambda(propertySelector);
            if (property == null)
                throw new ArgumentOutOfRangeException(nameof(propertySelector) + ": Only properties are supported.");

            var byteValue = GetBytes(propertyValue, property.PropertyType);

            // Look in the secondary index for this json
            var entry = this.IndexOf(property.Name)?.Get(byteValue);
            if (entry == null)
            {
                return default(T);
            }

            return this.jsonSerializer.Deserialize(this.jsonRecordStorage.Find(entry.Item2));
        }

        public IEnumerable<T> Find(Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDatabase");
            }

            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            var property = ReflectionHelper.PropertyFromLambda(propertySelector);
            if (property == null)
                throw new ArgumentOutOfRangeException(nameof(propertySelector) + ": Only properties are supported.");

            var byteValue = GetBytes(propertyValue, property.PropertyType);

            // Use the secondary index to find this json
            var index = this.IndexOf(property.Name);

            if (index == null)
                yield break;

            foreach (var entry in index.LargerThanOrEqualTo(byteValue))
            {
                // As soon as we reached larger key than the key given by client, stop
                if (ByteArrayComparer.Compare(entry.Item1, byteValue) > 0)
                {
                    break;
                }

                // Still in range, yield return
                yield return this.jsonSerializer.Deserialize(this.jsonRecordStorage.Find(entry.Item2));
            }
        }

        /// <summary>
        /// Find a json by its unique id
        /// </summary>
        public T Find(Guid objId)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDatabase");
            }

            // Look in the primary index for this json
            var entry = this.primaryIndex.Get(objId);
            if (entry == null)
            {
                return default(T);
            }

            return this.jsonSerializer.Deserialize(this.jsonRecordStorage.Find(entry.Item2));
        }

        /// <summary>
        /// Delete specified json from our database
        /// </summary>
        public void Delete(Guid objId)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDatabase");
            }

            var entry = this.Find(objId);

            if (EqualityComparer<T>.Default.Equals(entry, default(T)))
                return;

            Delete(entry);
        }

        private void Delete(T obj)
        {
            var entry = this.primaryIndex.Get(obj.Id);

            if (entry == null)
                return;

            this.jsonRecordStorage.Delete(entry.Item2);

            this.primaryIndex.Delete(entry.Item1);

            var objProps = obj
                .GetType()
                .GetProperties()
                .ToList();

            foreach (var idx in this.secondaryIndices)
            {
                var objProp = objProps.SingleOrDefault(p => p.Name == idx.Key);
                if (objProp != default(PropertyInfo))
                {
                    object value = objProp.GetValue(obj);
                    byte[] byteValue = GetBytes(value, objProp.PropertyType);
                    idx.Value.Delete(byteValue, entry.Item2);
                }
            }
        }

        private void InsertIntoSecondaryIndeces(T obj, uint recordId, IEnumerable<string> propertyNames = null)
        {
            var objProps = obj
                    .GetType()
                    .GetProperties()
                    .ToList();

            var indices = this.secondaryIndices;
            if (propertyNames != null)
            {
                indices = this.secondaryIndices
                    .Where(x => propertyNames.Contains(x.Key))
                    .ToDictionary(k => k.Key, v => v.Value);
            }

            foreach (var idx in indices)
            {
                var jsonProp = objProps
                    .Where(p => p.Name == idx.Key)
                    .SingleOrDefault();

                if (jsonProp != default(PropertyInfo))
                {
                    IndexTree indexToInsertInto = idx.Value;
                    object value = jsonProp.GetValue(obj);
                    byte[] byteValue = GetBytes(value, jsonProp.PropertyType);
                    indexToInsertInto.Insert(byteValue, recordId);
                }
            }
        }

        private Tuple<string, bool> CreateIndexOn(string propertyName, bool duplicateKeys = false)
        {
            bool indexExists = File.Exists(this.pathToJsonDb + "." + propertyName + ".idx");

            // Create Db file
            var dbFile = new FileStream(this.pathToJsonDb + "." + propertyName + ".idx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.secondaryIndexFiles.Add(propertyName, dbFile);

            this.secondaryIndices.Add(propertyName,
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
            return new Tuple<string, bool>(propertyName, indexExists);
        }

        private void RebuildIndices(bool rebuildPrimaryIndex, IEnumerable<string> propertyNames)
        {
            if (rebuildPrimaryIndex == false && (propertyNames == null || !propertyNames.Any()))
                return;

            for (uint curRecStart = 1;
                 curRecStart < this.mainDatabaseFile.Length / this.mainDatabaseFileBlockSize;
                 curRecStart++)
            {
                var currentRecord = this.jsonRecordStorage.Find(curRecStart);
                if (currentRecord != null)
                {
                    T obj = this.jsonSerializer.Deserialize(currentRecord);

                    // Primary index
                    if (rebuildPrimaryIndex) this.primaryIndex.Insert(obj.Id, curRecStart);

                    // Secondary Indeces
                    InsertIntoSecondaryIndeces(obj, curRecStart, propertyNames);

                }
            }
        }

        private IndexTree IndexOf(string propertyName)
        {
            IndexTree value = null;
            secondaryIndices.TryGetValue(propertyName, out value);
            return value;
        }

        // private byte[] GetBytes<I>(I value)
        // {
        //     return GetBytes((object)value, typeof(I));
        // }

        private byte[] GetBytes(object value, Type valueType)
        {
            if (valueType == typeof(int)) return LittleEndianByteOrder.GetBytes((int)value);
            if (valueType == typeof(long)) return LittleEndianByteOrder.GetBytes((long)value);
            if (valueType == typeof(uint)) return LittleEndianByteOrder.GetBytes((uint)value);
            if (valueType == typeof(float)) return LittleEndianByteOrder.GetBytes((float)value);
            if (valueType == typeof(double)) return LittleEndianByteOrder.GetBytes((double)value);
            if (valueType == typeof(string)) return Encoding.UTF8.GetBytesWithNullRepresentation((string)value);

            throw new InvalidOperationException($"Unsupported Type {valueType} used as Index.");
        }

        #region Dispose
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            base.Dispose();
        }

        bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                this.mainDatabaseFile.Dispose();
                this.primaryIndexFile.Dispose();
                foreach (var entry in this.secondaryIndexFiles)
                {
                    entry.Value.Dispose();
                }
                // this.secondaryIndexFile.Dispose();
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

