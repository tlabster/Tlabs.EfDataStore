using Microsoft.EntityFrameworkCore;

using Tlabs.Config;
using Tlabs.Data.Entity;

namespace Tlabs.Data.Store {  //required namespace to supporte generated migrations

  /// <summary><see cref="DocumentSchema"/> EF model.</summary>
  public class DocumentSchemaModel : SelfEnumConfigurator<IDStoreConfigModel>, IDStoreConfigModel {

    /// <inheritdoc/>
    public void ConfigureDb(DbContextOptionsBuilder optBuilder) {
      return; // do nothing
    }

    /// <inheritdoc/>
    public void ConfigureModel(ModelBuilder modelConfig) {
      modelConfig.Schema("Meta", modBuilder => {

        modBuilder.Entity<DocumentSchema>(docType => {
          docType.HasKey(d => d.Id);
        });

        modBuilder.Entity<DocumentSchema.Field>(dtAttribBuilder => {
          dtAttribBuilder.HasKey(a => a.Id);
          dtAttribBuilder.HasOne(a => a.Schema)
                         .WithMany(d => d.Fields).IsRequired();
        });

        modBuilder.Entity<DocumentSchema.ValidationRule>(validBuilder => {
          validBuilder.HasKey(v => v.Id);
          validBuilder.HasOne(v => v.Schema)
                      .WithMany(d => d.Validations).IsRequired();
        });
        modBuilder.Entity<DocumentSchema.EvaluationRef>(evalBuilder => {
          evalBuilder.HasKey(v => v.Id);
          evalBuilder.HasOne(v => v.Schema)
                     .WithMany(d => d.EvalReferences).IsRequired();
        });
      });
    }
  }
}