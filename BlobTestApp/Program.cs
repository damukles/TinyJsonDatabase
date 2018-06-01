using System;
using System.IO;
using System.Linq;
using TinyBlockStorage.Blob;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbPath = "data.db";

            if (args?.Length > 0)
            {
                var filePath = args[0];
                byte[] fileContent = File.ReadAllBytes(filePath);

                string addedDataId;

                using (var db = new BlobDatabase(dbPath))
                {
                    var data = new BlobModel(fileContent);
                    addedDataId = data.Id;
                    db.Insert(data);
                }

                using (var db = new BlobDatabase(dbPath))
                {
                    var entry = db.Find(addedDataId);

                    if (entry == null)
                    {
                        Console.WriteLine("Not found -.-");
                    }
                    else
                    {
                        Console.WriteLine(entry.ToString());
                    }
                }
            }
            else
            {
                using (var db = new BlobDatabase(dbPath))
                {
                    var data = db.Find("9jJCLXuEUbMWiAJAccjqlXBztMXJU8YxQS5/0Gn5efM=")?.BlockData;

                    if (data == null)
                    {
                        Console.WriteLine("Cloud not find data by GUID.");
                    }
                    else
                    {
                        File.WriteAllBytes("out.jpg", data);
                        Console.WriteLine("Data written to out.jpg");
                    }
                }
            }
        }
    }
}
