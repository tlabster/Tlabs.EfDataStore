using System;

using Microsoft.Extensions.DependencyInjection;

using Tlabs.Data.Filter;

namespace Tlabs.EfDataStore {
  /// <summary>
  /// Service Collection extensions for registration of generic filtering classes
  /// </summary>
  public static class FilterServiceCollectionExtensions {
    /// <summary>
    /// Generic registration helper using strongly-typed sorting
    /// </summary>
    public static IServiceCollection AddQueryServices<TEntity, TFilterCriteria, TSortCriteria, TFilterBuilder, TSortBuilder, TSortField>(
        this IServiceCollection services)
        where TEntity : class
        where TFilterCriteria : PagedFilterCriteria
        where TSortCriteria : ISortCriteria<TSortField>
        where TFilterBuilder : class, IFilterBuilder<TEntity, TFilterCriteria>
        where TSortBuilder : class, ISortBuilder<TEntity, TSortCriteria, TSortField>
        where TSortField : struct, Enum {
      services.AddScoped<IFilterBuilder<TEntity, TFilterCriteria>, TFilterBuilder>();
      services.AddScoped<ISortBuilder<TEntity, TSortCriteria, TSortField>, TSortBuilder>();
      services.AddScoped<IPagedQueryExecutor<TEntity, TFilterCriteria, TSortCriteria, TSortField>, PagedQueryExecutor<TEntity, TFilterCriteria, TSortCriteria, TSortField>>();

      return services;
    }
  }
}