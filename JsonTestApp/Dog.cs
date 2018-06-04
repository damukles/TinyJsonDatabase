using System;
using TinyBlockStorage.Json;

namespace TestApp
{
    public class Dog : JsonDocument
    {
        [PrimaryKey]
        public Guid DummyId { get; set; }
        public int Age { get; set; }
        public bool Barks { get; set; }
    }
}