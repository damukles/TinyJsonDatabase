using System;
using TinyBlockStorage.Json;

namespace TestApp
{
    public class Dog : JsonDocument
    {
        public int Age { get; set; }
        public bool Barks { get; set; }
    }
}