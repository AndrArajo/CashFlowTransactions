using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CashFlowTransactions.Domain.Interfaces
{
    public interface ITransactionQueueConsumer
    {
        Task StartConsumingAsync(CancellationToken cancellationToken);
    }
}
