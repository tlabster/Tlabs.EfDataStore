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
using System.Threading;

namespace Tlabs.Data.Store {

  ///<summary><see cref="IDataStore"/> implementaion based on the Entity Framework Core and a <see cref="DbContext"/>.</summary>
  public class EfDataStore<T> : IDataStore, IDisposable where T : DbContext {
    readonly T ctx;
    readonly ILogger<EfDataStore<T>> log;

    ///<summary>Ctor from <paramref name="ctx"/> and <paramref name="log"/>.</summary>
    public EfDataStore(T ctx, ILogger<EfDataStore<T>> log) {
      this.ctx= ctx;
      this.log= log;
    }

    ///<inheritdoc/>
    public bool AutoCommit { get; set; }

    ///<inheritdoc/>
    public void CommitChanges() {
      try {
        ctx.SaveChangesWithEvents();
      }
      catch (DbUpdateConcurrencyException e) { throw new DataConcurrentPersistenceException(e); }
      catch (DbUpdateException e) { throw new DataPersistenceException(e); }
    }

    ///<inheritdoc/>
    public void ResetChanges() {
      ctx.ChangeTracker.AcceptAllChanges();
    }

    ///<inheritdoc/>
    public void ResetAll() {
      Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry;
      while (null != (entry= ctx.ChangeTracker.Entries().Where(e => e.Entity != null).FirstOrDefault())) {
        entry.State= EntityState.Added;   // mark as 'added' (only) to just evict on remove...
        ctx.Remove(entry.Entity);
      }
    }

    ///<inheritdoc/>
    public void WithTransaction(Action<IDataTransaction> operation) {
      var strategy= ctx.Database.CreateExecutionStrategy();
      try {
        strategy.Execute(() => {
          using var tx= new EfDataTransaction<T>(this, ctx.Database);
          operation(tx);
        });
      }
      catch (DbUpdateConcurrencyException e) { throw new DataConcurrentPersistenceException(e); }
      catch (DbUpdateException e) { throw new DataPersistenceException(e); }
      catch (Exception e) { throw new DataTransactionException(e); }
    }

    ///<inheritdoc/>
    public void EnsureStore(IEnumerable<IDataSeed> seeds) {
      ctx.Database.Migrate();
      //ctx.Database.EnsureCreated();
      if (!allMigrationsApplied())
        log.LogWarning("CAUTION: Some required database schema updates (migrations) are missing from being applied!!!");
      
      if (null == seeds) return;
      
      foreach(var dataSeed in seeds) {
        log.LogWarning("Ensuring '{campaign}'", dataSeed.Campaign);
        dataSeed.Perform();
      }
    }

    private bool allMigrationsApplied() {
      var applied= ctx.GetService<IHistoryRepository>()
        .GetAppliedMigrations()
        .Select(m => m.MigrationId);

      var total= ctx.GetService<IMigrationsAssembly>()
        .Migrations
        .Select(m => m.Key);

      return !total.Except(applied).Any();
    }


    ///<inheritdoc/>
    public TEntity Get<TEntity>(params object[] keys) where TEntity : class => ctx.Find<TEntity>(keys);

    ///<inheritdoc/>
    public object GetIdentifier<TEntity>(TEntity entity) where TEntity : class {
      var entEntry= ctx.Entry(entity);
      var idName= entEntry.Metadata.FindPrimaryKey().Properties.Select(x => x.Name).Single();
      return entEntry.CurrentValues[idName];
    }

    ///<inheritdoc/>
    public System.Linq.IQueryable<TEntity> Query<TEntity>()  where TEntity : class => ctx.Set<TEntity>();

    ///<inheritdoc/>
    public System.Linq.IQueryable<TEntity> UntrackedQuery<TEntity>() where TEntity : class {
      return Query<TEntity>().AsNoTracking(); //ctx.Query<TEntity>() marked obsolete, this also support keyless query type entites...
    }

    ///<inheritdoc/>
    public TEntity Insert<TEntity>(TEntity entity) where TEntity : class {
      ctx.Add<TEntity>(entity);
      return entity;
    }

    ///<inheritdoc/>
    public IEnumerable<TEntity> Insert<TEntity>(IEnumerable<TEntity> entities) where TEntity : class {
      ctx.AddRange(entities);
      return entities;
    }

