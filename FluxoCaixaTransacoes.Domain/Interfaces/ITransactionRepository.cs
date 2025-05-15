using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Enums;


namespace CashFlowTransactions.Domain.Interfaces
{

    interface ITransactionRepository
    {
        Task GetTransaction();
    }
}
