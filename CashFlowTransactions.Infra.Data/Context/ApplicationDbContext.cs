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
                entity.ToTable("Transactions");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).UseIdentityColumn();
                
                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                    
                entity.Property(e => e.Description)
                    .HasMaxLength(200)
                    .IsRequired(false);
                    
                entity.Property(e => e.Origin)
                    .HasMaxLength(100)
                    .IsRequired(false);
                    
                entity.Property(e => e.TransactionDate)
                    .IsRequired();
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                entity.Property(e => e.Type)
                    .IsRequired();
            });
        }
    }
} 