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
                    var entries = db.FindBy(fileName);

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
            else
            {
                using (var db = new FileDatabase(dbPath))
                {
                    var data = db.Find(Guid.Parse("d86835fe-3ea5-4e09-bbf4-bb2ec9c04194"))?.BlockData;

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
