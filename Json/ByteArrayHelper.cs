using System;
using System.Text;
using TinyJsonDatabase.Core;

namespace TinyJsonDatabase.Json
{
    public static class ByteArrayHelper
    {
        public static byte[] GetBytes(object value, Type valueType)
        {
            if (valueType == typeof(int)) return LittleEndianByteOrder.GetBytes((int)value);
            if (valueType == typeof(long)) return LittleEndianByteOrder.GetBytes((long)value);
            if (valueType == typeof(uint)) return LittleEndianByteOrder.GetBytes((uint)value);
            if (valueType == typeof(float)) return LittleEndianByteOrder.GetBytes((float)value);
            if (valueType == typeof(double)) return LittleEndianByteOrder.GetBytes((double)value);
            if (valueType == typeof(string)) return Encoding.UTF8.GetBytesWithNullRepresentation((string)value);

            throw new InvalidOperationException($"Unsupported Type {valueType}.");
        }
    }
}