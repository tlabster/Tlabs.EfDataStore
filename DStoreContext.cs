using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

using Tlabs.Data.Entity;

namespace Tlabs.Data.Store {

  /// <summary>Interface of a <see cref="DbContext"/> decorator to setup the application specific data model.</summary>
  public interface IEntityModelDecorator {
    /// <summary>Configure the data model using the <paramref name="modBuilder"/>.</summary>
    void ConfigureModel(ModelBuilder modBuilder);
  }

  ///<summary>Generic EF Core <see cref="DbContext"/>.</summary>
  ///<remarks>
  ///The application specific entity model must be configured using a
  ///<see cref="IEntityModelDecorator"/> implementation of type <typeparamref name="T"/>.
  ///</remarks>
  public class DStoreContext<T> : DbContext where T : IEntityModelDecorator {
    private IEntityModelDecorator modDeco;
    private ILogger log;


    ///<summary>Ctor from see cref="DbContextOptions{T}"/> and <paramref name="log"/>.</summary>
    public DStoreContext(DbContextOptions<DStoreContext<T>> opt, T modDeco, ILogger<DStoreContext<T>> log) : base(opt) {
    this.log= log;
    log.LogTrace("Db context created.");
    this.modDeco= modDeco;
  }

    ///<inherit/>
    protected override void OnConfiguring(DbContextOptionsBuilder optBuilder) {
      //context MUST be configured per DbContextOptions passed to the ctor
      if (!optBuilder.IsConfigured) throw new InvalidOperationException($"{nameof(DbContextOptionsBuilder)} must be configured.");
    }

    ///<inherit/>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      log.LogTrace($"Building db model for {this.GetType()}");
      modDeco.ConfigureModel(modelBuilder);
    }
  }

}