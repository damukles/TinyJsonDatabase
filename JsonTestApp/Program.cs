using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TinyBlockStorage.Json;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbPath = "data.db";


            using (var db = new JsonDatabase<Dog>(dbPath))
            {
                // dummy object
                var dog = new Dog()
                {
                    Name = "Bello",
                    Age = 8,
                    Barks = false
                };

                // InsertDogs(db, 1000, dog);

                // First calls are very slow
                InitJsonConvert(dog);

                var stopWatch = Stopwatch.StartNew();

                var dbDog = db.Find(Guid.Parse("b82e8e8e-08af-40d9-a05e-72031c864532"));
                var init = stopWatch.ElapsedMilliseconds;

                var dogs = db.FindAll(x => x.ToString().StartsWith("b82e")).ToList();
                var partial = stopWatch.ElapsedMilliseconds;

                var dogs2 = db.FindAll(_ => true).ToList();
                var all = stopWatch.ElapsedMilliseconds;

                stopWatch.Stop();

                Console.WriteLine("Partial: {0}", dogs.Count);
                printOut(dogs);

                Console.WriteLine("All: {0}", dogs2.Count);
                // printOut(dogs2);

                Console.WriteLine("Single Known Time: {0}", init);
                Console.WriteLine("Partial Time: {0}", partial - init);
                Console.WriteLine("All Time: {0}", all - partial);
            }
        }

        private static void InitJsonConvert(Dog dog)
        {
            // Init the NewtonSoft JsonConvert class to optimize
            var dummy = JsonConvert.SerializeObject(dog);
            var _ = JsonConvert.DeserializeObject<Dog>(dummy);
        }

        private static void InsertDogs(JsonDatabase<Dog> db, int count, Dog dog)
        {
            Guid id;
            for (var i = 0; i < count; i++)
                id = db.Insert(dog);
        }

        private static void printOut(IEnumerable<Dog> dogs)
        {
            if (dogs == null || !dogs.Any())
            {
                Console.WriteLine("Not found -.-");
            }
            else
            {
                foreach (var dog in dogs)
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
