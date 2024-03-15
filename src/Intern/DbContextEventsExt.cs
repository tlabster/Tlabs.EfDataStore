using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using Tlabs.Data.Event;

namespace Tlabs.Data.Store.Intern {

  internal static class DbContextEventsExt {
    //Entity state transitions on canceled operation:
    static readonly IReadOnlyDictionary<EntityState, EntityState> CANCELLED_STATE= new Dictionary<EntityState, EntityState> {
      [EntityState.Detached]= EntityState.Detached, //no change
      [EntityState.Added]= EntityState.Detached,    //canceled add -> detached
      [EntityState.Deleted]= EntityState.Modified,  //canceled delete -> best guess modified
      [EntityState.Modified]= EntityState.Unchanged //canceled modify -> unchanged
    };

    public static TEntity GetOriginal<TEntity>(this DbContext ctx, TEntity entity) where TEntity : class, new() {
      return (TEntity)DbContextEventsExt.GetOriginal(ctx.Entry(entity));
    }

    static object GetOriginal(EntityEntry entry) {
      var orgEnt=    Activator.CreateInstance(entry.Entity.GetType())
                  ?? throw EX.New<InvalidOperationException>("Failed to create entity of type: {type}", entry.Entity.GetType().Name);
      foreach (var orgProp in entry.Properties)
        orgProp.Metadata.PropertyInfo?.SetValue(orgEnt, orgProp.OriginalValue);
      return orgEnt;
    }

    public static int SaveChangesWithEvents(this DbContext ctx, Boolean acceptAllChangesOnSuccess = true) {
      var chgTck= ctx.ChangeTracker;
      int cnt= 0;

      if (!chgTck.AutoDetectChangesEnabled)
        chgTck.DetectChanges();

      try {
        var afterEntries= RaiseBeforeEvents(chgTck);
        cnt= ctx.SaveChanges(acceptAllChangesOnSuccess);
        RaiseAfterEvents(afterEntries);
      }
      catch (DbUpdateException dbEx) when (RaiseFailedEvents(chgTck, dbEx) ) { } //catch if swallowed
      catch (Exception e) when (RaiseFailedEvents(chgTck.Entries(), e)) { }

      return cnt;
    }

    static readonly IReadOnlyDictionary<EntityState, Func<DataStoreEvent.ITrigger, object, Func<object>, bool>> RAISE_BEFORE=
    new Dictionary<EntityState, Func<DataStoreEvent.ITrigger, object, Func<object>, bool>> {
      [EntityState.Added]= (trigger, entity, obtainOrg) => trigger.RaiseInserting(entity),
      [EntityState.Modified]= (trigger, entity, obtainOrg) => trigger.RaiseUpdating(entity, obtainOrg),
      [EntityState.Deleted]= (trigger, entity, obtainOrg) => trigger.RaiseDeleting(entity, obtainOrg),
      [EntityState.Unchanged]= (trigger, entity, obtainOrg) => false,
      [EntityState.Detached]= (trigger, entity, obtainOrg) => true
    };

    private static List<AfterSaveEntry> RaiseBeforeEvents(ChangeTracker chgTck) {
      var untriggered= chgTck.Entries().ToList();
      var triggered= new List<EntityEntry>(untriggered.Count);
      var afterSave= new List<AfterSaveEntry>(untriggered.Count);

      while (0 != untriggered.Count) {
        foreach (var entry in untriggered) {
          // var cancel= false;
          // var trigger= DataStoreEvent.Trigger(entry.Entity.GetType());
          // Func<object> obtainOrg= ()=> GetOriginal(entry);
          var cancel= RAISE_BEFORE[entry.State](DataStoreEvent.Trigger(entry.Entity.GetType()), entry.Entity, () => GetOriginal(entry));

          // switch(entry.State) {
          //   case EntityState.Added:
          //     cancel= trigger.RaiseInserting(entry.Entity);
          //   break;

          //   case EntityState.Modified:
          //     cancel= trigger.RaiseUpdating(entry.Entity, obtainOrg);
          //   break;

          //   case EntityState.Deleted:
          //     cancel= trigger.RaiseDeleting(entry.Entity, obtainOrg);
          //   break;
          // }
          triggered.Add(entry);
          if (cancel) entry.State= CANCELLED_STATE[entry.State];
          else afterSave.Add(new AfterSaveEntry(entry, entry.State));
        }
        untriggered.Clear();
        /*+ Before handler execution might have caused additional entity modifications
         *  beeing added to the the changeTracker. Lets add these to untriggered and loop again.
         */
        untriggered.AddRange(chgTck.Entries().Except(triggered, EntityEntryEquality.Comparer));
      }
      return afterSave;
    }

