using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferApp.Models
{
    public enum TransactionStatus
    {
        Draft,
        Waiting2FA,
        Processing,
        WaitingRegistration,
        Completed,
        Cancelled,
        Failed
    }
}
