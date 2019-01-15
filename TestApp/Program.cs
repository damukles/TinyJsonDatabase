using System;
using TinyJsonDatabase;

namespace JsonDatabaseTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new JsonDatabaseBuilder()
                .AddCollection<Person>(config =>
                {
                    config.WithIndexOn(p => p.Name);
                })
                .AddCollection<Dog>();


            using (var database = builder.Build())
            {
                var person1 = new Person()
                {
                    Id = Guid.NewGuid(),
                    Name = "Daniel"
                };

                var person2 = new Person()
                {
                    Id = Guid.NewGuid(),
                    Name = "Stefan"
                };

                var person3 = new Person()
                {
                    Id = Guid.NewGuid(),
                    Name = "Daniel"
                };

                var person4 = new Person()
                {
                    Id = Guid.NewGuid(),
                    Name = "Daniel"
                };


                var coll = database.GetCollection<Person>();

                coll.Insert(person1);
                coll.Insert(person2);
                coll.Insert(person3);
                coll.Insert(person4);

                var firstDaniel = coll.First(p => p.Name, "Daniel");
                var allDaniels = coll.Find(p => p.Name, "Daniel");

                var firstStefan = coll.First(p => p.Name, "Stefan");
                var allStefans = coll.Find(p => p.Name, "Stefan");


                coll.DeleteFirst(p => p.Id, person1.Id);
                coll.Delete(p => p.Name, "Daniel");

                // coll.Delete(p => p.Id, person1.Id);
                // coll.DeleteFirst(p => p.Name, "Daniel");
                // coll.DeleteFirst(p => p.Name, "Daniel");

                var shouldBeEmpty = coll.Find(p => p.Name, "Daniel");
            }

        }
    }

    public class Dog : IJsonDocument
    {
        public Guid Id { get; set; }
    }

    public class Person : IJsonDocument
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
