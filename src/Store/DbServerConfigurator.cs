using System.Collections.Generic;
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
    IDictionary<string, string> config;
    ///<summary>Default ctor.</summary>
    protected DbServerConfigurator() : this(null) { }
    ///<summary>Ctor from <paramref name="config"/>.</summary>
    protected DbServerConfigurator(IDictionary<string, string> config) {
      this.config= config ?? new Dictionary<string, string>(0);
    }

    ///<inherit/>
    public void AddTo(IServiceCollection services, IConfiguration cfg) {
      var log= Tlabs.App.Logger<DbServerConfigurator<T>>();
      if (!config.TryGetValue("connection", out connStr)) throw new Tlabs.AppConfigException("Missing 'connection' config proerpty.");

      var opt= new DbContextOptionsBuilder<DStoreContext<T>>()
              .UseLoggerFactory(Tlabs.App.LogFactory)   //***TODO: for a strange reason w/o this line, log entries from Microsoft.EntityFrameworkCore won't appear */
           // .EnableSensitiveDataLogging()
              .ConfigureWarnings(warnings => {
                // warnings.Log(RelationalEventId.QueryClientEvaluationWarning);
                warnings.Throw(RelationalEventId.QueryClientEvaluationWarning);
                warnings.Log(RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning);
                warnings.Default(WarningBehavior.Log);
              });

      configureDbOptions(opt);

      services.AddSingleton(opt.Options);
      log.LogDebug("{dstore} added to service colletion.", nameof(DbContextOptions<DStoreContext<T>>));
    }

    ///<summary><see cref="DbContextOptions"/> configurator.</summary>
    protected abstract void configureDbOptions(DbContextOptionsBuilder<DStoreContext<T>> opt);
  }

}