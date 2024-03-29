﻿using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

using Tlabs.Config;

namespace Tlabs.Data.Store {

  ///<summary>Database server options configurator base class.</summary>
  ///<remarks>
  ///Implementations need to override <see cref="DbServerConfigurator{T}.configureDbOptions(DbContextOptionsBuilder{DStoreContext{T}})"/>.
  ///</remarks>
  public abstract class DbServerConfigurator<T> : IConfigurator<IServiceCollection> where T : class, IDStoreCtxConfigurator {
    ///<summary>Database connection string from configuration.</summary>
    protected string connStr;
    ///<summary>Configuration properties.</summary>
    protected readonly IDictionary<string, string> config;
    ///<summary>Default ctor.</summary>
    protected DbServerConfigurator() : this(null) { }
    ///<summary>Ctor from <paramref name="config"/>.</summary>
    protected DbServerConfigurator(IDictionary<string, string>? config) {
      this.config= config ?? new Dictionary<string, string>(0);
      if (!this.config.TryGetValue("connection", out var connStr)) throw new Tlabs.AppConfigException("Missing 'connection' config proerpty.");
      this.connStr= connStr;
    }

    ///<inheritdoc/>
    public void AddTo(IServiceCollection services, IConfiguration cfg) {
      var log= Tlabs.App.Logger<DbServerConfigurator<T>>();

      var opt= new DbServerCfgContextOptionsBuilder<DStoreContext<T>>(services)
              .UseLoggerFactory(Tlabs.App.LogFactory)   //***TODO: for a strange reason w/o this line, log entries from Microsoft.EntityFrameworkCore won't appear */
           // .EnableSensitiveDataLogging()
              .ConfigureWarnings(warnings => {
                warnings.Log(RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning);
                warnings.Default(WarningBehavior.Log);
              });

      configureDbOptions(opt);

      services.AddSingleton(opt.Options);
      log.LogDebug("{dstore} added to service colletion.", nameof(DbContextOptions<DStoreContext<T>>));
    }

    ///<summary><see cref="DbContextOptions"/> configurator.</summary>
    protected abstract void configureDbOptions(DbContextOptionsBuilder<DStoreContext<T>> opt);

    ///<summary><see cref="DbContextOptionsBuilder{T}"/> implementation with <see cref="SvcCollection"/>.</summary>
    public class DbServerCfgContextOptionsBuilder<C> : DbContextOptionsBuilder<C>, IDbServerCfgContextOptionsBuilder where C : DbContext {
      ///<summary>Ctor from <paramref name="svcColl"/>.</summary>
      public DbServerCfgContextOptionsBuilder(IServiceCollection svcColl) => this.SvcCollection= svcColl;
      ///<summary><see cref="IServiceCollection"/></summary>
      public IServiceCollection SvcCollection { get; }
    }

  }

  ///<summary>interfae of an DbContextOptionsBuilder that provides access to a <see cref="IServiceCollection"/></summary>
  public interface IDbServerCfgContextOptionsBuilder {
    ///<summary><see cref="IServiceCollection"/></summary>
    public IServiceCollection SvcCollection { get; }
  }

}