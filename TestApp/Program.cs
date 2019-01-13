using System;
using TinyJsonDatabase;
using TinyJsonDatabase.Json;

namespace JsonDatabaseTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new JsonDatabaseBuilder()
                .AddCollection<Person>(config =>
                {
                    config.WithIndexOn(p => p.Id, false);
                    config.WithIndexOn(p => p.Name);
                })
                .AddCollection<Dog>(config =>
                {
                    config.WithIndexOn(p => p.Id);
                });


            using (var database = builder.Build())
            {
                var pers = new Person()
                {
                    Id = Guid.NewGuid(),
                    Name = "Daniel"
                };

                database
                    .GetCollection<Person>()
                    .Insert(pers);

                var person = database
                    .GetCollection<Person>()
                    .First(p => p.Name, "Daniel");

                database
                    .GetCollection<Person>()
                    .DeleteFirst(p => p.Id, pers.Id);
            }
        }
    }

    public class Dog
    {
        public Guid Id { get; set; }
    }

    public class Person
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
