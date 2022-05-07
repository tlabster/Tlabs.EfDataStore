using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

using Tlabs.Config;

namespace Tlabs.Data.Store {

  //TODO: Move this into Tlabs.Data
  ///<summary><see cref="IDataStore"/> configurator.</summary>
  public class DataStoreConfigurator<T> : IConfigurator<IServiceCollection> where T : class, IDStoreCtxConfigurator {

    ///<inheritdoc/>
    public void AddTo(IServiceCollection services, IConfiguration cfg) {
      var log= Tlabs.App.Logger<DataStoreConfigurator<T>>();

      services.AddSingleton<T>();

      services.AddDbContext<DStoreContext<T>>();
      log.LogDebug("{ctx} added to service colletion.", nameof(DStoreContext<T>));

      services.AddScoped<IDataStore, EfDataStore<DStoreContext<T>>>();
      log.LogDebug("{dstore} added to service colletion.", nameof(EfDataStore<DStoreContext<T>>));
    }

  }

}