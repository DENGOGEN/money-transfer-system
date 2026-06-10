using System;

namespace MoneyTransferApp.Models
{
    public class Transaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string ToCardMask { get; set; }
        public decimal Amount { get; set; }
        public decimal Commission { get; set; }
        public string Type { get; set; } // p2p, c2c, topup, withdraw
        public TransactionStatus Status { get; set; } = TransactionStatus.Draft;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool TwoFaConfirmed { get; set; }

        public decimal TotalAmount => Amount + Commission;
    }
}