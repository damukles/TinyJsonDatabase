using System;
using TinyBlockStorage.Json;

namespace TestApp
{
    public class Dog : IJsonDocument
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public bool Barks { get; set; }
    }
}