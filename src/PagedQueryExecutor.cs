using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading;
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
    public async Task<PagedQueryResult<TModel>> ExecuteAsync<TModel>(
      QuerySpecification<TEntity, TFilterCriteria, TSortCriteria, TSortField> specification,
      Func<TEntity, Task<TModel>> asyncMapper,
      IQueryable<TEntity>? query = null,
      CancellationToken token = default
    ) {

      query ??= dataStore.UntrackedQuery<TEntity>();
      var filteredQuery = specification.Apply(query, filterBuilder, sortBuilder);
      var mappingTasks = filteredQuery.Select(asyncMapper);
      var models = await Task.WhenAll(mappingTasks);

      var totalCount = await GetTotalCountAsync(query, specification.Filter, token);

      return new PagedQueryResult<TModel> {
        Items = models,
        TotalCount = totalCount,
        Page = specification.Filter.Page,
        PageSize = specification.Filter.PageSize
      };
    }

    /// <summary>
    /// Executes a paged query and maps entities to models using an async mapper
    /// </summary>
    public async Task<PagedQueryResult<TModel>> ExecuteAsync<TModel>(
      QuerySpecification<TEntity, TFilterCriteria, TSortCriteria, TSortField> specification,
      Func<TEntity, TModel> mapper,
      IQueryable<TEntity>? query,
      CancellationToken token = default
    ) {
      query ??= dataStore.UntrackedQuery<TEntity>();
      var filteredQuery = specification.Apply(query, filterBuilder, sortBuilder);

      var entities = await filteredQuery.ToListAsync(token);
      var totalCount = await GetTotalCountAsync(query, specification.Filter, token);

      return new PagedQueryResult<TModel> {
        Items = entities.Select(mapper),
        TotalCount = totalCount,
        Page = specification.Filter.Page,
        PageSize = specification.Filter.PageSize
      };
    }


    private async Task<int?> GetTotalCountAsync(IQueryable<TEntity> query, TFilterCriteria? filter, CancellationToken token) {
      var hasFilter = HasFilterCriteria(filter);

      // If no filter is applied, count without limit (should be fast)
      // If filter is applied, use the max count limit to avoid performance issues
      int? maxCount = hasFilter ? filter!.MaxCountLimit : null;

      var count = await (maxCount != null ? query.Take(maxCount.Value).CountAsync(token) : query.CountAsync(token));

      // If limit was applied and reached, return null to indicate unknown total
      if (maxCount.HasValue && count >= maxCount.Value) {
        return null;
      }

      return count;
    }

    private bool HasFilterCriteria(TFilterCriteria? filter) {
      if (filter == null)
        return false;

      var expression = filterBuilder.BuildExpression(filter);
      return expression != null;
    }
  }
}