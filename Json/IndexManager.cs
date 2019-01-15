using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using TinyJsonDatabase.Core;

namespace TinyJsonDatabase.Json
{
    public class IndexManager<T> : IDisposable
    {
        private Dictionary<string, IndexTree> indexTrees = new Dictionary<string, IndexTree>();
        private Dictionary<string, Stream> indexFiles = new Dictionary<string, Stream>();
        private Comparer<byte[]> ByteArrayComparer => Comparer<byte[]>.Create((a, b) => a.CompareTo(b));

        public IEnumerable<string> IndicesToRebuild { get; set; } = null;

        public IndexManager(string pathToJsonDb, IEnumerable<IndexDefinition> indexTrees)
        {
            if (indexTrees != null)
            {
                IndicesToRebuild = indexTrees?
                .Select(index => CreateIndexOn(index, pathToJsonDb)).ToList()
                .Where(x => x.Item2 == false)
                .Select(x => x.Item1);
            }
        }

        public void Insert(T obj, uint recordId, IEnumerable<string> propertyNames = null)
        {
            var indexTrees = this.indexTrees;
            if (propertyNames != null)
            {
                indexTrees = this.indexTrees
                    .Where(x => propertyNames.Contains(x.Key))
                    .ToDictionary(k => k.Key, v => v.Value);
            }

            var objProperties = obj
                .GetType()
                .GetProperties()
                .ToList();

            foreach (var indexTree in indexTrees)
            {
                var jsonProperty = objProperties
                    .Where(prop => prop.Name == indexTree.Key)
                    .SingleOrDefault();

                if (jsonProperty != default(PropertyInfo))
                {
                    IndexTree indexForProperty = indexTree.Value;
                    object value = jsonProperty.GetValue(obj);
                    byte[] byteValue = ByteArrayHelper.GetBytes(value, jsonProperty.PropertyType);
                    indexForProperty.Insert(byteValue, recordId);
                }
            }
        }

        public uint? First(Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDocumentCollection");
            }

            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            var property = ReflectionHelper.PropertyFromLambda(propertySelector);
            if (property == null)
                throw new ArgumentOutOfRangeException(nameof(propertySelector) + ": Only properties are supported.");

            var byteValue = ByteArrayHelper.GetBytes(propertyValue, property.PropertyType);

            var entry = this.GetIndex(property.Name)?.Get(byteValue);
            if (entry == null)
            {
                return null;
            }
            return entry.Item2;
        }

        public IEnumerable<T> Find(Expression<Func<T, object>> propertySelector, object propertyValue, Func<uint, T> deserializer)
        {
            foreach (var recordId in FindRecordIds(propertySelector, propertyValue))
            {
                yield return deserializer(recordId);
            }
        }

        public IEnumerable<uint> FindRecordIds(Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("JsonDocumentCollection");
            }

            if (propertySelector == null)
                throw new ArgumentNullException(nameof(propertySelector));

            var property = ReflectionHelper.PropertyFromLambda(propertySelector);
            if (property == null)
                throw new ArgumentOutOfRangeException(nameof(propertySelector) + ": Only properties are supported.");

            var byteValue = ByteArrayHelper.GetBytes(propertyValue, property.PropertyType);

            var index = this.GetIndex(property.Name);

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
                yield return entry.Item2;
            }
        }

        public void Delete(T obj, uint recordId)
        {
            if (obj == null)
                return;

            var objProperties = obj
                .GetType()
                .GetProperties()
                .ToList();

            foreach (var indexTree in this.indexTrees)
            {
                var objProperty = objProperties.SingleOrDefault(prop => prop.Name == indexTree.Key);
                if (objProperty != default(PropertyInfo))
                {
                    object value = objProperty.GetValue(obj);
                    byte[] byteValue = ByteArrayHelper.GetBytes(value, objProperty.PropertyType);

                    if (indexTree.Value.AllowDuplicateKeys)
                    {
                        indexTree.Value.Delete(byteValue, recordId);
                    }
                    else
                    {
                        indexTree.Value.Delete(byteValue);
                    }
                }
            }
        }

        private IndexTree GetIndex(string propertyName)
        {
            IndexTree value = null;
            this.indexTrees.TryGetValue(propertyName, out value);
            return value;
        }

        private Tuple<string, bool> CreateIndexOn(IndexDefinition definition, string pathToJsonDb)
        {
            bool indexExists = File.Exists(pathToJsonDb + "." + definition.PropertyName + ".idx");

            // Create Db file
            var dbFile = new FileStream(pathToJsonDb + "." + definition.PropertyName + ".idx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.indexFiles.Add(definition.PropertyName, dbFile);

            var indexTree = new IndexTree(
                    new TreeDiskNodeManager<byte[], uint>(
                        new TreeByteArraySerializer(),
                        new TreeUIntSerializer(),
                        new RecordStorage(new BlockStorage(dbFile, 4096)),
                        ByteArrayComparer
                    ),
                    definition.AllowDuplicateKeys
                );
            this.indexTrees.Add(definition.PropertyName, indexTree);

            return new Tuple<string, bool>(definition.PropertyName, indexExists);
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
                foreach (var entry in this.indexFiles)
                {
                    entry.Value.Dispose();
                }
                this.disposed = true;
            }
        }

        ~IndexManager()
        {
            Dispose(false);
        }
        #endregion
    }
}