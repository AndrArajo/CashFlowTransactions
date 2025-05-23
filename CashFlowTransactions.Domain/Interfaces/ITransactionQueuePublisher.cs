﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CashFlowTransactions.Domain.Entities;

namespace CashFlowTransactions.Domain.Interfaces
{
    public interface ITransactionQueuePublisher
    {
        Task<string> PublishAsync(Transaction transaction);
    }
}
