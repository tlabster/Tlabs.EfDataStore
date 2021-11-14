using Microsoft.EntityFrameworkCore;
using Tlabs.Config;
using Tlabs.Data.Entity;

namespace Tlabs.Data.Store {

  /// <summary>User and Roles EF model.</summary>
  public class UserEnityModel : SelfEnumConfigurator<IDStoreConfigModel>, IDStoreConfigModel {

    /// <inheritdoc/>
    public void ConfigureDb(DbContextOptionsBuilder optBuilder) {
      return; // do nothing
    }

    /// <inheritoc/>
    public void ConfigureModel(ModelBuilder modelConfig) {
      modelConfig.Schema("Identity", modBuilder => {

        modBuilder.Entity<User>(userBuilder => {
          userBuilder.HasKey(u => u.Id);
          userBuilder.HasIndex(u => u.UserName).IsUnique();
          userBuilder.HasIndex(u => u.Email).IsUnique();
          /* userManager.FindByXXX currently not used, but would search by NormalizedXXX properties...
           * //userBuilder.HasIndex(u => u.NormalizedUserName).IsUnique();
           * //userBuilder.HasIndex(u => u.NormalizedEmail).IsUnique();
          */
          userBuilder.HasOne(u => u.Locale)
                     .WithMany();
        });

        modBuilder.Entity<Role>(roleBuilder => {
          roleBuilder.HasKey(r => r.Id);
          roleBuilder.HasIndex(r => r.Name).IsUnique();
        });

        modBuilder.Entity<User.RoleRef>(roleBuilder => {
          roleBuilder.HasKey("UserId", "RoleId");
          roleBuilder.HasOne(r => r.User)
                    .WithMany(r => r.Roles).IsRequired();

          roleBuilder.HasOne(r => r.Role)
                    .WithMany(r => r.Users);
        });

        modBuilder.Entity<ApiKey>(apiKeyBuilder => {
          apiKeyBuilder.HasKey(r => r.Id);
          apiKeyBuilder.HasIndex(r => r.TokenName).IsUnique();
          apiKeyBuilder.HasIndex(r => r.Hash).IsUnique();
        });

        modBuilder.Entity<ApiKey.RoleRef>(roleBuilder => {
          roleBuilder.HasKey("ApiKeyId", "RoleId");
          roleBuilder.HasOne(r => r.ApiKey)
                    .WithMany(r => r.Roles).IsRequired();

          roleBuilder.HasOne(r => r.Role)
                    .WithMany(r => r.ApiKeys);
        });

        modBuilder.Entity<AuditRecord>(auditBuilder => {
          auditBuilder.HasKey(a => a.Id);
          auditBuilder.HasIndex(a => a.Id);
#if SQLSERVER
                      .HasFilter(null)
                      .IncludeProperties(a => new {
                        a.Id
                      });
#endif
        });

        modBuilder.Entity<Locale>(loc => {
          loc.HasKey(l => l.Id);
          loc.HasIndex(l => l.Lang)
              .IsUnique();
        });
      });
    }
  }

}