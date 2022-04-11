using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

using Tlabs.Data.Entity;

namespace Tlabs.Data.Store {

  /// <summary>Interface of a <see cref="DStoreContext{T}"/> to setup the application specific configuration.</summary>
  public interface IDStoreCtxConfigurator {
    /// <summary>Configure the data model using the <paramref name="modBuilder"/>.</summary>
    /// <remarks>
    /// This model configuration is called only once per implementation type.
    /// </remarks>
    void ConfigureModel(ModelBuilder modBuilder);

    /// <summary>Configure the context options using the <paramref name="optBuilder"/>.</summary>
    /// <remarks>
    /// This context option configuration is called for each context instance that is created.
    /// </remarks>
    void ConfigureDb(DbContextOptionsBuilder optBuilder);
  }

  ///<summary>Generic EF Core <see cref="DbContext"/>.</summary>
  ///<remarks>
  ///The application specific entity model must be configured using a
  ///<see cref="IDStoreCtxConfigurator"/> implementation of type <typeparamref name="T"/>.
  ///</remarks>
  public class DStoreContext<T> : DbContext where T : IDStoreCtxConfigurator {
    readonly IDStoreCtxConfigurator ctxCfg;
    readonly ILogger log;


    ///<summary>Ctor from see cref="DbContextOptions{T}"/> and <paramref name="log"/>.</summary>
    public DStoreContext(DbContextOptions<DStoreContext<T>> opt, T ctxCfg, ILogger<DStoreContext<T>> log) : base(opt) {
    this.log= log;
    log.LogTrace("Db context created.");
    this.ctxCfg= ctxCfg;
  }

    ///<inherit/>
    protected override void OnConfiguring(DbContextOptionsBuilder optBuilder) {
      //context MUST be configured per DbContextOptions passed to the ctor
      if (!optBuilder.IsConfigured) throw new InvalidOperationException($"{nameof(DbContextOptionsBuilder)} must be configured.");
      ctxCfg.ConfigureDb(optBuilder);
    }

    ///<inherit/>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
      log.LogTrace("Building db model for {type}", this.GetType());
      ctxCfg.ConfigureModel(modelBuilder);
    }
  }

}