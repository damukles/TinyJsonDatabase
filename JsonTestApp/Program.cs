using System;
using System.IO;
using System.Linq;
using TinyBlockStorage.Json;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbPath = "data.db";

            Guid id;

            using (var db = new JsonDatabase<Dog>(dbPath))
            {
                id = db.Insert(new Dog()
                {
                    Name = "Bello",
                    Age = 8,
                    Barks = false
                });
            }

            using (var db = new JsonDatabase<Dog>(dbPath))
            {
                var dog = db.Find(id);

                if (dog == null)
                {
                    Console.WriteLine("Not found -.-");
                }
                else
                {
                    Console.WriteLine(String.Join(" - ", new string[] {
                        dog.Id.ToString(),
                        dog.Name,
                        dog.Age.ToString(),
                        dog.Barks.ToString()
                    }));
                }
            }
        }
    }
}
