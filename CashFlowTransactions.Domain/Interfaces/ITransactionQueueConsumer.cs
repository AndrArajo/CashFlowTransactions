using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;

namespace CashFlowTransactions.Domain.Interfaces
{
    public interface ITransactionQueueConsumer
    {
        Task ConsumeAsync(CancellationToken cancellationToken);
        event EventHandler<Transaction> OnMessageReceived;
    }
}
