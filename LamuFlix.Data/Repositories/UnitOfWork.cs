using System;
using LamuFlix.Data.Models;

namespace LamuFlix.Data.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        void Save();
        new void Dispose();
    }

    public class UnitOfWork : IUnitOfWork
    {
        public LamuFlixContext DataContext { get; }

        public UnitOfWork(LamuFlixContext dataContext)
        {
            DataContext = dataContext;
        }

        private GenericRepository<Movie>? movieRepository;

        public GenericRepository<Movie> MovieRepository
        {
            get
            {
                if (this.movieRepository == null)
                {
                    this.movieRepository = new GenericRepository<Movie>(DataContext);
                }
                return movieRepository;
            }
        }

        public void Save()
        {
            DataContext.SaveChanges();
        }

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    DataContext.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
