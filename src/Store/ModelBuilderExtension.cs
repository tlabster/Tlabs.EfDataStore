using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;

using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Tlabs.Data.Entity.Intern;

namespace Tlabs.Data.Store {

  /// <summary>Model builder extension.</summary>
  ///<remarks>Supports schema scoped entity model configuration:
  ///<code>
  ///public void ConfigureModel(ModelBuilder modelConfig) {
  ///  modelConfig.Schema("Identity", modBuilder => {
  ///    modelConfig.Entity&lt;User>(userBuilder => { .. });
  ///  });
  ///  ...
  /// }
  ///</code>
  ///</remarks>
  public static class ModelBuilderExtension {
    private const string TestAssemblyPrefix = "xunit.runner.visualstudio.testadapter";

    /// <summary>Configure entity of type <typeparamref name="T"/> with <paramref name="schema"/> in the model.</summary>
    public static ModelBuilder Entity<T>(this ModelBuilder bld, string schema, Action<EntityTypeBuilder<T>> buildAction) where T : class {
      bld.Entity<T>(entBuilder => {
        var declTypeName= typeof(T).DeclaringType?.Name ?? string.Empty;
        var tableName= declTypeName + entBuilder.Metadata.GetDefaultTableName();
        entBuilder.ToTable(tableName, schema);
        buildAction(entBuilder);
      });
      return bld;
    }

    /// <summary>Start a <paramref name="schema"/> scope for <paramref name="modelBuilderAction"/>.</summary>
    public static ModelBuilder Schema(this ModelBuilder bld, string schema, Action<SchemaScopedModelBuilder> modelBuilderAction) {
      modelBuilderAction(new SchemaScopedModelBuilder(bld, schema));
      return bld;
    }

    /// <summary>Configure entity of type <typeparamref name="T"/> with <see cref="SchemaScopedModelBuilder"/>.</summary>
    public static ref SchemaScopedModelBuilder Entity<T>(this ref SchemaScopedModelBuilder bld, Action<EntityTypeBuilder<T>> buildAction) where T : class {
      bld.ModelBuilder.Entity<T>(bld.Schema, buildAction);
      return ref bld;
    }

    /// <summary>Configure document entity of type <typeparamref name="TDocEntity"/> and <typeparamref name="TBody"/> type with <see cref="SchemaScopedModelBuilder"/>.</summary>
    public static ref SchemaScopedModelBuilder DocEntity<TDocEntity, TBody>(this ref SchemaScopedModelBuilder bld, Action<EntityTypeBuilder<TDocEntity>> buildAction)
      where TDocEntity : BaseDocument<TDocEntity> where TBody : BaseDocument<TDocEntity>.BodyData {
      bld.ModelBuilder.Entity<TDocEntity>(bld.Schema, buildAction);
      bld.ModelBuilder.Entity<TDocEntity>(bld.Schema, builder => {
        builder.HasOne(d => d.Body)
               .WithOne(b => b.Document)
               .HasForeignKey<TBody>(b => b.Id)
               .IsRequired();
      });
      var schema= bld.Schema;
      bld.ModelBuilder.Entity<TBody>(builder => {
        var bodyTableName= typeof(TDocEntity).Name + "Body";
        builder.ToTable(bodyTableName, schema)
               .HasKey(b => b.Id);
        builder.HasOne(b => b.Document)
               .WithOne(d => (TBody)d.Body).IsRequired();
      });
      return ref bld;
    }

    /// <summary>Configure document entity of type <typeparamref name="TDocEntity"/> type with <see cref="SchemaScopedModelBuilder"/>.</summary>
    public static ref SchemaScopedModelBuilder JsonDocumentEntity<TDocEntity>(this ref SchemaScopedModelBuilder bld, Action<EntityTypeBuilder<TDocEntity>> buildAction)
      where TDocEntity : DocumentEntity {
      bld.ModelBuilder.Entity<TDocEntity>(bld.Schema, buildAction);
      bld.ModelBuilder.Entity<TDocEntity>(bld.Schema, builder => {
        builder.HasJsonbConversion(b => b.Properties);
      });
      var schema= bld.Schema;
      return ref bld;
    }

    /// <summary>Configure document entity of type <typeparamref name="TDocEntity"/> type with <see cref="SchemaScopedModelBuilder"/>.</summary>
    public static ref SchemaScopedModelBuilder EditableJsonDocumentEntity<TDocEntity>(this ref SchemaScopedModelBuilder bld, Action<EntityTypeBuilder<TDocEntity>> buildAction)
      where TDocEntity : EditableDocumentEntity {
      bld.ModelBuilder.Entity<TDocEntity>(bld.Schema, buildAction);
      bld.ModelBuilder.Entity<TDocEntity>(bld.Schema, builder => {
        builder.HasJsonbConversion(b => b.Properties);
      });
      var schema= bld.Schema;
      return ref bld;
    }

    /// <summary>
    /// Creates a mapping of a given <paramref name="propertyExpression"/> to a jsonb field
    /// </summary>
    /// <typeparam name="TEntity">Type of the entity being configured</typeparam>
    /// <typeparam name="TProperty">Type of the property</typeparam>
    /// <param name="builder">Entity builder</param>
    /// <param name="propertyExpression">Expression representing the given property</param>
    /// <returns></returns>
    public static PropertyBuilder<TProperty?> HasJsonbConversion<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TProperty?>> propertyExpression
    ) where TEntity : class {
      var isTestEnvironment = AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(a => a.FullName != null && a.FullName.StartsWith(TestAssemblyPrefix, StringComparison.OrdinalIgnoreCase));

      var propertyBuilder = builder.Property(propertyExpression);

      if (isTestEnvironment) {
        propertyBuilder.HasConversion(
            v => JsonSerializer.Serialize(v, default(JsonSerializerOptions)),
            v => v != null ? JsonSerializer.Deserialize<TProperty>(v, default(JsonSerializerOptions)) : default(TProperty)
        );
      }
      else {
        propertyBuilder.HasColumnType("jsonb");
      }

      return propertyBuilder;
    }
  }

  /// <summary><see cref="ModelBuilder"/> the db scheme scope.</summary>
  public readonly struct SchemaScopedModelBuilder {
    internal SchemaScopedModelBuilder(ModelBuilder mb, string schema) { this.ModelBuilder= mb; this.Schema= schema; }
    internal ModelBuilder ModelBuilder { get; }
    internal string Schema { get; }
  }
}