﻿// using System;
// using TinyBlockStorage.Core;

// namespace TinyBlockStorage.Json
// {
//     public class StringSerializer : ISerializer<string>
//     {
//         public byte[] Serialize(string value)
//         {
//             var stringBytes = System.Text.Encoding.UTF8.GetBytes(value);

//             var data = new byte[
//                 4 +                    // First 4 bytes indicate length of the string
//                 stringBytes.Length +   // another X bytes of actual string content
//                 4                      // Ends with 4 bytes int value
//             ];

//             BufferHelper.WriteBuffer((int)stringBytes.Length, data, 0);
//             Buffer.BlockCopy(src: stringBytes, srcOffset: 0, dst: data, dstOffset: 4, count: stringBytes.Length);
//             return data;
//         }

//         public string Deserialize(byte[] buffer, int offset, int length)
//         {
//             var stringLength = BufferHelper.ReadBufferInt32(buffer, offset);
//             if (stringLength < 0 || stringLength > (16 * 1024))
//             {
//                 throw new Exception("Invalid string length: " + stringLength);
//             }
//             var stringValue = System.Text.Encoding.UTF8.GetString(buffer, offset + 4, stringLength);
//             return stringValue;
//         }

//         public bool IsFixedSize
//         {
//             get
//             {
//                 return false;
//             }
//         }

//         public int Length
//         {
//             get
//             {
//                 throw new InvalidOperationException();
//             }
//         }
//     }
// }

