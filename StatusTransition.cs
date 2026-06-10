namespace MoneyTransferApp.Services
{
    public static class StatusTransition
    {
        public static bool IsAllowed(TransactionStatus from, TransactionStatus to)
        {
            switch (from)
            {
                case TransactionStatus.Draft:
                    return to == TransactionStatus.Waiting2FA || to == TransactionStatus.Processing;
                case TransactionStatus.Waiting2FA:
                    return to == TransactionStatus.Processing || to == TransactionStatus.Cancelled;
                case TransactionStatus.Processing:
                    return to == TransactionStatus.WaitingRegistration || 
                           to == TransactionStatus.Completed || 
                           to == TransactionStatus.Cancelled;
                case TransactionStatus.WaitingRegistration:
                    return to == TransactionStatus.Completed || to == TransactionStatus.Cancelled;
                default:
                    return false;
            }
        }
    }
}
