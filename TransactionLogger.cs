using System;

namespace MoneyTransferApp.Services
{
    public static class TransactionLogger
    {
        public static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
        }
        
        public static void LogTransaction(string transactionId, string status)
        {
            Log($"Transaction {transactionId} status changed to {status}");
        }
    }
}
