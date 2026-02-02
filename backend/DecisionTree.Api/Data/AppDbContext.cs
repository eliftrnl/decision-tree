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
    public DbSet<ValidationLog> ValidationLogs => Set<ValidationLog>();
    public DbSet<ColumnValueMapping> ColumnValueMappings => Set<ColumnValueMapping>();

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

            e.Property(x => x.TableName)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Direction)
                
                
                
                .IsRequired();

            e.Property(x => x.StatusCode)
                
                
                
                .IsRequired();

            e.HasIndex(x => new { x.DecisionTreeId, x.TableName }).IsUnique();

            e.HasOne(x => x.DecisionTree)
                .WithMany(x => x.Tables)
                .HasForeignKey(x => x.DecisionTreeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // === TableColumn ===
        modelBuilder.Entity<TableColumn>(e =>
        {
            e.ToTable("decision_tree_column");

            e.HasKey(x => x.Id);

            e.Property(x => x.TableId)
                .HasColumnType("int")
                .IsRequired();

            e.Property(x => x.ColumnName)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.ExcelHeaderName)
                .HasMaxLength(150)
                .IsRequired(false);

            e.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired(false);

            e.Property(x => x.DataType)
                
                
                
                .IsRequired();

            e.Property(x => x.IsRequired)
                .HasColumnType("tinyint(1)")
                .IsRequired();

            e.Property(x => x.StatusCode)
                
                
                
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

            e.Property(x => x.ValidFrom)
                .HasColumnType("datetime(6)")
                .IsRequired(false);

            e.Property(x => x.ValidTo)
                .HasColumnType("datetime(6)")
                .IsRequired(false);

            e.HasIndex(x => new { x.TableId, x.ColumnName }).IsUnique();

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

            e.Property(x => x.DecisionTreeId).IsRequired();
            e.Property(x => x.TableId).IsRequired();
            e.Property(x => x.RowIndex).IsRequired();

            e.Property(x => x.RowDataJson)
                .HasColumnType("json")
                .IsRequired();

            e.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .ValueGeneratedOnAdd();

            e.Property(x => x.UpdatedAtUtc)
                .HasColumnType("datetime(6)");

            e.HasOne(x => x.DecisionTree)
                .WithMany()
                .HasForeignKey(x => x.DecisionTreeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Table)
                .WithMany()
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.TableId);
        });

        // === ValidationLog ===
        modelBuilder.Entity<ValidationLog>(e =>
        {
            e.ToTable("validation_log");

            e.HasKey(x => x.Id);

            e.Property(x => x.DecisionTreeId).IsRequired();
            e.Property(x => x.ColumnName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ErrorType).IsRequired();
            e.Property(x => x.ErrorMessage).HasMaxLength(500).IsRequired();
            e.Property(x => x.LoggedAtUtc).HasColumnType("datetime(6)").ValueGeneratedOnAdd();

            e.HasOne(x => x.DecisionTree)
                .WithMany()
                .HasForeignKey(x => x.DecisionTreeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Table)
                .WithMany()
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            e.HasIndex(x => x.DecisionTreeId);
        });

        // === ColumnValueMapping ===
        modelBuilder.Entity<ColumnValueMapping>(e =>
        {
            e.ToTable("column_value_mapping");

            e.HasKey(x => x.Id);

            e.Property(x => x.TableColumnId).IsRequired();
            e.Property(x => x.OldPosition).IsRequired();
            e.Property(x => x.NewPosition).IsRequired();
            e.Property(x => x.ChangedAtUtc).HasColumnType("datetime(6)").ValueGeneratedOnAdd();

            e.HasOne(x => x.TableColumn)
                .WithMany()
                .HasForeignKey(x => x.TableColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.TableColumnId);
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
