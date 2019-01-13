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
            var objProps = obj
                    .GetType()
                    .GetProperties()
                    .ToList();

            var indexTrees = this.indexTrees;
            if (propertyNames != null)
            {
                indexTrees = this.indexTrees
                    .Where(x => propertyNames.Contains(x.Key))
                    .ToDictionary(k => k.Key, v => v.Value);
            }

            foreach (var idx in indexTrees)
            {
                var jsonProp = objProps
                    .Where(p => p.Name == idx.Key)
                    .SingleOrDefault();

                if (jsonProp != default(PropertyInfo))
                {
                    IndexTree indexToInsertInto = idx.Value;
                    object value = jsonProp.GetValue(obj);
                    byte[] byteValue = ByteArrayHelper.GetBytes(value, jsonProp.PropertyType);
                    indexToInsertInto.Insert(byteValue, recordId);
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

            // Look in the secondary index for this json
            var entry = this.GetIndex(property.Name)?.Get(byteValue);
            if (entry == null)
            {
                return null;
            }
            return entry.Item2;
        }

        public IEnumerable<T> Find(Expression<Func<T, object>> propertySelector, object propertyValue, Func<uint, T> deserializer)
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
                yield return deserializer(entry.Item2);
            }
        }

        public void Delete(T obj)
        {
            if (obj == null)
                return;

            var objProps = obj
                .GetType()
                .GetProperties()
                .ToList();

            foreach (var idx in this.indexTrees)
            {
                var objProp = objProps.SingleOrDefault(p => p.Name == idx.Key);
                if (objProp != default(PropertyInfo))
                {
                    object value = objProp.GetValue(obj);
                    byte[] byteValue = ByteArrayHelper.GetBytes(value, objProp.PropertyType);
                    idx.Value.Delete(byteValue); // TODO unique or not?: , entry.Item2
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

            IndexTree indexTree = (IndexTree)Activator
                    .CreateInstance(typeof(Tree<,>).MakeGenericType(definition.PropertyType, typeof(uint)),
                        new object[]
                        {
                            Activator.CreateInstance(typeof(TreeDiskNodeManager<,>)
                                .MakeGenericType(new Type[] { definition.PropertyType, typeof(uint) }),
                            new object[]
                                {
                                    GetTreeTypeSerializer(definition.PropertyType),
                                    new TreeUIntSerializer(),
                                    new RecordStorage(new BlockStorage(dbFile, 4096))
                                })
                        }, definition.AllowDuplicateKeys);

            return new Tuple<string, bool>(definition.PropertyName, indexExists);
        }

        private object GetTreeTypeSerializer(Type propertyType)
        {
            if (propertyType == typeof(int)) return new TreeIntSerializer();
            if (propertyType == typeof(long)) return new TreeLongSerializer();
            if (propertyType == typeof(uint)) return new TreeUIntSerializer();
            // if (propertyType == typeof(float)) return new TreeFloatSerializer();
            // if (propertyType == typeof(double)) return new TreeDoubleSerializer();
            if (propertyType == typeof(string)) return new TreeTraverseDirection(); ;

            throw new InvalidOperationException($"Unsupported Type {propertyType} used as Index.");
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