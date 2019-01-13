using System;

namespace TinyJsonDatabase.Core
{
    public class TreeNodeSerializationException : Exception
    {
        public TreeNodeSerializationException(Exception innerException)
            : base("Failed to serialize/deserialize heat map node", innerException)
        {

        }
    }
}

