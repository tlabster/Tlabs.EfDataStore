using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tlabs.Data.Store {

  ///<summary>Entityframework core database transcation.</summary>
  public class EfDataTransaction<T> : IDataTransaction where T : DbContext {
    private EfDataStore<T> store;
    private DatabaseFacade db;

    ///<summary>Ctor from <paramref name="store"/> and <paramref name="db"/>.</summary>
    public EfDataTransaction(EfDataStore<T> store, DatabaseFacade db) {
      this.store= store;
      this.db= db;

      if (null != db.CurrentTransaction) throw new InvalidOperationException("transaction already started");
      var tx= db.BeginTransaction();
      if (tx != db.CurrentTransaction) throw new InvalidOperationException("tx != db.CurrentTransaction");
    }
    ///<inherit/>
    public object Id => db.CurrentTransaction.TransactionId;

    ///<inherit/>
    public void Cancel() {
      store.ResetChanges();
      db.CurrentTransaction.Rollback();
    }

    ///<inherit/>
    public void Commit() {
      store.CommitChanges();
      db.CurrentTransaction.Commit();
    }

    ///<inherit/>
    public void Dispose() => db.CurrentTransaction?.Dispose();
  }

}