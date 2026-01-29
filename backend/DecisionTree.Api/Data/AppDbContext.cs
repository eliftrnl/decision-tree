using DecisionTree.Api.Entities;
using Microsoft.EntityFrameworkCore;
using DecisionTreeEntity = DecisionTree.Api.Entities.DecisionTree;

namespace DecisionTree.Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<DecisionTreeEntity> DecisionTrees => Set<DecisionTreeEntity>();
    public DbSet<DecisionTreeTable> DecisionTreeTables => Set<DecisionTreeTable>();
    public DbSet<TableColumn> TableColumns => Set<TableColumn>();
    public DbSet<DecisionTreeData> DecisionTreeData => Set<DecisionTreeData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // === DecisionTree ===
        modelBuilder.Entity<DecisionTreeEntity>(e =>
        {
            e.ToTable("decision_tree");

            e.HasKey(x => x.Id);

            e.Property(x => x.Code)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.StatusCode)
                .HasConversion<string>()
                .HasColumnType("varchar(30)")
                .HasMaxLength(30)
                .IsRequired();

            e.Property(x => x.SchemaVersion)
                .HasDefaultValue(1)
                .IsRequired();

            e.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .ValueGeneratedOnAdd()
                .IsRequired();

            e.Property(x => x.UpdatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired(false);

            // ✅ Tek unique index: DB'deki isimle aynı
            e.HasIndex(x => x.Code)
                .IsUnique()
                .HasDatabaseName("uq_decision_tree_code");
        });

        // === DecisionTreeTable ===
        modelBuilder.Entity<DecisionTreeTable>(e =>
        {
            e.ToTable("decision_tree_table");

            e.HasKey(x => x.Id);

            e.Property(x => x.TableCode)
                .HasMaxLength(150)
                .IsRequired();

            e.Property(x => x.TableName)
                .HasMaxLength(200);

            e.Property(x => x.Direction)
                .HasConversion<string>()
                .HasColumnType("varchar(30)")
                .HasMaxLength(30)
                .IsRequired();

            e.Property(x => x.StatusCode)
                .HasConversion<string>()
                .HasColumnType("varchar(30)")
                .HasMaxLength(30)
                .IsRequired();

            e.HasIndex(x => new { x.DecisionTreeId, x.TableCode }).IsUnique();

            e.HasOne(x => x.DecisionTree)
                .WithMany(x => x.Tables)
                .HasForeignKey(x => x.DecisionTreeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // === TableColumn ===
        modelBuilder.Entity<TableColumn>(e =>
        {
            e.ToTable("table_column");

            e.HasKey(x => x.Id);

            e.Property(x => x.TableId)
                .HasColumnType("int")
                .IsRequired();

            e.Property(x => x.ColumnCode)
                .HasMaxLength(150)
                .IsRequired();

            e.Property(x => x.ColumnName)
                .HasMaxLength(200)
                .IsRequired(false);

            e.Property(x => x.ExcelHeaderName)
                .HasMaxLength(150)
                .IsRequired(false);

            e.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            e.Property(x => x.DataType)
                .HasConversion<string>()
                .HasColumnType("varchar(30)")
                .HasMaxLength(30)
                .IsRequired();

            e.Property(x => x.IsRequired)
                .HasColumnType("tinyint(1)")
                .IsRequired();

            e.Property(x => x.StatusCode)
                .HasConversion<string>()
                .HasColumnType("varchar(30)")
                .HasMaxLength(30)
                .IsRequired();

            e.Property(x => x.OrderIndex)
                .HasColumnType("int")
                .IsRequired();

            e.Property(x => x.Format)
                .HasColumnType("varchar(50)")
                .HasMaxLength(50)
                .IsRequired(false);

            e.Property(x => x.MaxLength)
                .HasColumnType("int")
                .IsRequired(false);

            e.Property(x => x.Precision)
                .HasColumnType("int")
                .IsRequired(false);

            e.Property(x => x.Scale)
                .HasColumnType("int")
                .IsRequired(false);

            e.Property(x => x.ColumnType)
                .HasColumnType("varchar(50)")
                .HasMaxLength(50)
                .IsRequired(false);

            e.Property(x => x.ValidFrom)
                .HasColumnType("datetime(6)")
                .IsRequired(false);

            e.Property(x => x.ValidTo)
                .HasColumnType("datetime(6)")
                .IsRequired(false);

            e.HasIndex(x => new { x.TableId, x.ColumnCode }).IsUnique();

            e.HasOne(x => x.Table)
                .WithMany(x => x.Columns)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // === DecisionTreeData ===
        modelBuilder.Entity<DecisionTreeData>(e =>
        {
            e.ToTable("decision_tree_data");

            e.HasKey(x => x.Id);

            e.Property(x => x.RowDataJson)
                .HasColumnType("json")
                .IsRequired();

            e.Property(x => x.RowCode)
                .HasMaxLength(100)
                .IsRequired(false);

            e.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .ValueGeneratedOnAdd();

            e.Property(x => x.UpdatedAtUtc)
                .HasColumnType("datetime(6)");

            e.HasOne(x => x.Table)
                .WithMany()
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.TableId);
        });
    }

    public override int SaveChanges()
    {
        ApplyUpdatedAtUtc();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyUpdatedAtUtc();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyUpdatedAtUtc()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                var prop = entry.Metadata.FindProperty("UpdatedAtUtc");
                if (prop is not null)
                {
                    entry.Property("UpdatedAtUtc").CurrentValue = now;
                }
            }
        }
    }
}
