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


            // dummy object
            var dog = new Dog()
            {
                Name = "bello",
                Age = 8,
                Barks = false
            };


            // First calls are very slow
            InitJsonConvert(dog);

            var stopWatch = Stopwatch.StartNew();

            var db = new JsonDatabase<Dog>(dbPath,
                new List<Tuple<string, bool>>()
                {
                    new Tuple<string, bool>("Name", true)
                }
            );

            var initTime = stopWatch.ElapsedMilliseconds;

            // InsertDogs(db, 100_000, dog);
            var insertTime = stopWatch.ElapsedMilliseconds;

            var first = db.First<string>("Name", "bello13");
            var firstTime = stopWatch.ElapsedMilliseconds;

            var all = db.Find<string>("Name", "bello13").ToList();
            var allTime = stopWatch.ElapsedMilliseconds;

            // var dog2 = db.Find(Guid.Parse("8205585d-481d-4221-b995-230119a97337"));
            // var findTime = stopWatch.ElapsedMilliseconds;

            // // Get onyl full matches
            // var dogs = db.FindByName("bello734").ToList();
            // var findFullMatchesTime = stopWatch.ElapsedMilliseconds;

            // // Get all that start with string
            // var dogs2 = db.FindByName("bello234", false).ToList();
            // var findStartWithMatchesTime = stopWatch.ElapsedMilliseconds;

            // var dogs3 = db.FindByName(x => x.Contains("102")).ToList();
            // var iterateOverStringIndexTime = stopWatch.ElapsedMilliseconds;

            stopWatch.Stop();
            db.Dispose();

            // Console.WriteLine("Full Matches: {0}", dogs.Count);
            // // printOut(dogs);

            // Console.WriteLine("StartsWith Matches: {0}", dogs2.Count);
            // // printOut(dogs2);

            // Console.WriteLine("Iterate over String Index Matches: {0}", dogs3.Count);

            Console.WriteLine("Init Time: {0}", initTime);
            Console.WriteLine("Insert Time: {0}", insertTime - initTime);
            Console.WriteLine("First Time: {0}", firstTime - insertTime);
            // Console.WriteLine("Single Known Time: {0}", findTime - initTime);
            var view = first == null ? null : new Dog[] { first };
            printOut(view);

            Console.WriteLine("All Matching Time: {0}", allTime - firstTime);
            Console.WriteLine("All Matching Count: {0}", all?.Count);
            printOut(all);
            // Console.WriteLine("Full Matches Time: {0}", findFullMatchesTime - findTime);
            // Console.WriteLine("StartsWith Time: {0}", findStartWithMatchesTime - findFullMatchesTime);
            // Console.WriteLine("Iterate over String Index Time: {0}", iterateOverStringIndexTime - findStartWithMatchesTime);
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
            {
                dog.Name = "bello" + i.ToString();
                id = db.Insert(dog);
            }
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
