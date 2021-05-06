using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tlabs.Data.Entity.Intern;

namespace Tlabs.Data.Store {

#nullable enable
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
      where TDocEntity : BaseDocument<TDocEntity> where TBody : BaseDocument<TDocEntity>.BodyData
    {
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

    /// <summary><see cref="ModelBuilder"/> the db scheme scope.</summary>
    public readonly struct SchemaScopedModelBuilder {
      internal SchemaScopedModelBuilder(ModelBuilder mb, string schema) { this.ModelBuilder= mb; this.Schema= schema; }
      internal ModelBuilder ModelBuilder { get; }
      internal string Schema { get; }
    }
  }
#nullable disable
}