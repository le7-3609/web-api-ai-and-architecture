using Microsoft.EntityFrameworkCore;
using Repositories;
using Microsoft.Data.Sqlite;

namespace Tests.IntegrationTests
{
    public class DatabaseFixture : IDisposable
    {
        private SqliteConnection _connection;
        public MyShopContext Context { get; private set; }

        public DatabaseFixture()
        {
            // Create in-memory SQLite connection (no external DB needed, CI-friendly)
            _connection = new SqliteConnection("DataSource=:memory:;");
            _connection.Open();

            var options = new DbContextOptionsBuilder<MyShopContext>()
                .UseSqlite(_connection)
                .Options;

            // Create a custom context that doesn't use getdate()
            Context = new TestMyShopContext(options);
            
            // Create database schema using EF Core
            CreateSQLiteSchema();
        }

        private void CreateSQLiteSchema()
        {
            // Disable foreign key constraints by executing PRAGMA before any operations
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA foreign_keys = OFF;";
                    command.ExecuteNonQuery();
                }
            }
            catch { }
            
            // Delete and recreate database using EF Core - this ensures schema matches the EF entities exactly
            try
            {
                Context.Database.EnsureDeletedAsync().Wait();
            }
            catch { }
            
            Context.Database.EnsureCreatedAsync().Wait();
            
            // Ensure PRAGMA is still disabled after schema creation
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA foreign_keys = OFF;";
                    command.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public void ClearDatabase()
        {
            try
            {
                Context.Database.EnsureDeleted();
                Context.Database.EnsureCreated();

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA foreign_keys = OFF;";
                    command.ExecuteNonQuery();
                }

                Context.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClearDatabase error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Context?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
    }

    // Custom context that skips getdate() configuration for tests
    public class TestMyShopContext : MyShopContext
    {
        public TestMyShopContext(DbContextOptions<MyShopContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Override Order's OrderDate to use NULL instead of getdate()
            modelBuilder.Entity<Entities.Order>(entity =>
            {
                entity.Property(e => e.OrderDate).HasDefaultValueSql(null);
            });
            
             modelBuilder.Entity<Entities.User>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql(null);
            });

            modelBuilder.Entity<Entities.Platform>(entity =>
            {
                entity.Property(e => e.PlatformPrompt).IsRequired(false);
            });

            modelBuilder.Entity<Entities.MainCategory>(entity =>
            {
                entity.Property(e => e.MainCategoryPrompt).IsRequired(false);
            });

            modelBuilder.Entity<Entities.SiteType>(entity =>
            {
                entity.Property(e => e.SiteTypeDescription).IsRequired(false);
                entity.Property(e => e.SiteTypeNamePrompt).IsRequired(false);
                entity.Property(e => e.SiteTypeDescriptionPrompt).IsRequired(false);
            });

            modelBuilder.Entity<Entities.Product>(entity =>
            {
                entity.Property(e => e.ProductPrompt).IsRequired(false);
            });

            modelBuilder.Entity<Entities.SubCategory>(entity =>
            {
                entity.Property(e => e.SubCategoryPrompt).IsRequired(false);
                entity.Property(e => e.CategoryDescription).IsRequired(false);
            });
        }
    }
}

