using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Tlabs.Config;
using Tlabs.Data.Entity;
using Tlabs.Data.Store;


namespace Tlabs.Data.Store {
  //TODO: Move this into Tlabs.Data
  /// <summary>Interface of a <see cref="IDataStore"/> model configuration.</summary>
  public interface IDStoreConfigModel {
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



  /// <summary>Aggregation of <see cref="IDStoreConfigModel"/>(s).</summary>
  public class AggregatingDStoreCtxConfigurator : IDStoreCtxConfigurator {
    readonly IEnumerable<IDStoreConfigModel> modelConfigs;

    ///<summary>Ctor from enumeration of <see cref="IDStoreConfigModel"/> to be aggregated.</summary>
    public AggregatingDStoreCtxConfigurator(IEnumerable<IDStoreConfigModel> modelConfigs) {
      this.modelConfigs= modelConfigs;
    }

    ///<inheritdoc/>
    public virtual void ConfigureModel(ModelBuilder modBuilder) {
      foreach (var modCfg in modelConfigs)
        modCfg.ConfigureModel(modBuilder);
    }

    ///<inheritdoc/>
    public virtual void ConfigureDb(DbContextOptionsBuilder optBuilder) {
      if (optBuilder.IsConfigured) return; //allready configured
      
      foreach (var modCfg in modelConfigs)
        modCfg.ConfigureDb(optBuilder);
    }

  }

}