using CashFlowTransactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlowTransactions.Infra.Data.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("transactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").UseIdentityColumn();
                
                entity.Property(e => e.Amount)
                    .HasColumnName("amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                    
                entity.Property(e => e.Description)
                    .HasColumnName("description")
                    .HasMaxLength(200)
                    .IsRequired(false);
                    
                entity.Property(e => e.Origin)
                    .HasColumnName("origin")
                    .HasMaxLength(100)
                    .IsRequired(false);
                    
                entity.Property(e => e.TransactionDate)
                    .HasColumnName("transaction_date")
                    .IsRequired();
                    
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();
                    
                entity.Property(e => e.Type)
                    .HasColumnName("type")
                    .IsRequired();
            });
        }
    }
} 