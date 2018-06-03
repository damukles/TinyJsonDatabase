// using System;

// namespace TinyBlockStorage.Json
// {
//     /// <summary>
//     /// Our database stores cows, first we define our Cow model
//     /// </summary>
//     public class JsonModel
//     {
//         public Guid Id
//         {
//             get;
//             set;
//         }

//         public string JsonName
//         {
//             get;
//             set;
//         }

//         public int LastEditedUnixTimeSeconds
//         {
//             get;
//             set;
//         }

//         public byte[] BlockData
//         {
//             get;
//             set;
//         }

//         public override string ToString()
//         {
//             return string.Format("[JsonModel: Id={0}, JsonName={1}, LastEdited={2}, BlockData={3}]", Id, JsonName, LastEditedUnixTimeSeconds, BlockData.Length + " bytes");
//         }
//     }
// }

