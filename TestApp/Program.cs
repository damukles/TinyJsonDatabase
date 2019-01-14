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
