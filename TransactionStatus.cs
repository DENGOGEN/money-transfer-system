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
