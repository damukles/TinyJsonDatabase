using System;

namespace TinyBlockStorage.Core
{
    public static class ByteArrayExtensions
    {
        public static int CompareTo(this byte[] a, byte[] b)
        {
            if (a.Length == b.Length)
            {

                for (int i = 0; i < a.Length; i++)
                {
                    int comparison = a[i].CompareTo(b[i]);
                    if (comparison != 0)
                        return comparison;
                }
                return 0;
            }
            else
            {
                return a.Length > b.Length ? 1 : -1;
            }
        }
    }
}