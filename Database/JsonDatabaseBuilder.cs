using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TinyJsonDatabase.Json;

namespace TinyJsonDatabase
{
    public class JsonDatabaseBuilder
    {
        private List<CollectionConfiguration> collections { get; set; }
        private string dbPathPrefix { get; set; }

        public JsonDatabaseBuilder()
        {
            this.dbPathPrefix = "data";
            this.collections = new List<CollectionConfiguration>();
        }

        /// <summary>
        /// Add a collection to the Database configuration
        /// </summary>
        public JsonDatabaseBuilder AddCollection<T>(Action<CollectionConfiguration<T>> configureAction = null) where T : IJsonDocument, new()
        {
            var collection = new CollectionConfiguration<T>(typeof(T));
            collection.WithIndexOn(p => p.Id, false);

            if (configureAction != null)
                configureAction(collection);

            this.collections.Add(collection);
            return this;
        }

        /// <summary>
        /// Specify a file path prefix, e.g. C:\Temp\data
        /// This will result in files like: data.Type.db, data.Type.db.idx, etc.
        /// </summary>
        public JsonDatabaseBuilder WithDatabasePath(string fullFilePrefixPath)
        {
            this.dbPathPrefix = fullFilePrefixPath;
            return this;
        }

        /// <summary>
        /// Build and instantiate the Database
        /// </summary>
        public JsonDatabase Build()
        {
            var collectionsDict = new Dictionary<Type, JsonDocumentCollection>();

            foreach (var coll in this.collections)
            {
                var dbPath = string.Join(".", new[] { this.dbPathPrefix, coll.Type.Name, "db" });

                JsonDocumentCollection jsonDocumentCollection = (JsonDocumentCollection)Activator
                    .CreateInstance(typeof(JsonDocumentCollection<>).MakeGenericType(coll.Type), new object[] { dbPath, coll.Indices });

                collectionsDict.Add(coll.Type, jsonDocumentCollection);
            }

            return new JsonDatabase(collectionsDict);
        }

    }

    public abstract class CollectionConfiguration
    {
        internal Type Type { get; set; }
        internal List<IndexDefinition> Indices;
    }

    public class CollectionConfiguration<T> : CollectionConfiguration where T : IJsonDocument, new()
    {

        internal CollectionConfiguration(Type type)
        {
            this.Type = type;
            this.Indices = new List<IndexDefinition>();
        }

        /// <summary>
        /// Create a secondary index on a property
        /// </summary>
        public CollectionConfiguration<T> WithIndexOn(Expression<Func<T, object>> propertySelector, bool allowDuplicateKeys = true)
        {
            var property = ReflectionHelper.PropertyFromLambda(propertySelector);
            if (property == null)
                throw new ArgumentOutOfRangeException(nameof(propertySelector) + ": Only properties are supported.");

            this.Indices.Add(new IndexDefinition(property.Name, allowDuplicateKeys));
            return this;
        }
W    }
}