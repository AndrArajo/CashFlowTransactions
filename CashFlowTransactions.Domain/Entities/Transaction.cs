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
    [Table("Transactions")]
    public sealed class Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero")]
        public decimal Amount { get; set; }

        [Required]
        [EnumDataType(typeof(TransactionType))]
        public TransactionType Type { get; set; }

        [MaxLength(100)]
        public string? Origin { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Construtor para Entity Framework
        public Transaction()
        {
        }

        public Transaction(decimal amount, TransactionType type, DateTime transactionDate, string? description = null, string? origin = null)
        {
            Amount          = amount;
            Type            = type;
            TransactionDate = transactionDate;
            Description     = description;
            Origin          = origin;
            CreatedAt       = DateTime.UtcNow;
        }
    }
}
