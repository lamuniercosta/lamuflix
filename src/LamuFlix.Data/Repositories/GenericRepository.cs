using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LamuFlix.Data.Repositories;

public interface IGenericRepository<T> where T : class
{
    IEnumerable<T> GetById(object id);
    IEnumerable<T> Get(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, string includeProperties = "");
    void Add(T entity);
    void Delete(object id);
    void Update(T entity);
}

public sealed class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private LamuFlixContext DataContext { get; }

    private readonly DbSet<T> _dbSet;

    public GenericRepository(LamuFlixContext dataContext)
    {
        DataContext = dataContext;
        _dbSet = DataContext.Set<T>();
    }

    public void Add(T entity)
    {
        _dbSet.Add(entity);
    }

    public void Delete(object id)
    {
        Delete(FindOrThrow(id));
    }

    private void Delete(T entityToDelete)
    {
        if (DataContext.Entry(entityToDelete).State == EntityState.Detached)
        {
            _dbSet.Attach(entityToDelete);
        }
        _dbSet.Remove(entityToDelete);
    }

    public T GetById(object id)
    {
        return FindOrThrow(id);
    }

    private T FindOrThrow(object id)
    {
        return _dbSet.Find(id) ?? throw new KeyNotFoundException($"{typeof(T).Name} with id '{id}' was not found.");
    }

    public IEnumerable<T> Get(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, string includeProperties = "")
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        query = includeProperties.Split([','], StringSplitOptions.RemoveEmptyEntries).Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

        if (orderBy != null)
        {
            return [.. orderBy(query)];
        }

        return [.. query];
    }

    public void Update(T entity)
    {
        _dbSet.Attach(entity);
        DataContext.Entry(entity).State = EntityState.Modified;
    }

    IEnumerable<T> IGenericRepository<T>.GetById(object id)
    {
        return [FindOrThrow(id)];
    }
}