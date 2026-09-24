using System;
using LamuFlix.Data;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.Tests.Common;

public static class LamuFlixContextFactory
{
    public static LamuFlixContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LamuFlixContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new LamuFlixContext(options);
    }
}