    ///<inheritdoc/>
    public TEntity Merge<TEntity>(TEntity entity) where TEntity : class, new() {
      TEntity persEnt= Get<TEntity>(GetIdentifier(entity));
      if (null == persEnt) {
        Insert<TEntity>(entity);
        return entity;
      }

      var entEntry= ctx.Entry(persEnt);
      
      /* We only want to merge in value properties or non-null values.
       * For this reason we can not use: entEntry.CurrentValues.SetValues(entity);
       */
      foreach (var prop in entEntry.Properties) {
        var pi= typeof(TEntity).GetRuntimeProperty(prop.Metadata.Name);
        //var pi= typeof(TEntity).GetRuntimeProperties().Where(p => p.Name == prop.Metadata.Name).SingleOrDefault();
        var v= pi?.GetValue(entity);
        if (null != v) 
          prop.CurrentValue= v;
      }
      return entEntry.Entity;
    }

    ///<inheritdoc/>
    public TEntity Update<TEntity>(TEntity entity) where TEntity : class {
      ctx.Update<TEntity>(entity);
      return entity;
    }

    ///<inheritdoc/>
    public IEnumerable<TEntity> Update<TEntity>(IEnumerable<TEntity> entities) where TEntity : class {
      ctx.UpdateRange(entities);
      return entities;
    }

    ///<inheritdoc/>
    public void Delete<TEntity>(TEntity entity) where TEntity : class => ctx.Remove<TEntity>(entity);

    ///<inheritdoc/>
    public void Delete<TEntity>(IEnumerable<TEntity> entities) where TEntity : class => ctx.RemoveRange(entities);

    ///<inheritdoc/>
    public TEntity Attach<TEntity>(TEntity entity) where TEntity : class {
      var entry= ctx.Entry<TEntity>(entity);
      if (EntityState.Detached == entry.State) try {
        entry.State= EntityState.Unchanged;
      }
      catch (InvalidOperationException) {
        entity= Get<TEntity>(GetIdentifier(entity));
      }
      return entity;
    }

    ///<inheritdoc/>
    public void Evict<TEntity>(TEntity entity) where TEntity : class {
      var entry= ctx.Entry<TEntity>(entity);
      entry.State= EntityState.Added;   // mark as 'added' (only) to just evict on remove...
      ctx.Remove<TEntity>(entity);
    }

    ///<inheritdoc/>
    public E LoadExplicit<E, P>(E entity, Expression<Func<E, IEnumerable<P>>> prop) where E : class where P : class {
      ctx.Entry(entity).Collection(prop).Load();
      return entity;
    }

    ///<inheritdoc/>
    public E LoadExplicit<E, P>(E entity, Expression<Func<E, P>> prop) where E : class where P : class {
      ctx.Entry(entity).Reference(prop).Load();
      return entity;
    }
    
    ///<inheritdoc/>
    public IQueryable<E> LoadRelated<E>(IQueryable<E> query, string navigationPropertyPath) where E : class => query.Include(navigationPropertyPath);

    ///<inheritdoc/>
    public IEagerLoadedQueryable<E, P> LoadRelated<E, P>(IQueryable<E> query, Expression<Func<E, P>> navProperty) where E : class {
      var q= query.Include(navProperty);
      return new EagerLoadedQueryable<E, P>(q);
      //return new EagerLoadedQueryable<E, P>(query.Include(navProperty));
    }

    ///<inheritdoc/>
    public IEagerLoadedQueryable<E, Prop> ThenLoadRelated<E, Prev, Prop>(IEagerLoadedQueryable<E, IEnumerable<Prev>> query, Expression<Func<Prev, Prop>> navProperty) where E : class {
      var q= (IIncludableQueryable<E, IEnumerable<Prev>>)query;
      return new EagerLoadedQueryable<E, Prop>(q.ThenInclude(navProperty));
    }

    ///<inheritdoc/>
    public IEagerLoadedQueryable<E, Prop> ThenLoadRelated<E, Prev, Prop>(IEagerLoadedQueryable<E, Prev> query, Expression<Func<Prev, Prop>> navProperty) where E : class {
      var q= (IIncludableQueryable<E, Prev>)query;
      return new EagerLoadedQueryable<E, Prop>(q.ThenInclude(navProperty));
    }

    ///<inheritdoc/>
    public void Dispose() {
      if (AutoCommit) {
        log.LogDebug($"{nameof(EfDataStore<T>)} auto committing changes on disposed.");
        CommitChanges();
      }
      GC.SuppressFinalize(this);
    }

    private class EagerLoadedQueryable<E, P> : IEagerLoadedQueryable<E, P>, IIncludableQueryable<E, P> {
      private readonly IQueryable<E> q;
      public EagerLoadedQueryable(IQueryable<E> q) {
        this.q = q;
      }
      public Expression Expression => q.Expression;
      public Type ElementType => q.ElementType;
      public IQueryProvider Provider => q.Provider;

      public IEnumerator<E> GetEnumerator() => q.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

  }

}