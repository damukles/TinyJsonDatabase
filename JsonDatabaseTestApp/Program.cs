using System;
using TinyBlockStorage.Json;
using TinyBlockStorage.JsonDatabase;

namespace JsonDatabaseTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new JsonDocumentDatabaseBuilder()
                .AddCollection<Person>(config =>
                {
                    config.WithIndexOn(p => p.Name);
                })
                .AddCollection<Dog>();


            using (var database = builder.Build())
            {
                var pers = new Person()
                {
                    Name = "Daniel"
                };

                var id = database
                    .GetCollection<Person>()
                    .Insert(pers);

                var person = database
                    .GetCollection<Person>()
                    .First(p => p.Name, "Daniel");

                database
                    .GetCollection<Person>()
                    .Delete(person.Id);
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
