using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

using Tlabs.Data.Store.Intern;

namespace Tlabs.Data.Store {

  ///<summary>Wrapper to convert a <see cref="EfDataStore{T}"/> into a <see cref="IDataStore"/> implementaion.</summary>
  public class IDataStoreEf<T> : IDataStore, IDisposable where T : DbContext {

    private EfDataStore<T> efStore;

    ///<summary>Ctor from <paramref name="efStore"/>.</summary>
    public IDataStoreEf(EfDataStore<T> efStore) {
      this.efStore= efStore;
    }


    ///<inherit/>
    public bool AutoCommit { get; set; }

    ///<inherit/>
    public void CommitChanges() => efStore.CommitChanges(); 

    ///<inherit/>
    public void ResetChanges() => efStore.ResetChanges();

    ///<inherit/>
    public void WithTransaction(Action<IDataTransaction> operation) => efStore.WithTransaction(operation);
    
    ///<inherit/>
    public void EnsureStore(IEnumerable<IDataSeed> seeds) => efStore.EnsureStore(seeds);

    ///<inherit/>
    public TEntity Get<TEntity>(params object[] keys) where TEntity : class => efStore.Get<TEntity>(keys);

    ///<inherit/>
    public object GetIdentifier<TEntity>(TEntity entity) where TEntity : class => efStore.GetIdentifier<TEntity>(entity);

    ///<inherit/>
    public System.Linq.IQueryable<TEntity> Query<TEntity>()  where TEntity : class => efStore.Query<TEntity>();

    ///<inherit/>
    public System.Linq.IQueryable<TEntity> UntrackedQuery<TEntity>() where TEntity : class => efStore.UntrackedQuery<TEntity>();

    ///<inherit/>
    public TEntity Insert<TEntity>(TEntity entity) where TEntity : class => efStore.Insert<TEntity>(entity);

    ///<inherit/>
    public TEntity Merge<TEntity>(TEntity entity) where TEntity : class, new() => efStore.Merge<TEntity>(entity);

    ///<inherit/>
    public TEntity Update<TEntity>(TEntity entity) where TEntity : class => efStore.Update<TEntity>(entity);

    ///<inherit/>
    public void Delete<TEntity>(TEntity entity) where TEntity : class => efStore.Delete<TEntity>(entity);

    ///<inherit/>
    public TEntity Attach<TEntity>(TEntity entity) where TEntity : class => efStore.Attach<TEntity>(entity);

    ///<inherit/>
    public void Evict<TEntity>(TEntity entity) where TEntity : class => efStore.Evict<TEntity>(entity);

    ///<inherit/>
    public E LoadExplicit<E, P>(E entity, Expression<Func<E, IEnumerable<P>>> prop) where E : class where P : class
      => efStore.LoadExplicit<E, P>(entity, prop);

    ///<inherit/>
    public E LoadExplicit<E, P>(E entity, Expression<Func<E, P>> prop) where E : class where P : class
      => efStore.LoadExplicit<E, P>(entity, prop);

    ///<inherit/>
    public IQueryable<E> LoadRelated<E>(IQueryable<E> query, string navigationPropertyPath) where E : class => efStore.LoadRelated(query, navigationPropertyPath);

    ///<inherit/>
    public IEagerLoadedQueryable<E, P> LoadRelated<E, P>(IQueryable<E> query, Expression<Func<E, P>> navProperty) where E : class => efStore.LoadRelated<E, P>(query, navProperty);

    ///<inherit/>
    public IEagerLoadedQueryable<E, Prop> ThenLoadRelated<E, Prev, Prop>(IEagerLoadedQueryable<E, IEnumerable<Prev>> query, Expression<Func<Prev, Prop>> navProperty) where E : class
      => efStore.ThenLoadRelated<E, Prev, Prop>(query, navProperty);


    ///<inherit/>
    public IEagerLoadedQueryable<E, Prop> ThenLoadRelated<E, Prev, Prop>(IEagerLoadedQueryable<E, Prev> query, Expression<Func<Prev, Prop>> navProperty) where E : class
      => efStore.ThenLoadRelated<E, Prev, Prop>(query, navProperty);


    ///<inherit/>
    public void Dispose() => efStore.Dispose();

  }

}