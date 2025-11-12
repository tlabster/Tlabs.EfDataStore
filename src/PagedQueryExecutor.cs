using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Tlabs.Data.Filter;
using Tlabs.Data.Repo.Intern;

namespace Tlabs.Data.Filter {
  /// <summary>
  /// Helper class to execute paged queries and map results to models
  /// </summary>
  public class PagedQueryExecutor<TEntity, TFilterCriteria, TSortCriteria, TSortField> : IPagedQueryExecutor<TEntity, TFilterCriteria, TSortCriteria, TSortField>
      where TEntity : class
      where TFilterCriteria : PagedFilterCriteria
      where TSortCriteria : ISortCriteria<TSortField>
      where TSortField : struct, Enum {

    private readonly IDataStore dataStore;
    private readonly IFilterBuilder<TEntity, TFilterCriteria> filterBuilder;
    private readonly ISortBuilder<TEntity, TSortCriteria, TSortField> sortBuilder;

    /// <summary>
    /// Ctor from <paramref name="dataStore"/> <paramref name="filterBuilder"/> and <paramref name="sortBuilder"/>
    /// </summary>
    public PagedQueryExecutor(IDataStore dataStore,
            IFilterBuilder<TEntity, TFilterCriteria> filterBuilder,
            ISortBuilder<TEntity, TSortCriteria, TSortField> sortBuilder) {
      this.dataStore = dataStore;
      this.filterBuilder = filterBuilder;
      this.sortBuilder = sortBuilder;
    }

    /// <summary>
    /// Executes a paged query and maps entities to models using an async mapper
    /// </summary>
    public async Task<PagedResult<TModel>> ExecuteAsync<TModel>(
      QuerySpecification<TEntity, TFilterCriteria, TSortCriteria, TSortField> specification,
      Func<TEntity, Task<TModel>> asyncMapper,
      IQueryable<TEntity>? query = null
    ) {

      var entities = query ?? dataStore.UntrackedQuery<TEntity>();
      entities = specification.Apply(entities, filterBuilder, sortBuilder);

      var totalCount = await entities.CountAsync();

      var mappingTasks = entities.Select(asyncMapper);
      var models = await Task.WhenAll(mappingTasks);

      return new PagedResult<TModel> {
        Items = models,
        TotalCount = totalCount,
        Page = specification.Filter.Page,
        PageSize = specification.Filter.PageSize
      };
    }

    /// <summary>
    /// Executes a paged query and maps entities to models using an async mapper
    /// </summary>
    public async Task<PagedResult<TModel>> ExecuteAsync<TModel>(
      QuerySpecification<TEntity, TFilterCriteria, TSortCriteria, TSortField> specification,
      Func<TEntity, TModel> mapper,
      IQueryable<TEntity>? query
    ) {
      var entities = query ?? dataStore.UntrackedQuery<TEntity>();
      entities = specification.Apply(entities, filterBuilder, sortBuilder);

      var totalCount = await entities.CountAsync();

      var models = entities.Select(mapper);

      return new PagedResult<TModel> {
        Items = models,
        TotalCount = totalCount,
        Page = specification.Filter.Page,
        PageSize = specification.Filter.PageSize
      };
    }
  }
}