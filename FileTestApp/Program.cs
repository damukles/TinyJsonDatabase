using System;
using System.IO;
using System.Linq;
using TinyBlockStorage.File;

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

                var fileLastEdited = (int)File.GetLastWriteTimeUtc(filePath).ToFileTimeUtc();
                var fileName = Path.GetFileName(filePath);
                byte[] fileContent = File.ReadAllBytes(filePath);

                using (var db = new FileDatabase(dbPath))
                {
                    db.Insert(new FileModel()
                    {
                        Id = Guid.NewGuid(),
                        FileName = fileName,
                        LastEditedUnixTimeSeconds = fileLastEdited,
                        BlockData = fileContent
                    });
                }

                using (var db = new FileDatabase(dbPath))
                {
                    var entries = db.FindBy(fileName, fileLastEdited);

                    if (entries == null || !entries.Any())
                    {
                        Console.WriteLine("Not found -.-");
                    }
                    else
                    {
                        foreach (var e in entries)
                            Console.WriteLine(e.ToString());
                    }
                }
            }

            using (var db = new FileDatabase(dbPath))
            {
                var data = db.Find(Guid.Parse("1f90ea04-ed60-425e-810f-594f2cd6801b"))?.BlockData;

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
