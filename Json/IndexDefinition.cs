using System;

namespace TinyJsonDatabase.Json
{
    public class IndexDefinition
    {
        public string PropertyName { get; set; }
        public bool AllowDuplicateKeys { get; set; }

        public IndexDefinition(string propertyName, bool allowDuplicateKeys)
        {
            PropertyName = propertyName;
            AllowDuplicateKeys = allowDuplicateKeys;
        }
    }
}