namespace MoneyTransferApp.Models
{
    public class Wallet
    {
        public string UserId { get; set; }
        public decimal Balance { get; set; }
        public decimal Frozen { get; set; }
        public string Currency { get; set; } = "RUB";

        public decimal AvailableBalance => Balance - Frozen;
    }
}