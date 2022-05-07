using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tlabs.Data.Store {

  ///<summary>Entityframework core database transcation.</summary>
  public class EfDataTransaction<T> : IDataTransaction where T : DbContext {
    readonly EfDataStore<T> store;
    readonly DatabaseFacade db;

    ///<summary>Ctor from <paramref name="store"/> and <paramref name="db"/>.</summary>
    public EfDataTransaction(EfDataStore<T> store, DatabaseFacade db) {
      this.store= store;
      this.db= db;

      if (null != db.CurrentTransaction) throw new InvalidOperationException("transaction already started");
      var tx= db.BeginTransaction();
      if (tx != db.CurrentTransaction) throw new InvalidOperationException("tx != db.CurrentTransaction");
    }
    ///<inheritdoc/>
    public object Id => db.CurrentTransaction.TransactionId;

    ///<inheritdoc/>
    public void Cancel() {
      store.ResetChanges();
      db.CurrentTransaction.Rollback();
    }

    ///<inheritdoc/>
    public void Commit() {
      store.CommitChanges();
      db.CurrentTransaction.Commit();
    }

    ///<inheritdoc/>
    public void Dispose() {
      db.CurrentTransaction?.Dispose();
      GC.SuppressFinalize(this);
    }
  }

}