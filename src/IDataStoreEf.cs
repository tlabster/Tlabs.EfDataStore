using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

namespace Tlabs.Data.Store {

  ///<summary>Wrapper to convert a <see cref="EfDataStore{T}"/> into a <see cref="IDataStore"/> implementaion.</summary>
  public class IDataStoreEf<T> : IDataStore, IDisposable where T : DbContext {

    readonly EfDataStore<T> efStore;

    ///<summary>Ctor from <paramref name="efStore"/>.</summary>
    public IDataStoreEf(EfDataStore<T> efStore) {
      this.efStore= efStore;
    }


    ///<inheritdoc/>
    public bool AutoCommit { get; set; }

    ///<inheritdoc/>
    public void CommitChanges() => efStore.CommitChanges(); 

    ///<inheritdoc/>
    public void ResetChanges() => efStore.ResetChanges();

    ///<inheritdoc/>
    public void ResetAll() => efStore.ResetAll();

    ///<inheritdoc/>
    public void WithTransaction(Action<IDataTransaction> operation) => efStore.WithTransaction(operation);
    
    ///<inheritdoc/>
    public void EnsureStore(IEnumerable<IDataSeed> seeds) => efStore.EnsureStore(seeds);

    ///<inheritdoc/>
    public TEntity Get<TEntity>(params object[] keys) where TEntity : class => efStore.Get<TEntity>(keys);

    ///<inheritdoc/>
    public object GetIdentifier<TEntity>(TEntity entity) where TEntity : class => efStore.GetIdentifier<TEntity>(entity);

    ///<inheritdoc/>
    public System.Linq.IQueryable<TEntity> Query<TEntity>()  where TEntity : class => efStore.Query<TEntity>();

    ///<inheritdoc/>
    public System.Linq.IQueryable<TEntity> UntrackedQuery<TEntity>() where TEntity : class => efStore.UntrackedQuery<TEntity>();

    ///<inheritdoc/>
    public TEntity Insert<TEntity>(TEntity entity) where TEntity : class => efStore.Insert<TEntity>(entity);

    ///<inheritdoc/>
    public IEnumerable<E> Insert<E>(IEnumerable<E> entities) where E : class => efStore.Insert(entities);

    ///<inheritdoc/>
    public TEntity Merge<TEntity>(TEntity entity) where TEntity : class, new() => efStore.Merge<TEntity>(entity);

    ///<inheritdoc/>
    public TEntity Update<TEntity>(TEntity entity) where TEntity : class => efStore.Update<TEntity>(entity);

    ///<inheritdoc/>
    public IEnumerable<E> Update<E>(IEnumerable<E> entities) where E : class => efStore.Update(entities);

    ///<inheritdoc/>
    public void Delete<TEntity>(TEntity entity) where TEntity : class => efStore.Delete<TEntity>(entity);

    ///<inheritdoc/>
    public void Delete<E>(IEnumerable<E> entities) where E : class => efStore.Delete(entities);

    ///<inheritdoc/>
    public TEntity Attach<TEntity>(TEntity entity) where TEntity : class => efStore.Attach<TEntity>(entity);

    ///<inheritdoc/>
    public void Evict<TEntity>(TEntity entity) where TEntity : class => efStore.Evict<TEntity>(entity);

    ///<inheritdoc/>
    public E LoadExplicit<E, P>(E entity, Expression<Func<E, IEnumerable<P>>> prop) where E : class where P : class
      => efStore.LoadExplicit<E, P>(entity, prop);

    ///<inheritdoc/>
    public E LoadExplicit<E, P>(E entity, Expression<Func<E, P>> prop) where E : class where P : class
      => efStore.LoadExplicit<E, P>(entity, prop);

    ///<inheritdoc/>
    public IQueryable<E> LoadRelated<E>(IQueryable<E> query, string navigationPropertyPath) where E : class => efStore.LoadRelated(query, navigationPropertyPath);

    ///<inheritdoc/>
    public IEagerLoadedQueryable<E, P> LoadRelated<E, P>(IQueryable<E> query, Expression<Func<E, P>> navProperty) where E : class => efStore.LoadRelated<E, P>(query, navProperty);

    ///<inheritdoc/>
    public IEagerLoadedQueryable<E, Prop> ThenLoadRelated<E, Prev, Prop>(IEagerLoadedQueryable<E, IEnumerable<Prev>> query, Expression<Func<Prev, Prop>> navProperty) where E : class
      => efStore.ThenLoadRelated<E, Prev, Prop>(query, navProperty);


    ///<inheritdoc/>
    public IEagerLoadedQueryable<E, Prop> ThenLoadRelated<E, Prev, Prop>(IEagerLoadedQueryable<E, Prev> query, Expression<Func<Prev, Prop>> navProperty) where E : class
      => efStore.ThenLoadRelated<E, Prev, Prop>(query, navProperty);


    ///<inheritdoc/>
    public void Dispose() {
      efStore.Dispose();
      GC.SuppressFinalize(this);
    }
  }

}