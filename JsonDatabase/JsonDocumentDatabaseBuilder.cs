using System;
using System.Collections.Generic;

namespace TinyBlockStorage.JsonDatabase
{
    public class JsonDocumentDatabaseBuilder
    {
        private List<CollectionConfiguration> collections { get; set; }

        public JsonDocumentDatabaseBuilder()
        {
            this.collections = new List<CollectionConfiguration>();
        }

        public JsonDocumentDatabaseBuilder AddCollection<T>(string name, Action<CollectionConfiguration> configureAction)
        {
            var collection = new CollectionConfiguration(name, typeof(T));
            configureAction(collection);
            this.collections.Add(collection);
            return this;
        }

    }

    public class CollectionConfiguration
    {
        private List<Tuple<string, bool>> secondaryIndices;

        private string name { get; set; }
        private Type type { get; set; }

        internal CollectionConfiguration(string name, Type type)
        {
            this.name = name;
            this.type = type;
            this.secondaryIndices = new List<Tuple<string, bool>>();
        }

        public CollectionConfiguration WithIndexOn(string propertyName, bool allowDuplicateKeys = true)
        {
            this.secondaryIndices.Add(new Tuple<string, bool>(propertyName, allowDuplicateKeys));
            return this;
        }

    }
}