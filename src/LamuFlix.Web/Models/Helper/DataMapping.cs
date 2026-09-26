using System;

namespace LamuFlix.Web.Models.Helper;

public class DataMapping(string destination, string? key = null) : Attribute
{
    private string Destination { get; } = destination;
    private string? Key { get; } = key;

    public string GetDestination()
    {
        return Destination;
    }

    public string? GetKey()
    {
        return Key;
    }
}