using System;

namespace MoneyTransferApp.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Phone { get; set; }
        public string PinHash { get; set; }
        public bool IsVerified { get; set; }
        public string Status { get; set; } = "active";
        public decimal DailySentAmount { get; set; }
        public DateTime LastActivity { get; set; } = DateTime.Now;
    }
}