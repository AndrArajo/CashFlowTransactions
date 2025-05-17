using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Enums;
using CashFlowTransactions.Domain.Interfaces;

namespace CashFlowTransactions.Domain.Entities
{
    [Table("transactions")]
    public sealed class Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [MaxLength(200)]
        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("amount", TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [Required]
        [Column("type")]
        [EnumDataType(typeof(TransactionType))]
        public TransactionType Type { get; set; }

        [MaxLength(100)]
        [Column("origin")]
        public string? Origin { get; set; }

        [Required]
        [Column("transaction_date")]
        public DateTime TransactionDate { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Transaction()
        {
            TransactionDate = DateTime.UtcNow;
        }

        public Transaction(decimal amount, TransactionType type, DateTime? transactionDate = null, string? description = null, string? origin = null)
        {
            Amount = amount;
            Type = type;
            
            if (transactionDate == null || transactionDate.Value.Date != DateTime.UtcNow.Date)
            {
                TransactionDate = DateTime.UtcNow;
            }
            else
            {
                // Converter para UTC se não for
                TransactionDate = transactionDate.Value.Kind != DateTimeKind.Utc 
                    ? DateTime.SpecifyKind(transactionDate.Value, DateTimeKind.Utc) 
                    : transactionDate.Value;
            }
            
            Description = description;
            Origin = origin;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
