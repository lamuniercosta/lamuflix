using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LamuFlix.Data.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        IEnumerable<T> GetById(object id);
        IEnumerable<T> Get(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, string includeProperties = "");
        void Add(T entity);
        void Delete(object id);
        void Update(T entity);
    }

    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        internal LamuFlixContext DataContext { get; }

        internal DbSet<T> dbSet;

        public GenericRepository(LamuFlixContext dataContext)
        {
            DataContext = dataContext;
            dbSet = DataContext.Set<T>();
        }

        public void Add(T entity)
        {
            dbSet.Add(entity);
        }

        public virtual void Delete(object id)
        {
            T? entityToDelete = dbSet.Find(id);
            if (entityToDelete is null)
            {
                return;
            }

            Delete(entityToDelete);
        }

        public virtual void Delete(T entityToDelete)
        {
            if (DataContext.Entry(entityToDelete).State == EntityState.Detached)
            {
                dbSet.Attach(entityToDelete);
            }
            dbSet.Remove(entityToDelete);
        }

        public virtual T? GetById(object id)
        {
            return dbSet.Find(id);
        }

        public virtual IEnumerable<T> Get(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, string includeProperties = "")
        {
            IQueryable<T> query = dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split
                ([','], StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            if (orderBy != null)
            {
                return [.. orderBy(query)];
            }
            else
            {
                return [.. query];
            }
        }

        public void Update(T entity)
        {
            dbSet.Attach(entity);
            DataContext.Entry(entity).State = EntityState.Modified;
        }

        IEnumerable<T> IGenericRepository<T>.GetById(object id)
        {
            throw new NotImplementedException();
        }
    }
}
