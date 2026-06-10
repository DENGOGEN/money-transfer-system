using System;

namespace MoneyTransferApp.Models
{
    public class Card
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string CardNumber { get; set; }
        public string MaskedNumber { get; set; }
        public string ExpiryDate { get; set; }
        public string Cvv { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}