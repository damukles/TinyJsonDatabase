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


            using (var db = builder.Build())
            {
                var pers = new Person()
                {
                    Name = "Daniel"
                };

                var id = db.GetCollection<Person>().Insert(pers);

                var person = db.GetCollection<Person>().First(p => p.Age, 1);

                db.GetCollection<Person>().Delete(person.Id);
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
