using System;

namespace LamuFlix.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    void Save();
    new void Dispose();
}

public class UnitOfWork(LamuFlixContext dataContext) : IUnitOfWork
{
    private LamuFlixContext DataContext { get; } = dataContext;

    public void Save()
    {
        DataContext.SaveChanges();
    }

    private bool _disposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DataContext.Dispose();
            }
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}