using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using TinyBlockStorage.Json;

[assembly: TypeForwardedToAttribute(typeof(IJsonDocument))]

namespace TinyBlockStorage.JsonDatabase
{
    public class JsonDocumentDatabaseBuilder
    {
        private List<CollectionConfiguration> collections { get; set; }
        private string dbPathPrefix { get; set; }

        public JsonDocumentDatabaseBuilder()
        {
            this.dbPathPrefix = "data";
            this.collections = new List<CollectionConfiguration>();
        }

        /// <summary>
        /// Add a collection to the Database configuration
        /// </summary>
        public JsonDocumentDatabaseBuilder AddCollection<T>(Action<CollectionConfiguration<T>> configureAction = null) where T : IJsonDocument, new()
        {
            var collection = new CollectionConfiguration<T>(typeof(T));
            if (configureAction != null)
                configureAction(collection);

            this.collections.Add(collection);
            return this;
        }

        /// <summary>
        /// Specify a file path prefix, e.g. C:\Temp\data
        /// This will result in files like: data.Type.db, data.Type.db.idx, etc.
        /// </summary>
        public JsonDocumentDatabaseBuilder WithDatabasePath(string fullFilePrefixPath)
        {
            this.dbPathPrefix = fullFilePrefixPath;
            return this;
        }

        /// <summary>
        /// Build and instantiate the Database
        /// </summary>
        public JsonDocumentDatabase Build()
        {
            var collectionsDict = new Dictionary<Type, JsonDocumentCollection>();

            foreach (var coll in this.collections)
            {
                var dbPath = string.Join(".", new[] { this.dbPathPrefix, coll.Type.Name, "db" });

                JsonDocumentCollection jsonDocumentCollection = (JsonDocumentCollection)Activator
                    .CreateInstance(typeof(JsonDocumentCollection<>).MakeGenericType(coll.Type), new object[] { dbPath, coll.SecondaryIndices });

                collectionsDict.Add(coll.Type, jsonDocumentCollection);
            }

            return new JsonDocumentDatabase(collectionsDict);
        }

    }

    public abstract class CollectionConfiguration
    {
        internal Type Type { get; set; }
        internal List<Tuple<string, bool>> SecondaryIndices;
    }

    public class CollectionConfiguration<T> : CollectionConfiguration where T : new()
    {

        internal CollectionConfiguration(Type type)
        {
            this.Type = type;
            this.SecondaryIndices = new List<Tuple<string, bool>>();
        }

        /// <summary>
        /// Create a secondary index on a property
        /// </summary>
        public CollectionConfiguration<T> WithIndexOn(Expression<Func<T, object>> properySelector, bool allowDuplicateKeys = true)
        {
            var propertyName = (((properySelector.Body as MemberExpression)?.Member) as PropertyInfo)?.Name;
            if (propertyName == null)
                throw new ArgumentOutOfRangeException(nameof(properySelector) + ": Only properties are supported.");

            this.SecondaryIndices.Add(new Tuple<string, bool>(propertyName, allowDuplicateKeys));
            return this;
        }

        // UNSAFE
        /// <summary>
        /// Create a secondary index on a property
        /// </summary>
        // public CollectionConfiguration<T> WithIndexOn(string propertyName, bool allowDuplicateKeys = true)
        // {
        //     this.SecondaryIndices.Add(new Tuple<string, bool>(propertyName, allowDuplicateKeys));
        //     return this;
        // }
    }
}