    static readonly IReadOnlyDictionary<EntityState, Action<DataStoreEvent.ITrigger, object>> RAISE_AFTER=
    new Dictionary<EntityState, Action<DataStoreEvent.ITrigger, object>> {
      [EntityState.Added]= (trigger, entity) => trigger.RaiseInserted(entity),
      [EntityState.Modified]= (trigger, entity) => trigger.RaiseUpdated(entity),
      [EntityState.Deleted]= (trigger, entity) => trigger.RaiseDeleted(entity),
      [EntityState.Unchanged]= (trigger, entity) => {},
      [EntityState.Detached]= (trigger, entity) => {}
    };

    private static void RaiseAfterEvents(IEnumerable<AfterSaveEntry> afterEntries)  {
      foreach (var afterEntry in afterEntries) {
        RAISE_AFTER[afterEntry.InitialState](DataStoreEvent.Trigger(afterEntry.Entry.Entity.GetType()), afterEntry.Entry.Entity);

        // var trigger= DataStoreEvent.Trigger(afterEntry.Entry.Entity.GetType());
        // switch (afterEntry.InitialState) {
        //   case EntityState.Added:
        //     trigger.RaiseInserted(afterEntry.Entry.Entity);
        //   break;

        //   case EntityState.Modified:
        //     trigger.RaiseUpdated(afterEntry.Entry.Entity);
        //   break;

        //   case EntityState.Deleted:
        //     trigger.RaiseDeleted(afterEntry.Entry.Entity);
        //   break;
        // }
      }
    }

    private static bool RaiseFailedEvents(ChangeTracker chgTck, DbUpdateException dbEx) {
      if (dbEx.Entries.Any())
        return RaiseFailedEvents(dbEx.Entries, dbEx);

      return RaiseFailedEvents(chgTck.Entries(), dbEx);
    }

    static readonly IReadOnlyDictionary<EntityState, Func<DataStoreEvent.ITrigger, object, Func<object>, Exception, bool>> RAISE_FAILED=
    new Dictionary<EntityState, Func<DataStoreEvent.ITrigger, object, Func<object>, Exception, bool>> {
      [EntityState.Added]= (trigger, entity, obtainOrg, ex) => trigger.RaiseInsertFailed(entity, ex),
      [EntityState.Modified]= (trigger, entity, obtainOrg, ex) => trigger.RaiseUpdateFailed(entity, obtainOrg, ex),
      [EntityState.Deleted]= (trigger, entity, obtainOrg, ex) => trigger.RaiseDeleteFailed(entity, obtainOrg, ex),
      [EntityState.Unchanged]= (trigger, entity, obtainOrg, ex) => false,
      [EntityState.Detached]= (trigger, entity, obtainOrg, ex) => false
    };

    private static bool RaiseFailedEvents(IEnumerable<EntityEntry> failedEntries, Exception ex) {
      var swallow= false;
      foreach (var entry in failedEntries) {
        swallow|= RAISE_FAILED[entry.State](DataStoreEvent.Trigger(entry.Entity.GetType()), entry.Entity, () => GetOriginal(entry), ex);

        // var trigger= DataStoreEvent.Trigger(entry.Entity.GetType());
        // Func<object> obtainOrg= ()=> GetOriginal(entry);
        // switch (entry.State) {
        //   case EntityState.Added:
        //     swallow|= trigger.RaiseInsertFailed(entry.Entity, ex);
        //     break;

        //   case EntityState.Modified:
        //     swallow|= trigger.RaiseUpdateFailed(entry.Entity, obtainOrg, ex);
        //     break;

        //   case EntityState.Deleted:
        //     swallow|= trigger.RaiseDeleteFailed(entry.Entity, obtainOrg, ex);
        //     break;
        // }
      }
      return swallow;
    }

    struct AfterSaveEntry {
      public EntityEntry Entry;
      public EntityState InitialState;
      public AfterSaveEntry(EntityEntry entry, EntityState state) { this.Entry= entry; this.InitialState= state; }
    }


    private sealed class EntityEntryEquality : IEqualityComparer<EntityEntry> {
      public Boolean Equals(EntityEntry? x, EntityEntry? y) => ReferenceEquals(x?.Entity, y?.Entity);
      public Int32 GetHashCode(EntityEntry obj) => obj.Entity.GetHashCode();
      public static readonly EntityEntryEquality Comparer= new EntityEntryEquality();
      private EntityEntryEquality() { }
    }

  }

}
