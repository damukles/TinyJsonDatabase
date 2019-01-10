using TinyBlockStorage.Core;

namespace System.Text
{
    public static class EncodingExtensions
    {
        private static readonly byte[] bytesNullRepresentation = new byte[1];
        // private static readonly Func<Encoding, string> stringNullRepresentation =
        //     (encoding) => encoding.GetString(bytesNullRepresentation);

        public static byte[] GetBytesWithNullRepresentation(this Encoding encoding, string s)
        {
            if (s == null)
            {
                return bytesNullRepresentation;
            }

            return encoding.GetBytes(s);
        }

        // public static string GetStringWithNullRepresentiation(this Encoding encoding, byte[] bytes)
        // {
        //     if (bytes.CompareTo(bytesNullRepresentation) == 0)
        //     {
        //         return null;
        //     }

        //     return encoding.GetString(bytes);
        // }
    }
}