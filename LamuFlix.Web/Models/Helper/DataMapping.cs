using System;

namespace LamuFlix.Web.Models.Helper
{
    public class DataMapping : Attribute
    {
        private string Destination { get; }
        private string? Key { get; }

        public DataMapping(string destination, string? key = null)
        {
            Destination = destination;
            Key = key;
        }

        public string GetDestination()
        {
            return Destination;
        }

        public string? GetKey()
        {
            return Key;
        }
    }
}
